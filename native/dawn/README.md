# Native Dawn builds

`build.py` reads the release tag or full commit SHA from the repository's
`dawn-native-version.txt`, checks out that revision from `https://github.com/google/dawn`, and builds
`DAWN_BUILD_MONOLITHIC_LIBRARY=SHARED` with `BUILD_SHARED_LIBS=OFF`.
CMake downloads Dawn's pinned dependencies. No Skia checkout is required.

The `Update Dawn bindings` workflow derives the native revision from
`infra/bots/deps/deps_gen.go` at the mono/skia revision pinned by `skia-tag.txt`.
Despite its name, this file accepts a commit SHA, tag or branch from **mono/skia**,
not a SkiaSharp NuGet version. The current SHA `a9e4fac6f7587c9993142d3109ee411843e70655`
is the `externals/skia` submodule revision in mono/SkiaSharp tag
`v4.156.0-preview.1.26512.10`. Prefer a commit SHA for reproducible builds; when
updating SkiaSharp, copy the submodule SHA from the matching release tag.
Separately, it records the Emdawn release from the .NET workload in
`emdawn-version.txt` and generates the shared WebGPU bindings from that older,
browser-compatible API surface.

Requirements: Python 3.9+, Git, CMake 3.30+ and a C++ compiler.
Unix builds also require Ninja. Apple builds require full Xcode selected with
`xcode-select`; Windows builds require Visual Studio 2022 with Desktop C++,
Windows SDK 10.0.26100.0, DIA SDK and the x86/ARM64 tools for the requested architectures.
Android builds require NDK r28 (`ANDROID_NDK_HOME` or `ANDROID_NDK_ROOT`).
Linux release builds use native `quay.io/pypa/manylinux_2_28_x86_64` and
`quay.io/pypa/manylinux_2_28_aarch64` containers on matching hosts. These provide
the glibc 2.28 baseline and a modern C++ compiler. X11, EGL, Wayland and xkbcommon
development packages are installed with `dnf`; see the workflow for the list.
Local Linux builds must also use a glibc 2.28 build environment: verification
rejects a library importing newer GLIBC symbols. This targets glibc-based Linux;
graphics drivers and system libraries are still runtime requirements.

Run from the repository root:

```sh
python3 native/dawn/build.py macos
python3 native/dawn/build.py ios
python3 native/dawn/build.py iossimulator
ANDROID_NDK_HOME=/path/to/ndk python3 native/dawn/build.py android
python3 native/dawn/build.py linux
```

On Windows:

```powershell
python native/dawn/build.py windows
python native/dawn/build.py windows --arch x64 --jobs 4
```

By default all supported architectures are built, except Linux which builds
only the host architecture. Use `--arch` to build a single architecture.
Apple targets must run on macOS; Windows on Windows; Linux on the requested
architecture's Linux host. Android supports macOS and Linux hosts.

| Platform | Architectures | Backends |
| --- | --- | --- |
| macos | arm64, x86_64 | Metal |
| ios | arm64 | Metal |
| iossimulator | arm64, x86_64 | Metal |
| android | arm64-v8a, armeabi-v7a, x86, x86_64 | Vulkan, OpenGL ES |
| linux | x64, arm64 | Vulkan, OpenGL ES; X11/Wayland |
| windows | x64, x86, arm64 | D3D11, D3D12, Vulkan |

SPIR-V validation is enabled on Android, Linux and Windows, matching Dawn's
default for Vulkan/GL configurations. It remains off for Metal-only Apple builds.

Windows enables `DAWN_USE_BUILT_DXC`. DXC is built from the revision pinned in
Dawn's own `DEPS`, for the same architecture as `webgpu_dawn.dll`. Both DLLs are
copied into each `win-<arch>/native` folder, along with `DXC-LICENSE.TXT` and
`DXC-ThirdPartyNotices.txt`. Dawn searches for DXC next to its own DLL. The pinned
DXC builds its validator into `dxcompiler.dll`; a separate `dxil.dll` is not
required by Dawn's DXC loading path. The legacy FXC path continues using the
system `d3dcompiler_47.dll` when Dawn selects it.

