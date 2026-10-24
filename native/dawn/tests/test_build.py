import hashlib
import json
import os
from pathlib import Path
import struct
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import build
import package
import verify
from check_api import check_api


def elf(machine):
    header = bytearray(64)
    header[:6] = b"\x7fELF\x02\x01"
    struct.pack_into("<H", header, 18, machine)
    return header


def pe(machine):
    header = bytearray(128)
    header[:2] = b"MZ"
    struct.pack_into("<I", header, 60, 64)
    header[64:68] = b"PE\0\0"
    struct.pack_into("<H", header, 68, machine)
    return header


class BinaryVerificationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)

    def binary(self, data):
        path = self.root / "binary"
        path.write_bytes(data)
        return path

    def test_elf_architectures(self):
        for machine, arch in ((3, "x86"), (62, "x64"), (40, "arm"), (183, "arm64")):
            with self.subTest(arch=arch):
                self.assertEqual(verify.architectures(self.binary(elf(machine))), {arch})

    def test_windows_architectures(self):
        for machine, arch in ((0x14c, "x86"), (0x8664, "x64"), (0xaa64, "arm64")):
            header = bytearray(128)
            header[:2] = b"MZ"
            struct.pack_into("<I", header, 60, 64)
            header[64:68] = b"PE\0\0"
            struct.pack_into("<H", header, 68, machine)
            self.assertEqual(verify.architectures(self.binary(header)), {arch})

    def test_apple_slices(self):
        for cpu, arch in ((0x1000007, "x64"), (0x100000c, "arm64")):
            self.assertEqual(verify.architectures(self.binary(struct.pack("<IIIIIIII", 0xfeedfacf, cpu, 0, 0, 0, 0, 0, 0))), {arch})
        fat = struct.pack(">II", 0xcafebabe, 2)
        fat += b"".join(struct.pack(">IIIII", cpu, 0, 0, 0, 0) for cpu in (0x1000007, 0x100000c))
        self.assertEqual(verify.architectures(self.binary(fat)), {"x64", "arm64"})

    def test_wrong_architecture_and_unknown_format_fail(self):
        with self.assertRaises(RuntimeError):
            verify.check_binary(self.binary(elf(62)), ["arm64"])
        with self.assertRaises(RuntimeError):
            verify.architectures(self.binary(b"not a library"))

    def test_universal_file_size_is_checked(self):
        with patch.object(verify, "MAX_BLOB_SIZE", 32):
            with self.assertRaises(RuntimeError):
                verify.check_binary(self.binary(elf(62)), ["x64"])

    def test_missing_export_prevents_success_report(self):
        (self.root / "libwebgpu_dawn.so").write_bytes(elf(62))
        with patch.object(verify, "exports", return_value=set()):
            with self.assertRaisesRegex(RuntimeError, "Missing WebGPU exports"):
                verify.verify(self.root, "linux", "x64")
        self.assertFalse((self.root / "verification.json").exists())

    def test_verification_report_hash(self):
        data = elf(62)
        (self.root / "libwebgpu_dawn.so").write_bytes(data)
        with patch.object(verify, "exports", return_value=verify.required_exports()), \
                patch.object(verify, "smoke", return_value="passed"), \
                patch.object(verify, "glibc_requirement", return_value="2.28"):
            verify.verify(self.root, "linux", "x64")
        report = json.loads((self.root / "verification.json").read_text())
        self.assertEqual(report["sha256"], hashlib.sha256(data).hexdigest())

    def test_packaging_rejects_stale_binary_before_copying(self):
        folder = self.root / "linux-x64"
        folder.mkdir()
        (folder / "dawn-native-version.txt").write_text("revision")
        (folder / "libwebgpu_dawn.so").write_bytes(elf(62))
        (folder / "verification.json").write_text(json.dumps(dict(
            platform="linux", arch="x64", sha256="stale", exports=sorted(verify.required_exports()))))
        with patch.object(package, "ARCHES", {"linux": ["x64"]}), \
                patch.object(package.shutil, "copy2") as copy:
            with self.assertRaisesRegex(RuntimeError, "stale binary"):
                package.validate_artifacts(self.root, "revision")
            copy.assert_not_called()

    def test_cross_build_is_never_loaded(self):
        with patch.object(verify.platform, "system", return_value="Darwin"), \
                patch.object(verify.ctypes, "CDLL") as load:
            self.assertIn("not run", verify.smoke("unused.dll", "windows", "x64"))
            load.assert_not_called()

    def test_git_long_paths_preserve_existing_runtime_configuration(self):
        with patch.dict(os.environ, {"GIT_CONFIG_COUNT": "1", "GIT_CONFIG_KEY_0": "core.autocrlf",
                                     "GIT_CONFIG_VALUE_0": "false"}, clear=True):
            build.enable_git_long_paths()
            self.assertEqual(os.environ["GIT_CONFIG_COUNT"], "2")
            self.assertEqual(os.environ["GIT_CONFIG_KEY_0"], "core.autocrlf")
            self.assertEqual(os.environ["GIT_CONFIG_VALUE_0"], "false")
            self.assertEqual(os.environ["GIT_CONFIG_KEY_1"], "core.longpaths")
            self.assertEqual(os.environ["GIT_CONFIG_VALUE_1"], "true")

    def test_git_long_paths_reach_dependency_fetcher_subprocesses(self):
        git = build.shutil.which("git")
        if not git:
            self.skipTest("Git is required for the subprocess inheritance check")
        with patch.dict(os.environ, {"GIT_CONFIG_COUNT": ""}):
            build.enable_git_long_paths()
            # Model CMake -> Python dependency fetcher -> Git, outside the source
            # checkout, so this cannot accidentally use repository-local config.
            child = ("import subprocess, sys; "
                     "print(subprocess.check_output([sys.argv[1], 'config', '--get', 'core.longpaths'], text=True).strip())")
            actual = build.subprocess.check_output([sys.executable, "-c", child, git],
                                                   cwd=self.root, text=True).strip()
            self.assertEqual(actual, "true")

    def test_glibc_ceiling_uses_numeric_versions(self):
        output = "Name: GLIBC_2.9\nName: GLIBC_2.28\nName: GLIBCXX_3.4.29\n"
        with patch.object(verify.subprocess, "check_output", return_value=output):
            self.assertEqual(verify.glibc_requirement("library.so"), "2.28")
        for bad in ("GLIBC_2.29", "GLIBC_2.38", "GLIBC_PRIVATE", "GLIBC_ABI_DT_RELR"):
            with self.subTest(bad=bad), \
                    patch.object(verify.subprocess, "check_output", return_value="Name: " + bad):
                with self.assertRaises(RuntimeError):
                    verify.glibc_requirement("library.so")

    def test_dxc_missing_export_or_wrong_architecture_is_rejected(self):
        (self.root / "dxcompiler.dll").write_bytes(pe(0x8664))
        with self.assertRaisesRegex(RuntimeError, "Wrong architectures"):
            verify.verify_dxc(self.root, "arm64")
        with patch.object(verify, "exports", return_value=set()):
            with self.assertRaisesRegex(RuntimeError, "DxcCreateInstance"):
                verify.verify_dxc(self.root, "x64")

    def windows_artifact(self):
        folder = self.root / "artifacts/dawn/windows-x64"
        (folder / "include/dawn").mkdir(parents=True)
        (folder / "include/dawn/webgpu.h").write_text("header")
        (folder / "dawn-native-version.txt").write_text("revision")
        (folder / "webgpu_dawn.dll").write_bytes(pe(0x8664))
        (folder / "dxcompiler.dll").write_bytes(pe(0x8664))
        for asset in verify.DXC_ASSETS[1:]:
            (folder / asset).write_text(asset)
        with patch.object(verify, "exports", side_effect=lambda binary, target:
                          {"DxcCreateInstance"} if binary.name == "dxcompiler.dll" else verify.required_exports()), \
                patch.object(verify, "smoke", return_value="passed"), \
                patch.object(verify.platform, "system", return_value="Darwin"):
            verify.verify(folder, "windows", "x64")
        (self.root / "dawn-native-version.txt").write_text("revision")
        return folder

    def test_windows_packaging_includes_dxc_and_notices(self):
        folder = self.windows_artifact()
        with patch.object(package, "ROOT", self.root), \
                patch.object(package, "ARCHES", {"windows": ["x64"]}), \
                patch.object(package.platform, "system", return_value="Darwin"):
            package.main()
        dest = self.root / "Istok.WebGPU.Native.Win32/runtimes/win-x64/native"
        self.assertEqual({item.name for item in dest.iterdir()}, set(verify.runtime_assets("windows")))
        for asset in verify.runtime_assets("windows"):
            self.assertEqual((dest / asset).read_bytes(), (folder / asset).read_bytes())

    def test_windows_packaging_rejects_replaced_dxc(self):
        folder = self.windows_artifact()
        with (folder / "dxcompiler.dll").open("ab") as stream:
            stream.write(b"replaced")
        with patch.object(package, "ARCHES", {"windows": ["x64"]}):
            with self.assertRaisesRegex(RuntimeError, "stale DXC"):
                package.validate_artifacts(self.root / "artifacts/dawn", "revision")

    def test_stage_dxc_uses_matching_build_and_ships_licenses(self):
        source = self.root / "source"
        dxc = source / "third_party/directx-shader-compiler/src"
        dxc.mkdir(parents=True)
        (dxc / "LICENSE.TXT").write_text("license")
        (dxc / "ThirdPartyNotices.txt").write_text("notices")
        built = self.root / "build/Release"
        built.mkdir(parents=True)
        (built / "dxcompiler.dll").write_bytes(pe(0xaa64))
        (built / "dxc.exe").write_bytes(pe(0xaa64))
        output = self.root / "output"
        output.mkdir()
        build.stage_dxc(source, built.parent, output)
        self.assertEqual(verify.architectures(output / "dxcompiler.dll"), {"arm64"})
        self.assertEqual((output / "DXC-LICENSE.TXT").read_text(), "license")
        self.assertTrue((output / "smoke/dxc.exe").exists())

    def test_api_guard_accepts_additions_but_rejects_layout_changes(self):
        old = {"descriptor": {"category": "structure", "members": [{"name": "size", "type": "uint64_t"}]},
               "mode": {"category": "enum", "values": [{"name": "first", "value": 1}]}}
        binding = self.root / "bindings.json"
        native = self.root / "native.json"
        binding.write_text(json.dumps(old))
        new = json.loads(json.dumps(old))
        new["mode"]["values"].append({"name": "second", "value": 2})
        native.write_text(json.dumps(new))
        check_api(native, binding)
        new["descriptor"]["members"].append({"name": "extra", "type": "uint32_t"})
        native.write_text(json.dumps(new))
        with self.assertRaisesRegex(RuntimeError, "structure layout changed"):
            check_api(native, binding)

    def test_api_guard_rejects_enum_renumbering_and_signature_changes(self):
        old = {"mode": {"category": "enum", "values": [{"name": "first", "value": 1}]},
               "device": {"category": "object", "methods": [{"name": "get", "returns": "uint32_t"}]}}
        binding = self.root / "bindings.json"
        native = self.root / "native.json"
        binding.write_text(json.dumps(old))
        new = json.loads(json.dumps(old))
        new["mode"]["values"][0]["value"] = 2
        new["device"]["methods"][0]["returns"] = "uint64_t"
        native.write_text(json.dumps(new))
        with self.assertRaisesRegex(RuntimeError, "enum value changed"):
            check_api(native, binding)


if __name__ == "__main__":
    unittest.main()
