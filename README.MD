# WebGPU C# Bindings
A high-performance, cross-platform C# wrapper for WebGPU, providing a unified API for desktop, mobile, and web environments.

The primary goal of this project is to provide a single, consistent API that allows you to write graphics code once and run it everywhere.

## Key Features
* **Unified API:** Identical code paths for Windows, Linux, macOS, iOS, Android, and WebAssembly.

* **Web-Idiomatic Design:** The API is designed to closely mirror the Browser WebGPU API. This includes descriptor naming conventions and default values, making it intuitive for those familiar with the JavaScript/TypeScript standard.

* **Auto-Generated:** Bindings are strictly generated from the official webgpu.json specification.

## Supported Platforms

| Platform | Backend Implementation |
| --- | --- |
| **Windows, Linux, macOS** | [wgpu-native v29.0.0.0](https://github.com/gfx-rs/wgpu-native/releases/tag/v29.0.0.0) |
| **Android, iOS** | [wgpu-native v29.0.0.0](https://github.com/gfx-rs/wgpu-native/releases/tag/v29.0.0.0) |
| **Browser (WebAssembly)** | [Google Dawn v20260324.184550](https://github.com/google/dawn/releases/tag/v20260324.184550) |

---

## Browser Native Plugin Build Instructions (WebAssembly)

To update the library for the browser, you must use the Emscripten SDK (**emsdk 3.1.56** for dotnet 10).

1. **Initialize Emscripten:**
```bash
emsdk activate 3.1.56
source ./emsdk_env.sh
```


2. **Compile the Library:**
   Navigate to your Dawn binary package and run:

```bash
    cd [path_to_dawn_bin]/emdawnwebgpu_pkg/webgpu/
    emcc -c src/webgpu.cpp -o wgpu_native.a -I./include -std=c++20 -s WASM=1
```

---

## Project Architecture

The solution is divided into several layers to ensure flexibility and ease of maintenance:

* **`Istok.WebGPU.LowLevel.Generator`**: A specialized tool that parses the `webgpu.json` definition to generate C# bindings.
* **`Istok.WebGPU.LowLevel`**: Contains the raw, auto-generated bindings.
* **`Istok.WebGPU`**: The main high-level library. It wraps the low-level layer to provide the **Web-Idiomatic Design**.
* **`Istok.WebGPU.Native.*`**: Projects (including `.Android`, `.Browser`, `.iOS`) that bundle the required native binaries for their respective platforms.
* **`Istok.WebGPU.View.*`**: (including `.Browser`, `.SDL`) Provides a unified interface for window management, including surface creation and handling display dimensions.

---

## License

This project is available under the MIT License.
