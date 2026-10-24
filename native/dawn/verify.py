"""Validate Dawn binary architecture, C exports and loading on a compatible host."""
import argparse
import ctypes
import hashlib
import json
import platform
import re
import shutil
import struct
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MAX_BLOB_SIZE = 100 * 1024 * 1024
GLIBC_MAX = (2, 28)
DXC_ASSETS = ("dxcompiler.dll", "DXC-LICENSE.TXT", "DXC-ThirdPartyNotices.txt")


def filename(target):
    return ("webgpu_dawn.dll" if target == "windows" else
            "libwebgpu_dawn.dylib" if target in ("macos", "ios", "iossimulator") else
            "libwebgpu_dawn.so")


def runtime_assets(target):
    return (filename(target),) + (DXC_ASSETS if target == "windows" else ())


def glibc_requirement(binary):
    output = subprocess.check_output(["readelf", "--version-info", "--wide", str(binary)], text=True)
    names = set(re.findall(r"\bName:\s+(GLIBC_[\w.]+)", output))
    versions = []
    for name in names:
        if not re.fullmatch(r"GLIBC_\d+(?:\.\d+)+", name):
            raise RuntimeError(f"Unsupported GLIBC ABI requirement {name} in {binary}")
        versions.append(tuple(map(int, name.removeprefix("GLIBC_").split("."))))
    if not versions:
        raise RuntimeError(f"No GLIBC version requirements found in {binary}")
    required = max(versions)
    if required > GLIBC_MAX:
        raise RuntimeError(f"{binary} requires GLIBC_{'.'.join(map(str, required))}; "
                           "build inside manylinux_2_28 (maximum GLIBC_2.28)")
    return ".".join(map(str, required))


def architectures(binary):
    with Path(binary).open("rb") as stream:
        header = stream.read(64)
        if header[:4] == b"\x7fELF":
            endian = "<" if header[5] == 1 else ">"
            machine = struct.unpack_from(endian + "H", header, 18)[0]
            return { {3: "x86", 62: "x64", 40: "arm", 183: "arm64"}[machine] }
        if header[:2] == b"MZ":
            stream.seek(struct.unpack_from("<I", header, 60)[0])
            pe = stream.read(6)
            if pe[:4] != b"PE\0\0":
                raise RuntimeError(f"Invalid PE header: {binary}")
            return { {0x14c: "x86", 0x8664: "x64", 0xaa64: "arm64"}[struct.unpack_from("<H", pe, 4)[0]] }
        if header[:4] in (b"\xcf\xfa\xed\xfe", b"\xce\xfa\xed\xfe"):
            return { {0x1000007: "x64", 0x100000c: "arm64"}[struct.unpack_from("<I", header, 4)[0]] }
        if header[:4] == b"\xca\xfe\xba\xbe":
            count = struct.unpack_from(">I", header, 4)[0]
            stream.seek(8)
            return {{0x1000007: "x64", 0x100000c: "arm64"}[struct.unpack(">IIIII", stream.read(20))[0]]
                    for _ in range(count)}
    raise RuntimeError(f"Unsupported binary format: {binary}")


def required_exports():
    functions = ROOT / "Istok.WebGPU.LowLevel/Generated/Functions.cs"
    result = set(re.findall(r'EntryPoint\s*=\s*"(wgpu\w+)"', functions.read_text(encoding="utf-8-sig")))
    if not result:
        raise RuntimeError(f"No WebGPU imports found in {functions}")
    return result


def exports(binary, target):
    if target == "windows":
        tool = shutil.which("dumpbin")
        if not tool:
            import os
            vswhere = Path(os.environ.get("ProgramFiles(x86)", "C:/Program Files (x86)")) / "Microsoft Visual Studio/Installer/vswhere.exe"
            install = subprocess.check_output([str(vswhere), "-latest", "-products", "*",
                                               "-requires", "Microsoft.VisualStudio.Component.VC.Tools.x86.x64",
                                               "-property", "installationPath"], text=True).strip()
            tools = sorted(Path(install).glob("VC/Tools/MSVC/*/bin/Hostx64/x64/dumpbin.exe"))
            if not tools:
                raise RuntimeError("dumpbin from Visual Studio is required to verify Windows exports")
            tool = str(tools[-1])
        command = [tool, "/exports", str(binary)]
    elif target in ("macos", "ios", "iossimulator"):
        command = ["nm", "-gU", str(binary)]
    elif target == "android":
        import os
        ndk = Path(os.environ.get("ANDROID_NDK_HOME") or os.environ["ANDROID_NDK_ROOT"])
        tools = list((ndk / "toolchains/llvm/prebuilt").glob("*/bin/llvm-nm*"))
        tools = [tool for tool in tools if tool.name in ("llvm-nm", "llvm-nm.exe")]
        if len(tools) != 1:
            raise RuntimeError(f"Expected one Android llvm-nm in {ndk}")
        command = [str(tools[0]), "-D", "--defined-only", str(binary)]
    else:
        command = ["nm", "-D", "--defined-only", str(binary)]
    output = subprocess.check_output(command, text=True, errors="replace")
    return set(re.findall(r"\b_?((?:wgpu|Dxc)\w+)\b", output))


