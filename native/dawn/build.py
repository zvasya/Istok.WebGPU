#!/usr/bin/env python3
"""Build the Dawn revision in dawn-native-version.txt. Requires Python 3.9+, Git and CMake."""
import argparse
import os
import platform
import plistlib
import re
import shutil
import subprocess
from pathlib import Path
from verify import filename as library_filename, verify, runtime_assets
from check_api import check_api

ROOT = Path(__file__).resolve().parents[2]
ARCHES = {
    "macos": ["arm64", "x86_64"], "ios": ["arm64"],
    "iossimulator": ["arm64", "x86_64"],
    "android": ["arm64-v8a", "armeabi-v7a", "x86", "x86_64"],
    "linux": ["x64", "arm64"], "windows": ["x64", "x86", "arm64"],
}
GITHUB_MAX_BLOB_SIZE = 100 * 1024 * 1024


def run(*args):
    subprocess.run([str(a) for a in args], check=True)


def enable_git_long_paths():
    # Runtime Git configuration is inherited by CMake and Dawn's dependency
    # fetcher. A source-repository setting alone would not cover their clones.
    # Preserve any configuration already supplied by the caller, without
    # changing the user's global Git settings.
    count = int(os.environ.get("GIT_CONFIG_COUNT") or "0")
    os.environ[f"GIT_CONFIG_KEY_{count}"] = "core.longpaths"
    os.environ[f"GIT_CONFIG_VALUE_{count}"] = "true"
    os.environ["GIT_CONFIG_COUNT"] = str(count + 1)


def dxc_host_tools(source, work, flags, jobs):
    """ARM64 DXC must execute x64 TableGen tools during cross compilation."""
    host_dir = work / "windows-dxc-host-x64"
    host_flags = dict(flags)
    host_flags.update(DAWN_ENABLE_INSTALL="OFF", DAWN_BUILD_MONOLITHIC_LIBRARY="OFF",
                      DAWN_ENABLE_D3D11="OFF", DAWN_ENABLE_D3D12="OFF",
                      DAWN_ENABLE_VULKAN="OFF", DAWN_ENABLE_SPIRV_VALIDATION="OFF")
    run("cmake", "-S", source, "-B", host_dir, "-G", "Visual Studio 17 2022", "-A", "x64",
        *[f"-D{k}={v}" for k, v in host_flags.items()])
    run("cmake", "--build", host_dir, "--config", "Release", "--target",
        "llvm-tblgen", "clang-tblgen", "--parallel", jobs)
    result = {}
    for variable, name in (("LLVM_TABLEGEN", "llvm-tblgen.exe"), ("CLANG_TABLEGEN", "clang-tblgen.exe")):
        tools = list(host_dir.rglob(name))
        if len(tools) != 1:
            raise RuntimeError(f"Expected one host {name} in {host_dir}, got {tools}")
        result[variable] = tools[0]
    return result


