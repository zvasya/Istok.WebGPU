using ExampleiOS;
using Examples;
using Examples.GpuLife;
using Istok.WebGPU;
using Istok.WebGPU.LowLevel;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl.iOS;
using Istok.WebGPU.View;
using static Istok.WebGPU.LowLevel.WebGPUNative;
using Window = Silk.NET.Windowing.Window;

GPU _Instance;
GPUSurface _Surface;
GPUSurfaceCapabilities _SurfaceCapabilities;
GPUAdapter _Adapter;
GPUDevice _Device;
ExampleBase _Example;

Console.WriteLine("Hello World!");
// This is the main entry point of the application.
// If you want to use a different Application Delegate class from "AppDelegate"
// you can specify it here.
SilkMobile.RunApp([], strings =>
{
    const bool colorSrgb = true;

    Window.PrioritizeSdl();

    var options = ViewOptions.Default;
    options.API = GraphicsAPI.None;
    options.FramesPerSecond = 60;
    options.UpdatesPerSecond = 60;
    options.ShouldSwapAutomatically = false;
    options.IsContextControlDisabled = true;

    IView window = Window.GetView(options);
    window.Initialize();

    _Instance = GPU.Create();
    
    _Surface = _Instance.CreateWebGPUSurfaceIOS(window);

    {
        //Get adapter
        var requestAdapterOptions = new WGPURequestAdapterOptions
        {
            compatibleSurface = _Surface.Handle,
        };

        _Adapter = _Instance.RequestAdapter(requestAdapterOptions).Result;

        Console.WriteLine($"Got adapter {(nuint)_Adapter.Handle.Handle:X}");

        // PrintAdapterProperties();
        // PrintAdapterFeatures();
    } //Get adapter

    _SurfaceCapabilities = _Surface.GetCapabilities(_Adapter);

    {
        //Get device
        WGPULimits limits = new WGPULimits
        {
            maxTextureDimension1D = LimitU32Undefined,
            maxTextureDimension2D = LimitU32Undefined,
            maxTextureDimension3D = LimitU32Undefined,
            maxTextureArrayLayers = LimitU32Undefined,
            maxBindGroups = LimitU32Undefined,
            maxBindGroupsPlusVertexBuffers = LimitU32Undefined,
            maxBindingsPerBindGroup = LimitU32Undefined,
            maxDynamicUniformBuffersPerPipelineLayout = LimitU32Undefined,
            maxDynamicStorageBuffersPerPipelineLayout = LimitU32Undefined,
            maxSampledTexturesPerShaderStage = LimitU32Undefined,
            maxSamplersPerShaderStage = LimitU32Undefined,
            maxStorageBuffersPerShaderStage = 8,
            maxStorageTexturesPerShaderStage = LimitU32Undefined,
            maxUniformBuffersPerShaderStage = LimitU32Undefined,
            maxUniformBufferBindingSize = LimitU64Undefined,
            maxStorageBufferBindingSize = LimitU64Undefined,
            minUniformBufferOffsetAlignment = LimitU32Undefined,
            minStorageBufferOffsetAlignment = LimitU32Undefined,
            maxVertexBuffers = LimitU32Undefined,
            maxBufferSize = LimitU64Undefined,
            maxVertexAttributes = LimitU32Undefined,
            maxVertexBufferArrayStride = LimitU32Undefined,
            maxInterStageShaderVariables = LimitU32Undefined,
            maxColorAttachments = LimitU32Undefined,
            maxColorAttachmentBytesPerSample = LimitU32Undefined,
            maxComputeWorkgroupStorageSize = LimitU32Undefined,
            maxComputeInvocationsPerWorkgroup = LimitU32Undefined,
            maxComputeWorkgroupSizeX = LimitU32Undefined,
            maxComputeWorkgroupSizeY = LimitU32Undefined,
            maxComputeWorkgroupSizeZ = LimitU32Undefined,
            maxComputeWorkgroupsPerDimension = LimitU32Undefined,
            maxImmediateSize = LimitU32Undefined,
        };
        var deviceDescriptor = new GPUDeviceDescriptor
        {
            RequiredLimits = ref limits
            // DeviceLostCallback = new PfnDeviceLostCallback((delegate* unmanaged[Cdecl]<DeviceLostReason, byte*, void*, void>)SilkMarshal.DelegateToPtr(DeviceLost, DelegatePointerKind.Passthrough)),
            // deviceLostCallback = PfnDeviceLostCallback.Create(DeviceLost),
        };

        _Device = _Adapter.RequestDevice(deviceDescriptor).Result;

        Console.WriteLine($"Got device {(nuint)_Device.Handle.Handle:X}");
    } //Get device

    IWebGpuView webGpuView = new WebGpuSdlView(window);

    _Example = new GpuLife(_Device, webGpuView, _SurfaceCapabilities, _Surface, new ResourcesProvider());
    _Example.OnLoad().Wait();
    webGpuView.Render += _Example.WindowOnRender;
    webGpuView.FramebufferResize += _Example.FramebufferResize;
    
    window.Run();
});