The Windows build uses a static CRT for Dawn, Abseil and DXC. ARM64 builds on
x64 first build host `llvm-tblgen` and `clang-tblgen` tools, then pass their paths
to the ARM64 build so generation never tries to execute an ARM64 host tool.

Sources and build trees are isolated by version in `.native-build/<tag>/`.
An existing checkout must match the tag and contain no tracked modifications.
The CI workflow uses the shorter `<runner.temp>/dn/<version>/` location to
leave room for generated Windows paths. On Windows, the script enables Git's
`core.longpaths` for its process and descendants, including dependency clones,
without changing global Git configuration. If an earlier checkout failed with
`Filename too long`, use a fresh `--work-dir`; incomplete checkouts are still
rejected by the source-integrity check.
To rebuild after changing CMake/compiler options, use a new `--work-dir`.
`--jobs` defaults to 2 to limit compiler memory usage.

Outputs are in `artifacts/dawn/<platform>-<arch>/`: installed headers and
libraries, the shared binary at the top level, and `dawn-native-version.txt`.
macOS produces `libwebgpu_dawn.dylib`, Unix/Android `libwebgpu_dawn.so`,
Windows `webgpu_dawn.dll` plus its import library. iOS and simulator also
produce an ad-hoc signed `webgpu_dawn.framework` per architecture. Device
deployment requires signing with the application's provisioning identity.
GitHub artifacts should be extracted on macOS before using Apple frameworks
and re-signed when packaging the application.

By default the script also replaces libraries in
`Istok.WebGPU.Native.<platform>/runtimes/<rid>/native`.
macOS uses `osx`, iOS uses `ios`, and the simulator uses `iossimulator`, matching
the existing project layout. Building all macOS/simulator architectures combines
the slices with `lipo`; `--arch` copies only the selected slice into that location.
Android, Linux and Windows use architecture-specific RIDs such as `android-arm64`
and `win-x64`. Use `--no-copy-to-packages` to build only into `artifacts/dawn`.
Android and Linux shared libraries are stripped of symbols that are not needed
at runtime before packaging. The build fails early if a resulting binary would
exceed GitHub's 100 MiB per-file limit, including `dxcompiler.dll`.

Verification also checks architectures, all WebGPU exports used by the generated
C# imports, and compatibility of the shared C API subset before compilation.
`verification.json` records the binary hashes, runtime dependency hashes, smoke
results, and (Linux) the highest imported GLIBC version. Packaging rejects
incomplete or changed runtime dependencies before copying any packages.

Desktop builds compile a small C++ consumer into `smoke/`. On a compatible host
it checks loading, instance creation, and GPU adapter/device/buffer creation.
No GPU is an explicit skip; `dawn_smoke --require-adapter` makes it an error.
Windows artifacts also contain `smoke/dxc.exe`: compatible x64 and x86 runners
compile a small HLSL compute shader to Shader Model 6.0 DXIL without needing a
GPU. Cross-built ARM64 executables need a target host for runtime testing.
These checks do not build `Istok.WebGPU` or its examples.

Environment overrides: `MACOS_MIN` (11.0), `IOS_MIN` (14.0),
`ANDROID_API` (24), and standard CMake `CC`/`CXX` on Unix.

The `Build Dawn native libraries` workflow runs this same script across all
architectures. Trigger it manually or by changing `dawn-native-version.txt`/build
scripts. After the entire matrix succeeds, a macOS job assembles all runtime
folders, combines Apple slices and creates/updates one Pull Request on
`automation/update-dawn-native`. The repository must allow GitHub Actions to create
Pull Requests. The `dawn-<platform>-<arch>` artifacts are also downloadable.
