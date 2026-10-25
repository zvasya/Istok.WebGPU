# libSkiaSharp with Graphite + Dawn

Official SkiaSharp native binaries are built without `skia_use_dawn`, so
`sk_graphite_context_make_dawn` (`SKGraphiteContext.CreateDawn`) is a stub that
returns `NULL`. These scripts rebuild libSkiaSharp from the exact sources of
SkiaSharp **4.156.0-preview.1.26512.10** with Dawn enabled, and ship Dawn as a
separate shared library (`webgpu_dawn`) so the app and Skia share one
`WGPUInstance`/`WGPUDevice`.

| | Revision |
|---|---|
| SkiaSharp | mono/SkiaSharp `5417ed37972a2945e32fd999f51b277cd1f19803` (release/4.156.0-preview.1) |
| Skia | `zvasya/skia` tag from the repository's `skia-tag.txt` |
| Dawn | revision from `dawn-native-version.txt`, matching Skia's `infra/bots/deps/deps_gen.go` |

The fork already contains the prebuilt shared Dawn support and Apple linker
fixes. No local patch is needed.

## CI automation

`Build SkiaSharp native libraries` (`.github/workflows/build-skia.yml`) runs on
changes to `skia-tag.txt`, `dawn-native-version.txt`, native build scripts or the
workflow, and can be dispatched manually. It builds the same 14 slices as Dawn:
macOS arm64/x64, iOS arm64, simulator arm64/x64, four Android ABIs, Linux
x64/arm64 (manylinux 2.28), Windows x64/x86/arm64.

Each job checks out the exact tag from `zvasya/skia`, syncs dependencies and
builds shared Dawn through `native/dawn/build.py`, using the same settings as
`Istok.WebGPU.Native.*`. Skia links to that Dawn's installed headers and library
(import library on Windows, framework on iOS). No Dawn runtime packages are
modified by this workflow. When updating Skia to a different Dawn revision,
merge the revision change produced by `Update Dawn bindings` first; a mismatch
fails early with instructions to run that workflow.

After the entire matrix succeeds on the default branch, a macOS job verifies
all slices, combines universal libraries, creates/signs iOS frameworks, copies
only SkiaSharp runtimes to the projects below, and creates or updates the PR
branch `automation/update-skia-native`. Enable GitHub Actions permission to
create Pull Requests in the repository settings. PR runs validate and build
without creating another PR.

Artifacts `skia-<platform>-<arch>` contain the library and `verification.json`
with the source tag/commit, Dawn revision, binary hash, architecture and checked
SkiaSharp exports. Linux additionally checks the maximum imported GLIBC version
(2.28). Packaging rejects missing slices, changed files, mixed source revisions,
wrong architectures and binaries at or above GitHub's 100 MiB limit before
replacing package contents.

Run a single CI-style build locally (Python 3.9+, Git, CMake, Ninja and the
platform toolchain are required; Windows additionally needs PowerShell/LLVM):

```sh
python3 native/skiasharp-dawn/build.py macos --arch arm64 --jobs 2
# Other platforms: ios, iossimulator, android, linux, windows; see workflow for dependencies.
# Outputs: artifacts/skia/<platform>-<arch> (no package changes).
# Once all 14 artifacts have been collected on macOS:
python3 native/skiasharp-dawn/package.py
python3 -m unittest discover -s native/skiasharp-dawn/tests -v
```

Use a fresh `--work-dir` when changing tags or toolchain settings; existing
checkouts are checked for the requested tag and tracked modifications.

## Output

Dawn goes into `Istok.WebGPU.Native.<platform>`, libSkiaSharp into
`Istok.SkiaSharp.NativeAssets.<platform>` (which depends on the former):

| Platform | Istok.WebGPU.Native.* | Istok.SkiaSharp.NativeAssets.* |
|---|---|---|
| macOS | `runtimes/osx/native/libwebgpu_dawn.dylib` (arm64 + x86_64) | `runtimes/osx/native/libSkiaSharp.dylib` |
| iOS | `runtimes/{ios,iossimulator}/native/webgpu_dawn.framework` | `runtimes/{ios,iossimulator}/native/libSkiaSharp.framework` |
| Android | `runtimes/android-{arm64,arm,x86,x64}/native/libwebgpu_dawn.so` | `runtimes/android-*/native/libSkiaSharp.so` |
| Linux | `runtimes/linux-{x64,arm64}/native/libwebgpu_dawn.so` | `runtimes/linux-*/native/libSkiaSharp.so` |
| Windows | `runtimes/win-{x64,x86,arm64}/native/webgpu_dawn.dll` | `runtimes/win-*/native/libSkiaSharp.dll` |

Both packages use the same RID folders on purpose: libSkiaSharp finds
libwebgpu_dawn next to itself (`@loader_path` / `$ORIGIN`).

## Build helpers

`build.py` is the entry point for local builds and CI. It prepares the source
and Dawn, then calls `build-skiasharp.sh` (Unix) or `build-windows.ps1` (Windows).
`config.sh` provides the Unix build settings, and `linux/libSkiaSharp.map`
controls Linux exports. `package.py` assembles the verified matrix, while
`Istok.SkiaSharp.NativeAssets.props` provides shared NuGet project settings.

After packaging, build NuGet packages with the .NET SDK from `global.json`:

```sh
dotnet pack Istok.SkiaSharp.NativeAssets.macOS -c Release -o nupkgs
```

## Using from C#

Replace the stock native assets, keep the managed SkiaSharp package:

```xml
<PackageReference Include="SkiaSharp" Version="4.156.0-preview.1.26512.10" />
<PackageReference Include="Istok.SkiaSharp.NativeAssets.macOS" Version="4.156.0-preview.1.26512.10.dawn.1" />
```

`Istok.WebGPU.Native.macOS` comes in as a dependency. Each
`Istok.SkiaSharp.NativeAssets.<platform>` package automatically excludes the
matching stock package's native assets from build and publish outputs, including
Android and iOS packaging. This also works when the Istok package is referenced
transitively. Stock packages may still be downloaded during restore because they
remain dependencies of managed SkiaSharp; no explicit `ExcludeAssets` reference
is needed. Only platforms with an Istok replacement are filtered.

When using `ProjectReference` instead of NuGet, import
`native/skiasharp-dawn/Istok.SkiaSharp.NativeAssets.ProjectReference.targets`
after the project references, as the examples do. NuGet build targets do not flow
through project references. The iOS source example additionally imports the
framework registration targets for SkiaSharp and Dawn.

The WebGPU handles passed to `SKGraphiteContext.CreateDawn` must come from the
same `webgpu_dawn` library that libSkiaSharp is linked against; Istok.WebGPU
P/Invokes `webgpu_dawn` (`@rpath/webgpu_dawn.framework/webgpu_dawn` on iOS).
