#include <dawn/webgpu.h>
#include <chrono>
#include <cstdio>
#include <cstring>
#include <thread>

struct Request {
    bool done = false;
    WGPUAdapter adapter = nullptr;
    WGPUDevice device = nullptr;
};

static bool wait(WGPUInstance instance, Request& request) {
    auto deadline = std::chrono::steady_clock::now() + std::chrono::seconds(20);
    while (!request.done && std::chrono::steady_clock::now() < deadline) {
        wgpuInstanceProcessEvents(instance);
        std::this_thread::sleep_for(std::chrono::milliseconds(1));
    }
    return request.done;
}

int main(int argc, char** argv) {
    bool requireAdapter = argc > 1 && std::strcmp(argv[1], "--require-adapter") == 0;
    WGPUInstance instance = wgpuCreateInstance(nullptr);
    if (!instance) return 1;
    Request adapter;
    WGPURequestAdapterCallbackInfo adapterCallback = WGPU_REQUEST_ADAPTER_CALLBACK_INFO_INIT;
    adapterCallback.mode = WGPUCallbackMode_AllowProcessEvents;
    adapterCallback.userdata1 = &adapter;
    adapterCallback.callback = [](WGPURequestAdapterStatus status, WGPUAdapter value,
                                  WGPUStringView, void* userdata, void*) {
        auto& request = *static_cast<Request*>(userdata);
        if (status == WGPURequestAdapterStatus_Success) request.adapter = value;
        request.done = true;
    };
    wgpuInstanceRequestAdapter(instance, nullptr, adapterCallback);
    if (!wait(instance, adapter)) return 2;
    if (!adapter.adapter) {
        std::puts("SKIP: no GPU adapter; library loading and instance creation passed");
        wgpuInstanceRelease(instance);
        return requireAdapter ? 3 : 0;
    }
    Request device;
    WGPURequestDeviceCallbackInfo deviceCallback = WGPU_REQUEST_DEVICE_CALLBACK_INFO_INIT;
    deviceCallback.mode = WGPUCallbackMode_AllowProcessEvents;
    deviceCallback.userdata1 = &device;
    deviceCallback.callback = [](WGPURequestDeviceStatus status, WGPUDevice value,
                                 WGPUStringView, void* userdata, void*) {
        auto& request = *static_cast<Request*>(userdata);
        if (status == WGPURequestDeviceStatus_Success) request.device = value;
        request.done = true;
    };
    wgpuAdapterRequestDevice(adapter.adapter, nullptr, deviceCallback);
    if (!wait(instance, device) || !device.device) return 4;
    WGPUBufferDescriptor descriptor = WGPU_BUFFER_DESCRIPTOR_INIT;
    descriptor.size = 16;
    descriptor.usage = WGPUBufferUsage_CopySrc | WGPUBufferUsage_CopyDst;
    WGPUBuffer buffer = wgpuDeviceCreateBuffer(device.device, &descriptor);
    if (!buffer || wgpuBufferGetSize(buffer) != 16) return 5;
    wgpuBufferRelease(buffer);
    wgpuDeviceRelease(device.device);
    wgpuAdapterRelease(adapter.adapter);
    wgpuInstanceRelease(instance);
    std::puts("PASS: GPU adapter, device and buffer creation");
    return 0;
}
