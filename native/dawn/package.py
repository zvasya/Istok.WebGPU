"""Collect matrix outputs into the repository's native runtime folders (macOS host)."""
import shutil
from pathlib import Path
from build import ARCHES, ROOT, run


def package_apple(output, target, arches):
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
    run("codesign", "--force", "--sign", "-", "--timestamp=none",
        result if target == "macos" else framework)


def main():
    output = ROOT / "artifacts/dawn"
    # Require the full successful matrix before changing package contents.
    version = (ROOT / "dawn-version.txt").read_text().strip()
    for target, arches in ARCHES.items():
        for arch in arches:
            folder = output / f"{target}-{arch}"
            if (folder / "dawn-version.txt").read_text().strip() != version:
                raise RuntimeError(f"Unexpected Dawn version in {folder}")
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
            shutil.copy2(output / f"{target}-{arch}" / filename, dest / filename)


if __name__ == "__main__":
    main()
