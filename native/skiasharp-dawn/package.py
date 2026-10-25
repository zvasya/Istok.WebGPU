#!/usr/bin/env python3
"""Validate the complete Skia matrix, then assemble NativeAssets runtimes on macOS."""
import hashlib
import json
from pathlib import Path
import platform
import plistlib
import shutil
import sys

# Do not import this folder's build.py as 'build': Dawn's helpers use that name.
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "dawn"))
from build import ARCHES, ROOT, run
from verify import check_binary, GLIBC_MAX
import importlib.util
_spec = importlib.util.spec_from_file_location("skia_build", Path(__file__).with_name("build.py"))
skia = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(skia)


def validate_artifacts(output, tag, version):
    commits = set()
    for target, arches in ARCHES.items():
        for arch in arches:
            folder = output / f"{target}-{arch}"
            binary = folder / skia.filename(target)
            report = json.loads((folder / "verification.json").read_text())
            check_binary(binary, [skia.normalized_arch(arch)])
            if (report["platform"] != target or report["arch"] != arch or
                    report["skia_tag"] != tag or report["dawn_revision"] != version or
                    report["sha256"] != hashlib.sha256(binary.read_bytes()).hexdigest() or
                    skia.REQUIRED_EXPORTS - set(report["exports"])):
                raise RuntimeError(f"Missing or stale Skia verification in {folder}")
            commits.add(report["skia_commit"])
            if target == "linux" and tuple(map(int, report["glibc_max"].split("."))) > GLIBC_MAX:
                raise RuntimeError(f"Unsupported GLIBC requirement in {folder}")
    if len(commits) != 1:
        raise RuntimeError("Skia matrix was built from different commits")


def destination(target, arch=None):
    package = {"macos": "macOS", "ios": "iOS", "iossimulator": "iOS",
               "android": "Android", "linux": "Linux", "windows": "Win32"}[target]
    rid = {"macos": "osx", "windows": "win"}.get(target, target)
    if arch:
        rid += "-" + skia.normalized_arch(arch)
    return ROOT / f"Istok.SkiaSharp.NativeAssets.{package}/runtimes/{rid}/native"


def package_apple(output, target, arches):
    dest = destination(target)
    dest.mkdir(parents=True, exist_ok=True)
    if target == "macos":
        result = dest / skia.filename(target)
        signed = result
    else:
        signed = dest / "libSkiaSharp.framework"
        if signed.exists():
            shutil.rmtree(signed)
        signed.mkdir()
        result = signed / "libSkiaSharp"
        info = dict(CFBundleExecutable="libSkiaSharp", CFBundleIdentifier="com.microsoft.libSkiaSharp",
                    CFBundleName="libSkiaSharp", CFBundlePackageType="FMWK", CFBundleVersion="1",
                    CFBundleShortVersionString="1.0", MinimumOSVersion="14.0",
                    CFBundleSupportedPlatforms=["iPhoneOS" if target == "ios" else "iPhoneSimulator"],
                    UIDeviceFamily=[1, 2])
        with (signed / "Info.plist").open("wb") as stream:
            plistlib.dump(info, stream)
    binaries = [output / f"{target}-{arch}" / skia.filename(target) for arch in arches]
    if len(binaries) == 1:
        shutil.copy2(binaries[0], result)
    else:
        run("lipo", "-create", *binaries, "-output", result)
    run("install_name_tool", "-id", "@rpath/" + ("libSkiaSharp.dylib" if target == "macos" else
                                                 "libSkiaSharp.framework/libSkiaSharp"), result)
    result.chmod(0o755)
    check_binary(result, [skia.normalized_arch(a) for a in arches])
    run("codesign", "--force", "--sign", "-", "--timestamp=none", signed)
    run("codesign", "--verify", "--strict", signed)


def main():
    if platform.system() != "Darwin":
        raise RuntimeError("Full matrix packaging requires macOS (lipo and codesign)")
    output = ROOT / "artifacts/skia"
    validate_artifacts(output, (ROOT / "skia-tag.txt").read_text().strip(),
                       (ROOT / "dawn-native-version.txt").read_text().strip())
    for target, arches in ARCHES.items():
        if target in ("macos", "ios", "iossimulator"):
            package_apple(output, target, arches)
        else:
            for arch in arches:
                dest = destination(target, arch)
                dest.mkdir(parents=True, exist_ok=True)
                shutil.copy2(output / f"{target}-{arch}" / skia.filename(target), dest)


if __name__ == "__main__":
    main()