def stage_dxc(source, build_dir, output):
    # Dawn's AddSubdirectoryDXC sets these output locations for VS Release.
    # DXC is EXCLUDE_FROM_ALL, so Dawn's install does not ship its DLL.
    shutil.copy2(build_dir / "Release/dxcompiler.dll", output / "dxcompiler.dll")
    dxc_source = source / "third_party/directx-shader-compiler/src"
    for original, packaged in (("LICENSE.TXT", "DXC-LICENSE.TXT"),
                               ("ThirdPartyNotices.txt", "DXC-ThirdPartyNotices.txt")):
        shutil.copy2(dxc_source / original, output / packaged)
    # The CLI is a test tool in downloadable artifacts, not a NuGet runtime asset.
    (output / "smoke").mkdir(exist_ok=True)
    shutil.copy2(build_dir / "Release/dxc.exe", output / "smoke/dxc.exe")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("platform", choices=ARCHES)
    parser.add_argument("--arch", help="One architecture; default: all (Linux: host only)")
    parser.add_argument("--jobs", type=int, default=2, help="Parallel compilation jobs")
    parser.add_argument("--copy-to-packages", action="store_true", default=True,
                        help="Copy binaries to native packages (default)")
    parser.add_argument("--no-copy-to-packages", dest="copy_to_packages", action="store_false")
    parser.add_argument("--work-dir", type=Path, default=ROOT / ".native-build")
    parser.add_argument("--output-dir", type=Path, default=ROOT / "artifacts/dawn")
    args = parser.parse_args()
    # Relative command-line paths must not change meaning in child tools.
    args.work_dir = args.work_dir.resolve()
    args.output_dir = args.output_dir.resolve()
    host = {"Darwin": "macos", "Linux": "linux", "Windows": "windows"}.get(platform.system())
    if args.platform in ("macos", "ios", "iossimulator") and host != "macos":
        parser.error("Apple builds require macOS and Xcode")
    if args.platform in ("linux", "windows") and args.platform != host:
        parser.error(f"{args.platform} requires a {args.platform} host")
    host_arch = {"x86_64": "x64", "AMD64": "x64", "aarch64": "arm64", "arm64": "arm64"}.get(platform.machine())
    arches = [args.arch] if args.arch else ([host_arch] if args.platform == "linux" else ARCHES[args.platform])
    if any(a not in ARCHES[args.platform] for a in arches):
        parser.error(f"Supported architectures: {ARCHES[args.platform]}")
    if args.platform == "linux" and arches != [host_arch]:
        parser.error("Linux builds require a host of the requested architecture")
    if args.jobs < 1:
        parser.error("--jobs must be positive")
    version = (ROOT / "dawn-native-version.txt").read_text().strip()
    is_release_tag = re.fullmatch(r"v\d{8}\.\d{6}", version) is not None
    is_commit = re.fullmatch(r"[0-9a-f]{40}", version) is not None
    if not is_release_tag and not is_commit:
        parser.error("dawn-native-version.txt must contain a Dawn release tag or full commit SHA")
    if platform.system() == "Windows":
        enable_git_long_paths()
    work = args.work_dir.resolve() / version
    source = work / "source"
    if not source.exists():
        source.parent.mkdir(parents=True, exist_ok=True)
        if is_release_tag:
            run("git", "-c", "core.autocrlf=false", "-c", "core.eol=lf",
                "clone", "--depth", "1", "--branch", version,
                "https://github.com/google/dawn.git", source)
        else:
            run("git", "init", source)
            run("git", "-C", source, "config", "core.autocrlf", "false")
            run("git", "-C", source, "config", "core.eol", "lf")
            run("git", "-C", source, "remote", "add", "origin", "https://github.com/google/dawn.git")
            run("git", "-C", source, "fetch", "--depth", "1", "origin", version)
            run("git", "-C", source, "checkout", "--detach", "FETCH_HEAD")
    actual = subprocess.check_output(["git", "-C", str(source), "rev-parse", "HEAD"], text=True).strip()
    expected = version if is_commit else subprocess.check_output(
        ["git", "-C", str(source), "rev-list", "-n", "1", version], text=True).strip()
    if actual != expected:
        raise RuntimeError(f"Dawn checkout {actual} differs from {version} ({expected})")
    status = subprocess.check_output([
        "git", "-C", str(source), "status", "--porcelain",
        "--untracked-files=no", "--ignore-submodules=all",
    ], text=True).strip()
    if status:
        raise RuntimeError(f"Dawn checkout has local modifications:\n{status}")
    for arch in arches:
        build(args, source, work, arch, version)
    if args.copy_to_packages and args.platform in ("macos", "ios", "iossimulator"):
        from package import package_apple
        package_apple(args.output_dir.resolve(), args.platform, arches)


