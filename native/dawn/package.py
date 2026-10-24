"""Collect matrix outputs into the repository's native runtime folders (macOS host)."""
import shutil
import hashlib
import json
import platform
from pathlib import Path
from build import ARCHES, ROOT, run
from verify import check_binary, filename, required_exports, runtime_assets, DXC_ASSETS, GLIBC_MAX


def package_apple(output, target, arches):
    if platform.system() != "Darwin":
        raise RuntimeError("Apple packaging requires macOS (lipo and codesign)")
    expected = [{"x86_64": "x64"}.get(arch, arch) for arch in arches]
    # Legacy package layout expects a universal macOS/simulator library.
    package = "macOS" if target == "macos" else "iOS"
    rid = "osx" if target == "macos" else target
    dest = ROOT / f"Istok.WebGPU.Native.{package}/runtimes/{rid}/native"
    dest.mkdir(parents=True, exist_ok=True)
    if target == "macos":
        filename = "libwebgpu_dawn.dylib"
        binaries = [output / f"{target}-{arch}" / filename for arch in arches]
        result = dest / filename
    else:
        name = "webgpu_dawn.framework"
        frameworks = [output / f"{target}-{arch}" / name for arch in arches]
        binaries = [fw / "webgpu_dawn" for fw in frameworks]
        framework = dest / name
        # A previous signature must not survive a replacement executable.
        if framework.exists():
            shutil.rmtree(framework)
        shutil.copytree(frameworks[0], framework)
        result = framework / "webgpu_dawn"
    if len(binaries) == 1:
        shutil.copy2(binaries[0], result)
    else:
        run("lipo", "-create", *binaries, "-output", result)
    check_binary(result, expected)
    run("codesign", "--force", "--sign", "-", "--timestamp=none",
        result if target == "macos" else framework)
    run("codesign", "--verify", "--strict", result if target == "macos" else framework)


def validate_artifacts(output, version):
    """Check every input before replacing any runtime package."""
    required = required_exports()
    for target, arches in ARCHES.items():
        for arch in arches:
            folder = output / f"{target}-{arch}"
            if (folder / "dawn-native-version.txt").read_text().strip() != version:
                raise RuntimeError(f"Unexpected Dawn version in {folder}")
            report = json.loads((folder / "verification.json").read_text())
            binary = folder / filename(target)
            expected = {"x86_64": "x64", "arm64-v8a": "arm64", "armeabi-v7a": "arm"}.get(arch, arch)
            check_binary(binary, [expected])
            if (report["platform"] != target or report["arch"] != arch or
                    report["sha256"] != hashlib.sha256(binary.read_bytes()).hexdigest() or
                    required - set(report["exports"])):
                raise RuntimeError(f"Missing or stale binary verification in {folder}")
            if not (folder / "include/dawn/webgpu.h").is_file():
                raise RuntimeError(f"Missing installed Dawn headers in {folder}")
            if target == "linux":
                if tuple(map(int, report["glibc_max"].split("."))) > GLIBC_MAX:
                    raise RuntimeError(f"Unsupported GLIBC requirement in {folder}")
            if target == "windows":
                check_binary(folder / "dxcompiler.dll", [expected])
                for asset in DXC_ASSETS:
                    if report.get("runtime_dependencies", {}).get(asset) != hashlib.sha256((folder / asset).read_bytes()).hexdigest():
                        raise RuntimeError(f"Missing or stale DXC verification for {asset} in {folder}")
            if target in ("ios", "iossimulator"):
                framework = folder / "webgpu_dawn.framework"
                check_binary(framework / "webgpu_dawn", [expected])
                if report.get("framework_sha256") != hashlib.sha256((framework / "webgpu_dawn").read_bytes()).hexdigest():
                    raise RuntimeError(f"Stale framework verification in {folder}")
                if not (framework / "Info.plist").is_file():
                    raise RuntimeError(f"Missing Info.plist in {framework}")


def main():
    output = ROOT / "artifacts/dawn"
    # Require the full successful matrix before changing package contents.
    version = (ROOT / "dawn-native-version.txt").read_text().strip()
    if platform.system() != "Darwin":
        raise RuntimeError("Full matrix packaging requires macOS")
    validate_artifacts(output, version)
    for target, arches in ARCHES.items():
        if target in ("macos", "ios", "iossimulator"):
            package_apple(output, target, arches)
            continue
        package = {"android": "Android", "linux": "Linux", "windows": "Win32"}[target]
        filename = "webgpu_dawn.dll" if target == "windows" else "libwebgpu_dawn.so"
        for arch in arches:
            rid_arch = {"arm64-v8a": "arm64", "armeabi-v7a": "arm", "x86_64": "x64"}.get(arch, arch)
            rid = "win" if target == "windows" else target
            dest = ROOT / f"Istok.WebGPU.Native.{package}/runtimes/{rid}-{rid_arch}/native"
            dest.mkdir(parents=True, exist_ok=True)
            for asset in runtime_assets(target):
                shutil.copy2(output / f"{target}-{arch}" / asset, dest / asset)


if __name__ == "__main__":
    main()
