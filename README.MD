# WebGPU C# Bindings
A high-performance, cross-platform C# wrapper for WebGPU, providing a unified API for desktop, mobile, and web environments.

The primary goal of this project is to provide a single, consistent API that allows you to write graphics code once and run it everywhere.

## Key Features
* **Unified API:** Identical code paths for Windows, Linux, macOS, iOS, Android, and WebAssembly.

* **Web-Idiomatic Design:** The API is designed to closely mirror the Browser WebGPU API. This includes descriptor naming conventions and default values, making it intuitive for those familiar with the JavaScript/TypeScript standard.

* **Auto-Generated:** Bindings are generated from Google Dawn's `dawn.json` for the Emdawn version supplied by the current `wasm-tools` workload of the .NET SDK selected in `global.json`. The update workflow reads that version from the workload and records it in `emdawn-version.txt` (currently `v20260423.175430`).

## Supported Platforms

| Platform | Backend Implementation |
| --- | --- |
| **Windows, Linux, macOS** | [Google Dawn `e03f1d5`](https://github.com/google/dawn/commit/e03f1d590a8778f8b0d6aa232bf7d7406213fa37) (shared `webgpu_dawn`) |
| **Android, iOS** | [Google Dawn `e03f1d5`](https://github.com/google/dawn/commit/e03f1d590a8778f8b0d6aa232bf7d7406213fa37) (shared `webgpu_dawn`) |
| **Browser (WebAssembly), wasm-tools** | Emdawn `v20260423.175430` from the .NET `wasm-tools` workload |
| **Browser (WebAssembly), SkiaSharp** | Emdawn `v20260624.223603` bundled with `SkiaSharp.NativeAssets.WebAssembly` **4.156.0-preview.1.26512.10** |

Native Dawn is tied to the selected Skia version: the update workflow reads its revision from `infra/bots/deps/deps_gen.go` in the `zvasya/skia` tag specified by `skia-tag.txt` (currently `4.156.0-preview.1-dawn`) and writes it to `dawn-native-version.txt`.

---

## Browser (WebAssembly)

The browser can use either of the following WebGPU implementations:

* **From `wasm-tools`:** Emdawn **v20260423.175430**, matching the version used to generate the bindings. Enable the workload's Emdawn port in the browser project's `.csproj`:

```xml
<PropertyGroup>
    <EmccExtraCFlags>$(EmccExtraCFlags) --use-port=emdawnwebgpu</EmccExtraCFlags>
    <EmccExtraLDFlags>$(EmccExtraLDFlags) --use-port=emdawnwebgpu -sASSERTIONS=0</EmccExtraLDFlags>
</PropertyGroup>
```

* **From `SkiaSharp.NativeAssets.WebAssembly`:** Package **4.156.0-preview.1.26512.10** includes Emdawn **v20260624.223603** (revision `11ab3f92ed451db8c1e1366abcddd9c202e0738d`). The package integrates its bundled WebGPU implementation through its build targets; use this instead of enabling the workload port above. This is the variant used by `Examples/ExampleBrowser`.

---

## Project Architecture

The solution is divided into several layers to ensure flexibility and ease of maintenance:

* **`Istok.WebGPU.LowLevel.Generator`**: A specialized tool that parses Dawn's `dawn.json` definition to generate C# bindings, following the rules of Dawn's `dawn_json_generator.py`.
* **`Istok.WebGPU.LowLevel`**: Contains the raw, auto-generated bindings.
* **`Istok.WebGPU`**: The main high-level library. It wraps the low-level layer to provide the **Web-Idiomatic Design**.
* **`Istok.WebGPU.Native.*`**: Projects (`.Win32`, `.Linux`, `.macOS`, `.Android`, `.iOS`) that bundle the required native binaries for their respective platforms.
* **`Istok.SkiaSharp.NativeAssets.*`**: SkiaSharp **4.156.0-preview.1.26512.10** native libraries built with Graphite + Dawn from the `zvasya/skia` tag in `skia-tag.txt`. Each platform package depends on the matching `Istok.WebGPU.Native.*` package so SkiaSharp and the app share the same `webgpu_dawn` library. NuGet consumers do not need explicit `ExcludeAssets` references: the packages automatically exclude the matching stock native assets from build and publish outputs, including when referenced transitively (see [native/skiasharp-dawn](native/skiasharp-dawn/README.md)).
* **`Istok.WebGPU.View.*`**: (including `.Browser`, `.SDL`) Provides a unified interface for window management, including surface creation and handling display dimensions.

---

## License

This project is available under the MIT License.