def build(args, source, work, arch, version):
    check_api(source / "src/dawn/dawn.json")
    target = args.platform
    build_dir = work / f"{target}-{arch}"
    install = build_dir / "install"
    flags = {
        "CMAKE_BUILD_TYPE": "Release", "CMAKE_INSTALL_PREFIX": install,
        "DAWN_BUILD_MONOLITHIC_LIBRARY": "SHARED", "BUILD_SHARED_LIBS": "OFF",
        "DAWN_ENABLE_INSTALL": "ON", "TINT_ENABLE_INSTALL": "OFF",
        "DAWN_FETCH_DEPENDENCIES": "ON", "DAWN_BUILD_SAMPLES": "OFF",
        "DAWN_BUILD_TESTS": "OFF", "DAWN_BUILD_BENCHMARKS": "OFF",
        "DAWN_BUILD_PROTOBUF": "OFF", "DAWN_USE_GLFW": "OFF",
        "DAWN_ENABLE_SWIFTSHADER": "OFF", "DAWN_ENABLE_NULL": "OFF",
        "DAWN_FORCE_SYSTEM_COMPONENT_LOAD": "ON", "TINT_BUILD_TESTS": "OFF",
        "TINT_BUILD_CMD_TOOLS": "OFF", "TINT_BUILD_BENCHMARKS": "OFF",
        "TINT_BUILD_IR_BINARY": "OFF", "TINT_BUILD_NULL_WRITER": "ON",
        "TINT_BUILD_GLSL_VALIDATOR": "OFF",
        "DAWN_ENABLE_SPIRV_VALIDATION": "ON" if target in ("android", "linux", "windows") else "OFF",
        "DAWN_USE_BUILT_DXC": "ON" if target == "windows" else "OFF",
        "DAWN_DXC_ENABLE_ASSERTS_IN_NDEBUG": "OFF",
        # The generated C++20 module interface is not used by webgpu_dawn.
        # Dawn's compiler probe can enable it for GCC even when CMake cannot
        # discover module import dependencies (notably on Linux ARM64).
        "DAWN_SUPPORTS_CXX_MODULES": "OFF",
        "DAWN_ENABLE_DESKTOP_GL": "OFF",
        "DAWN_ENABLE_METAL": "ON" if target in ("macos", "ios", "iossimulator") else "OFF",
        "DAWN_ENABLE_VULKAN": "ON" if target in ("android", "linux", "windows") else "OFF",
        "DAWN_ENABLE_OPENGLES": "ON" if target in ("android", "linux") else "OFF",
        "DAWN_ENABLE_D3D11": "ON" if target == "windows" else "OFF",
        "DAWN_ENABLE_D3D12": "ON" if target == "windows" else "OFF",
    }
    generator = ["-G", "Ninja"]
    if target in ("macos", "ios", "iossimulator"):
        flags.update(CMAKE_OSX_ARCHITECTURES=arch,
                     CMAKE_OSX_DEPLOYMENT_TARGET=os.environ.get("MACOS_MIN", "11.0") if target == "macos" else os.environ.get("IOS_MIN", "14.0"))
        if target != "macos":
            flags.update(CMAKE_SYSTEM_NAME="iOS", CMAKE_OSX_SYSROOT="iphoneos" if target == "ios" else "iphonesimulator")
    elif target == "android":
        ndk = os.environ.get("ANDROID_NDK_HOME") or os.environ.get("ANDROID_NDK_ROOT")
        if not ndk:
            raise RuntimeError("Set ANDROID_NDK_HOME to an Android NDK installation")
        flags.update(CMAKE_TOOLCHAIN_FILE=Path(ndk) / "build/cmake/android.toolchain.cmake",
                     ANDROID_ABI=arch, ANDROID_PLATFORM="android-" + os.environ.get("ANDROID_API", "24"),
                     ANDROID_STL="c++_static", CMAKE_SHARED_LINKER_FLAGS="-Wl,-z,max-page-size=16384 -Wl,--build-id=sha1")
    elif target == "linux":
        # Keep libstdc++ and libgcc dynamic. Their static archives are not
        # guaranteed to be PIC and cannot reliably be linked into a shared
        # webgpu_dawn library on all Linux architectures.
        # An explicit empty value also clears the old flags from an existing
        # CMake cache when a local build directory is reused.
        flags.update(DAWN_USE_X11="ON", DAWN_USE_WAYLAND="ON",
                     CMAKE_SHARED_LINKER_FLAGS="")
    elif target == "windows":
        generator = ["-G", "Visual Studio 17 2022", "-A", {"x64": "x64", "x86": "Win32", "arm64": "ARM64"}[arch]]
        # Keep the CRT inside the DLL, including Abseil's separately configured
        # targets, so consumers don't need a matching VC++ redistributable.
        flags.update(CMAKE_MSVC_RUNTIME_LIBRARY="MultiThreaded",
                     ABSL_MSVC_STATIC_RUNTIME="ON", LLVM_USE_CRT_RELEASE="MT",
                     CMAKE_SYSTEM_VERSION="10.0.26100.0",
                     CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION="10.0.26100.0")
        if arch == "arm64" and platform.machine().lower() not in ("arm64", "aarch64"):
            flags.update(dxc_host_tools(source, work, flags, args.jobs))
    run("cmake", "-S", source, "-B", build_dir, *generator,
        *[f"-D{k}={v}" for k, v in flags.items()])
    targets = ["webgpu_dawn", "webgpu_upstream_cpp_headers_gen"]
    if target == "windows":
        targets += ["dxcompiler", "dxc"]
    run("cmake", "--build", build_dir, "--config", "Release",
        # Installation includes upstream C++ headers which aren't dependencies
        # of the monolithic C library (especially with C++ modules disabled).
        "--target", *targets, "--parallel", args.jobs)
    if install.exists():
        shutil.rmtree(install)
    run("cmake", "--install", build_dir, "--config", "Release")
    output = args.output_dir.resolve() / f"{target}-{arch}"
    if output.exists():
        shutil.rmtree(output)
    output.mkdir(parents=True, exist_ok=True)
    filename = library_filename(target)
    binaries = list(install.rglob(filename))
    if len(binaries) != 1:
        raise RuntimeError(f"Expected one {filename} in {install}, got {binaries}")
    installed_binary = binaries[0]
    if target == "android":
        # CMake's Release configuration still leaves a very large ELF symbol
        # table in Dawn. Keep the dynamic symbols needed by consumers, but
        # remove everything that is not required at runtime before publishing.
        ndk = Path(os.environ.get("ANDROID_NDK_HOME") or os.environ["ANDROID_NDK_ROOT"])
        strip_tools = [tool for tool in (ndk / "toolchains/llvm/prebuilt").glob("*/bin/llvm-strip*")
                       if tool.name in ("llvm-strip", "llvm-strip.exe")]
        if len(strip_tools) != 1:
            raise RuntimeError(f"Expected one Android llvm-strip in {ndk}, got {strip_tools}")
        run(strip_tools[0], "--strip-unneeded", installed_binary)
    elif target == "linux":
        strip_tool = shutil.which("strip")
        if not strip_tool:
            raise RuntimeError("GNU strip is required for Linux packaging")
        run(strip_tool, "--strip-unneeded", installed_binary)
    if installed_binary.stat().st_size >= GITHUB_MAX_BLOB_SIZE:
        size_mib = installed_binary.stat().st_size / 1024 / 1024
        raise RuntimeError(
            f"{installed_binary} is {size_mib:.2f} MiB after stripping; "
            "GitHub rejects files of 100 MiB or larger"
        )
    for directory in ("bin", "lib", "include"):
        if (install / directory).exists():
            shutil.copytree(install / directory, output / directory, dirs_exist_ok=True)
    binary = output / filename
    shutil.copy2(installed_binary, binary)
    if target == "windows":
        stage_dxc(source, build_dir, output)
    if target in ("ios", "iossimulator"):
        framework = output / "webgpu_dawn.framework"
        framework.mkdir(exist_ok=True)
        shutil.copy2(binary, framework / "webgpu_dawn")
        run("install_name_tool", "-id", "@rpath/webgpu_dawn.framework/webgpu_dawn", framework / "webgpu_dawn")
        info = dict(CFBundleExecutable="webgpu_dawn", CFBundleIdentifier="com.google.dawn.webgpu",
                    CFBundleName="webgpu_dawn", CFBundlePackageType="FMWK", CFBundleVersion="1",
                    CFBundleShortVersionString="1.0", MinimumOSVersion=flags["CMAKE_OSX_DEPLOYMENT_TARGET"],
                    CFBundleSupportedPlatforms=["iPhoneOS" if target == "ios" else "iPhoneSimulator"],
                    UIDeviceFamily=[1, 2])
        with (framework / "Info.plist").open("wb") as stream:
            plistlib.dump(info, stream)
        run("codesign", "--force", "--sign", "-", "--timestamp=none", framework)
    if target == "macos":
        run("install_name_tool", "-id", "@rpath/libwebgpu_dawn.dylib", binary)
        run("codesign", "--force", "--sign", "-", "--timestamp=none", binary)
    smoke_executable = None
    if target in ("macos", "linux", "windows"):
        smoke_dir = build_dir / "smoke"
        smoke_output = output / "smoke"
        # Compile a real consumer against the installed C API and import library.
        # Cross-built executables are retained for testing on the target host.
        smoke_flags = {k: v for k, v in flags.items() if k.startswith("CMAKE_")}
        smoke_flags.update(CMAKE_PREFIX_PATH=install, DAWN_BINARY=binary,
                           CMAKE_RUNTIME_OUTPUT_DIRECTORY=smoke_output,
                           CMAKE_RUNTIME_OUTPUT_DIRECTORY_RELEASE=smoke_output)
        run("cmake", "-S", ROOT / "native/dawn/smoke", "-B", smoke_dir, *generator,
            *[f"-D{k}={v}" for k, v in smoke_flags.items()])
        run("cmake", "--build", smoke_dir, "--config", "Release", "--parallel", args.jobs)
        smoke_executable = smoke_output / ("dawn_smoke.exe" if target == "windows" else "dawn_smoke")
    verify(output, target, arch, smoke_executable)
    (output / "dawn-native-version.txt").write_text(version + "\n")
    shutil.copy2(source / "LICENSE", output / "LICENSE")
    if args.copy_to_packages and target not in ("macos", "ios", "iossimulator"):
        package = {"macos": "macOS", "ios": "iOS", "iossimulator": "iOS", "android": "Android", "linux": "Linux", "windows": "Win32"}[target]
        rid = {"macos": "osx", "windows": "win"}.get(target, target)
        rid_arch = {"x86_64": "x64", "arm64-v8a": "arm64", "armeabi-v7a": "arm"}.get(arch, arch)
        dest = ROOT / f"Istok.WebGPU.Native.{package}/runtimes/{rid}-{rid_arch}/native"
        dest.mkdir(parents=True, exist_ok=True)
        for asset in runtime_assets(target):
            shutil.copy2(output / asset, dest / asset)
    print(f"Built {target}-{arch}: {output}", flush=True)


if __name__ == "__main__":
    main()
