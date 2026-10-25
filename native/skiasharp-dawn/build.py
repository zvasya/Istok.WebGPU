#!/usr/bin/env python3
"""Build one SkiaSharp slice from zvasya/skia at skia-tag.txt, using shared Dawn."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys

# Share the supported architectures and binary inspection with the Dawn pipeline.
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "dawn"))
from build import ARCHES, ROOT, enable_git_long_paths, run
from verify import check_binary, glibc_requirement

SCRIPT = Path(__file__).resolve().parent
REQUIRED_EXPORTS = {"sk_version_get_milestone", "sk_graphite_context_make_dawn"}


def filename(target):
    return "libSkiaSharp." + ("dll" if target == "windows" else
                              "dylib" if target in ("macos", "ios", "iossimulator") else "so")


def normalized_arch(arch):
    return {"x86_64": "x64", "arm64-v8a": "arm64", "armeabi-v7a": "arm"}.get(arch, arch)


def check_dawn_revision(source, version):
    deps = (source / "infra/bots/deps/deps_gen.go").read_text()
    match = re.search(r'"dawn.googlesource.com/dawn":\s*\{[^}]*Version:\s*"([0-9a-f]{40})"', deps)
    if not match or match[1] != version:
        raise RuntimeError("Skia's Dawn revision differs from dawn-native-version.txt; run Update Dawn bindings first")


def verify(binary, target, arch):
    check_binary(binary, [normalized_arch(arch)])
    if target == "windows":
        vswhere = Path(os.environ.get("ProgramFiles(x86)", "C:/Program Files (x86)")) / "Microsoft Visual Studio/Installer/vswhere.exe"
        vs = subprocess.check_output([str(vswhere), "-latest", "-products", "*", "-requires",
                                      "Microsoft.VisualStudio.Component.VC.Tools.x86.x64", "-property", "installationPath"], text=True).strip()
        tool = sorted(Path(vs).glob("VC/Tools/MSVC/*/bin/Hostx64/x64/dumpbin.exe"))[-1]
        command = [str(tool), "/exports", str(binary)]
    elif target in ("macos", "ios", "iossimulator"):
        command = ["nm", "-gU", str(binary)]
    elif target == "android":
        tool = next((Path(os.environ["ANDROID_NDK_HOME"]) / "toolchains/llvm/prebuilt").glob("*/bin/llvm-nm"))
        command = [str(tool), "-D", "--defined-only", str(binary)]
    else:
        command = ["nm", "-D", "--defined-only", str(binary)]
    symbols = subprocess.check_output(command, text=True)
    found = set(re.findall(r"\b_?(sk_\w+)\b", symbols))
    if REQUIRED_EXPORTS - found:
        raise RuntimeError(f"Missing SkiaSharp exports in {binary}: {REQUIRED_EXPORTS - found}")
    report = dict(platform=target, arch=arch, sha256=hashlib.sha256(binary.read_bytes()).hexdigest(),
                  exports=sorted(found))
    if target == "linux":
        report["glibc_max"] = glibc_requirement(binary)
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("platform", choices=ARCHES)
    parser.add_argument("--arch", required=True)
    parser.add_argument("--jobs", type=int, default=2)
    parser.add_argument("--work-dir", type=Path, default=ROOT / ".native-build/skia")
    parser.add_argument("--output-dir", type=Path, default=ROOT / "artifacts/skia")
    args = parser.parse_args()
    target, arch = args.platform, args.arch
    if arch not in ARCHES[target] or args.jobs < 1:
        parser.error("Unsupported architecture or invalid job count")
    tag = (ROOT / "skia-tag.txt").read_text().strip()
    run("git", "check-ref-format", "refs/tags/" + tag)
    version = (ROOT / "dawn-native-version.txt").read_text().strip()
    if os.name == "nt":
        enable_git_long_paths()
    work = args.work_dir.resolve()
    work.mkdir(parents=True, exist_ok=True)
    source = work / "skia"
    if not source.exists():
        run("git", "-c", "core.autocrlf=false", "clone", "--depth", "1", "--branch", tag,
            "https://github.com/zvasya/skia.git", source)
    actual = subprocess.check_output(["git", "-C", str(source), "rev-parse", "HEAD"], text=True).strip()
    expected = subprocess.check_output(["git", "-C", str(source), "rev-parse", f"refs/tags/{tag}^{{commit}}"], text=True).strip()
    if actual != expected:
        raise RuntimeError(f"Skia checkout {actual} differs from tag {tag}")
    status = subprocess.check_output(["git", "-C", str(source), "status", "--porcelain",
                                      "--untracked-files=no", "--ignore-submodules=all"], text=True).strip()
    if status:
        raise RuntimeError(f"Skia checkout has local modifications:\n{status}")
    check_dawn_revision(source, version)
    subprocess.run([sys.executable, "tools/git-sync-deps"], cwd=source, check=True)
    subprocess.run([sys.executable, "bin/fetch-gn"], cwd=source, check=True)

    # Use exactly the build settings used for Istok.WebGPU.Native.*. Its output
    # includes installed C++ headers and Windows import libraries required by Skia.
    dawn_output = work / "dawn-artifacts"
    run(sys.executable, ROOT / "native/dawn/build.py", target, "--arch", arch,
        "--jobs", args.jobs, "--no-copy-to-packages", "--work-dir", work / "dn",
        "--output-dir", dawn_output)
    dawn = dawn_output / f"{target}-{arch}"
    install = work / "prebuilt" / f"{target}-{arch}" / "install"
    if install.exists():
        shutil.rmtree(install)
    shutil.copytree(dawn, install)
    if target == "windows":
        run("pwsh", "-NoProfile", "-File", SCRIPT / "build-windows.ps1", "-Arch", arch,
            "-SkiaDir", source, "-DawnOutDir", work / "prebuilt", "-Jobs", args.jobs)
    else:
        lib = (install / "webgpu_dawn.framework/webgpu_dawn" if target in ("ios", "iossimulator") else
               install / ("libwebgpu_dawn.dylib" if target == "macos" else "libwebgpu_dawn.so"))
        env = dict(os.environ, SKIA_DIR=str(source), DAWN_DIR=str(work / "dn" / version / "source"),
                   DAWN_OUT_DIR=str(work / "prebuilt"), BUILD_JOBS=str(args.jobs))
        subprocess.run(["bash", str(SCRIPT / "build-skiasharp.sh"), target, arch, str(lib)], env=env, check=True)
    binary = source / f"out/istok/{target}-{arch}" / filename(target)
    if target in ("macos", "ios", "iossimulator"):
        run("strip", "-x", binary)
    elif target == "linux":
        run("strip", "--strip-unneeded", binary)
    elif target == "android":
        tool = next((Path(os.environ["ANDROID_NDK_HOME"]) / "toolchains/llvm/prebuilt").glob("*/bin/llvm-strip"))
        run(tool, "--strip-unneeded", binary)
    output = args.output_dir.resolve() / f"{target}-{arch}"
    if output.exists():
        shutil.rmtree(output)
    output.mkdir(parents=True)
    shutil.copy2(binary, output / binary.name)
    report = verify(output / binary.name, target, arch)
    report.update(skia_tag=tag, skia_commit=actual, dawn_revision=version)
    (output / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
    print(f"Built {target}-{arch}: {output}", flush=True)


if __name__ == "__main__":
    main()