def verify_dxc(folder, arch):
    dll = folder / "dxcompiler.dll"
    check_binary(dll, [arch])
    if "DxcCreateInstance" not in exports(dll, "windows"):
        raise RuntimeError(f"DxcCreateInstance is missing from {dll}")
    hashes = {name: hashlib.sha256((folder / name).read_bytes()).hexdigest() for name in DXC_ASSETS}
    # A subprocess can run an x86 DXC on an x64 runner even though 64-bit
    # Python cannot load the x86 DLL with ctypes. ARM64 requires its own host.
    host_arch = platform.machine().lower()
    can_run = (platform.system() == "Windows" and
               ((host_arch in ("amd64", "x86_64") and arch in ("x86", "x64")) or
                (host_arch in ("arm64", "aarch64") and arch == "arm64")))
    result = "not run: cross compilation requires a target host"
    if can_run:
        import os
        shader = folder / "smoke/dxc-smoke.hlsl"
        shader.write_text("[numthreads(1, 1, 1)] void main() {}\n")
        compiled = shader.with_suffix(".dxil")
        env = dict(os.environ)
        env["PATH"] = str(folder) + os.pathsep + env.get("PATH", "")
        subprocess.run([str(folder / "smoke/dxc.exe"), "-T", "cs_6_0", "-E", "main",
                        "-Fo", str(compiled), str(shader)], env=env, check=True, timeout=60)
        if not compiled.is_file() or compiled.stat().st_size == 0:
            raise RuntimeError("DXC produced no shader bytecode")
        result = "passed: HLSL compilation to Shader Model 6.0 DXIL"
    return hashes, result


def check_binary(binary, expected):
    binary = Path(binary)
    actual = architectures(binary)
    if actual != set(expected):
        raise RuntimeError(f"Wrong architectures in {binary}: {actual}, expected {set(expected)}")
    if binary.stat().st_size >= MAX_BLOB_SIZE:
        raise RuntimeError(f"{binary} exceeds GitHub's 100 MiB file limit")


def smoke(binary, target, arch, executable=None):
    host = {"Darwin": "macos", "Linux": "linux", "Windows": "windows"}.get(platform.system())
    machine = {"x86_64": "x64", "AMD64": "x64", "aarch64": "arm64", "arm64": "arm64",
               "i386": "x86", "i686": "x86", "x86": "x86"}.get(platform.machine())
    # Python's bitness determines which Windows DLL it can load.
    if host == "windows" and ctypes.sizeof(ctypes.c_void_p) == 4:
        machine = "x86"
    if target != host or arch != machine:
        return "not run: cross compilation requires a target device/host"
    lib = ctypes.CDLL(str(Path(binary).resolve()))
    lib.wgpuCreateInstance.argtypes = [ctypes.c_void_p]
    lib.wgpuCreateInstance.restype = ctypes.c_void_p
    lib.wgpuInstanceRelease.argtypes = [ctypes.c_void_p]
    lib.wgpuInstanceRelease.restype = None
    instance = lib.wgpuCreateInstance(None)
    if not instance:
        raise RuntimeError("wgpuCreateInstance returned NULL")
    lib.wgpuInstanceRelease(instance)
    if executable is not None:
        # Windows searches next to the EXE and on PATH for webgpu_dawn.dll.
        import os
        env = dict(os.environ)
        env["PATH"] = str(Path(binary).parent) + os.pathsep + env.get("PATH", "")
        return subprocess.check_output([str(executable)], env=env, text=True, timeout=60).strip()
    return "passed: load, wgpuCreateInstance, wgpuInstanceRelease"


def verify(folder, target, arch, executable=None):
    binary = folder / filename(target)
    normalized = {"x86_64": "x64", "arm64-v8a": "arm64", "armeabi-v7a": "arm"}.get(arch, arch)
    check_binary(binary, [normalized])
    symbols = exports(binary, target)
    missing = required_exports() - symbols
    if missing:
        raise RuntimeError(f"Missing WebGPU exports in {binary}: {sorted(missing)}")
    report = dict(platform=target, arch=arch, sha256=hashlib.sha256(binary.read_bytes()).hexdigest(),
                  exports=sorted(symbols), smoke=smoke(binary, target, normalized, executable))
    if target == "linux":
        report["glibc_max"] = glibc_requirement(binary)
    if target == "windows":
        report["runtime_dependencies"], report["dxc_smoke"] = verify_dxc(folder, normalized)
    if target in ("ios", "iossimulator"):
        framework = folder / "webgpu_dawn.framework/webgpu_dawn"
        check_binary(framework, [normalized])
        report["framework_sha256"] = hashlib.sha256(framework.read_bytes()).hexdigest()
    (folder / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
    print(f"Verified {target}-{arch}: {len(symbols)} exports; {report['smoke']}", flush=True)
    if target == "linux":
        print(f"Linux ABI: maximum imported GLIBC_{report['glibc_max']}", flush=True)
    if target == "windows":
        print(f"DXC: {report['dxc_smoke']}", flush=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("platform")
    parser.add_argument("arch")
    parser.add_argument("folder", type=Path)
    args = parser.parse_args()
    verify(args.folder.resolve(), args.platform, args.arch)
