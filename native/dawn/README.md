# Native Dawn builds

`build.py` reads the release tag from the repository's `dawn-version.txt`,
clones that tag from `https://github.com/google/dawn`, and builds
`DAWN_BUILD_MONOLITHIC_LIBRARY=SHARED` with `BUILD_SHARED_LIBS=OFF`.
CMake downloads Dawn's pinned dependencies. No Skia checkout is required.

Requirements: Python 3.9+, Git, CMake 3.30+ and a C++ compiler.
Unix builds also require Ninja. Apple builds require full Xcode selected with
`xcode-select`; Windows builds require Visual Studio 2022 with Desktop C++,
Windows SDK and the x86/ARM64 tools for the requested architectures.
Android builds require NDK r28 (`ANDROID_NDK_HOME` or `ANDROID_NDK_ROOT`).
Linux builds require X11, EGL, Wayland and xkbcommon development packages;
the workflow lists the Ubuntu packages.

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

Sources and build trees are isolated by version in `.native-build/<tag>/`.
An existing checkout must match the tag and contain no tracked modifications.
To rebuild after changing CMake/compiler options, use a new `--work-dir`.
`--jobs` defaults to 2 to limit compiler memory usage.

Outputs are in `artifacts/dawn/<platform>-<arch>/`: installed headers and
libraries, the shared binary at the top level, and `dawn-version.txt`.
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

Environment overrides: `MACOS_MIN` (11.0), `IOS_MIN` (14.0),
`ANDROID_API` (24), and standard CMake `CC`/`CXX` on Unix.

The `Build Dawn native libraries` workflow runs this same script across all
architectures. Trigger it manually or by changing `dawn-version.txt`/build
scripts. After the entire matrix succeeds, a macOS job assembles all runtime
folders, combines Apple slices and creates/updates one Pull Request on
`codex/update-dawn-native`. The repository must allow GitHub Actions to create
Pull Requests. The `dawn-<platform>-<arch>` artifacts are also downloadable.
