import hashlib
import importlib.util
import json
from pathlib import Path
import struct
import os
import subprocess
import sys
import tempfile
import unittest

SCRIPTS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(SCRIPTS.parent / "dawn"))
spec = importlib.util.spec_from_file_location("skia_package", SCRIPTS / "package.py")
package = importlib.util.module_from_spec(spec)
spec.loader.exec_module(package)
skia = package.skia


class PipelineTests(unittest.TestCase):
    def test_dawn_revision_must_match_skia(self):
        with tempfile.TemporaryDirectory() as temp:
            source = Path(temp)
            deps = source / "infra/bots/deps/deps_gen.go"
            deps.parent.mkdir(parents=True)
            deps.write_text('"dawn.googlesource.com/dawn": {\nId: "dawn.googlesource.com/dawn",\nVersion: "' + 'a' * 40 + '",\n},')
            skia.check_dawn_revision(source, 'a' * 40)
            with self.assertRaisesRegex(RuntimeError, 'Dawn revision differs'):
                skia.check_dawn_revision(source, 'b' * 40)
            deps.write_text('')
            with self.assertRaises(RuntimeError):
                skia.check_dawn_revision(source, 'a' * 40)

    def fixture(self, output):
        for target, arches in package.ARCHES.items():
            for arch in arches:
                folder = output / f'{target}-{arch}'
                folder.mkdir()
                # Minimal ELF header accepted by the shared architecture inspector.
                header = bytearray(64)
                header[:6] = b'\x7fELF\x02\x01'
                struct.pack_into('<H', header, 18, {'x64': 62, 'x86': 3, 'arm64': 183, 'arm': 40}[skia.normalized_arch(arch)])
                binary = folder / skia.filename(target)
                binary.write_bytes(header)
                report = dict(platform=target, arch=arch, skia_tag='tag', skia_commit='a' * 40,
                              dawn_revision='b' * 40, exports=sorted(skia.REQUIRED_EXPORTS),
                              sha256=hashlib.sha256(header).hexdigest(), glibc_max='2.28')
                (folder / 'verification.json').write_text(json.dumps(report))

    def test_full_matrix_and_reject_corruption(self):
        with tempfile.TemporaryDirectory() as temp:
            output = Path(temp)
            self.fixture(output)
            package.validate_artifacts(output, 'tag', 'b' * 40)
            binary = output / 'linux-arm64/libSkiaSharp.so'
            binary.write_bytes(binary.read_bytes() + b'changed')
            with self.assertRaisesRegex(RuntimeError, 'stale Skia verification'):
                package.validate_artifacts(output, 'tag', 'b' * 40)

    def test_missing_slice_and_wrong_versions(self):
        with tempfile.TemporaryDirectory() as temp:
            output = Path(temp)
            self.fixture(output)
            for tag, version in [('other', 'b' * 40), ('tag', 'c' * 40)]:
                with self.assertRaises(RuntimeError):
                    package.validate_artifacts(output, tag, version)
            (output / 'windows-arm64/libSkiaSharp.dll').unlink()
            with self.assertRaises(FileNotFoundError):
                package.validate_artifacts(output, 'tag', 'b' * 40)

    def test_reject_mixed_commits_missing_exports_and_glibc(self):
        with tempfile.TemporaryDirectory() as temp:
            output = Path(temp)
            self.fixture(output)
            report_file = output / 'linux-x64/verification.json'
            original = json.loads(report_file.read_text())
            for key, value in [('skia_commit', 'c' * 40), ('exports', []), ('glibc_max', '2.34')]:
                report_file.write_text(json.dumps(dict(original, **{key: value})))
                with self.assertRaises(RuntimeError):
                    package.validate_artifacts(output, 'tag', 'b' * 40)

    def test_unix_build_uses_shared_dawn_and_preserves_rpath(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / 'bin').mkdir()
            (root / 'dawn').mkdir()
            gn = root / 'bin/gn'
            gn.write_text('#!/usr/bin/env python3\nimport json, os, sys\nopen(os.environ["CAPTURE_ARGS"], "w").write(json.dumps(sys.argv))\n')
            gn.chmod(0o755)
            ninja = root / 'bin/ninja'
            ninja.write_text('#!/bin/sh\nexit 0\n')
            ninja.chmod(0o755)
            capture = root / 'args.json'
            env = dict(os.environ, SKIA_DIR=str(root), DAWN_DIR=str(root / 'dawn'),
                       DAWN_OUT_DIR=str(root / 'prebuilt'), CAPTURE_ARGS=str(capture),
                       PATH=str(root / 'bin') + os.pathsep + os.environ['PATH'],
                       ANDROID_NDK_HOME=str(root / 'ndk'))
            for target, arch in [('macos', 'arm64'), ('ios', 'arm64'),
                                 ('iossimulator', 'x86_64'), ('linux', 'x64'), ('android', 'arm64-v8a')]:
                subprocess.run(['bash', str(SCRIPTS / 'build-skiasharp.sh'), target, arch,
                                str(root / 'dawn/libwebgpu_dawn.so')], env=env, check=True, capture_output=True)
                args = json.loads(capture.read_text())[-1]
                self.assertIn('skia_use_dawn=true', args)
                self.assertIn('skia_enable_graphite=true', args)
                self.assertIn('skia_dawn_prebuilt_dir="' + str(root / f'prebuilt/{target}-{arch}/install') + '"', args)
                if target == 'linux':
                    self.assertIn('ORIGIN', args)
                    self.assertNotIn('-static-libstdc++', args)
                if target in ('ios', 'iossimulator'):
                    self.assertIn('@loader_path/..', args)

    def test_runtime_layout(self):
        cases = [('android', 'armeabi-v7a', 'Android/runtimes/android-arm/native'),
                 ('windows', 'x86', 'Win32/runtimes/win-x86/native'),
                 ('linux', 'arm64', 'Linux/runtimes/linux-arm64/native'),
                 ('macos', None, 'macOS/runtimes/osx/native'),
                 ('iossimulator', None, 'iOS/runtimes/iossimulator/native')]
        for target, arch, suffix in cases:
            self.assertEqual(package.destination(target, arch), package.ROOT / ('Istok.SkiaSharp.NativeAssets.' + suffix))


if __name__ == '__main__':
    unittest.main()
