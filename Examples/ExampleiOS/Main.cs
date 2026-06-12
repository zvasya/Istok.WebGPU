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

    var options = ViewOptions.Default with
    {
        API = GraphicsAPI.None,
        FramesPerSecond = 60,
        UpdatesPerSecond = 60,
        ShouldSwapAutomatically = false,
        IsContextControlDisabled = true,
    };

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
        WGPULimits limits = new WGPULimits { maxStorageBuffersPerShaderStage = 8 };
        var deviceDescriptor = new GPUDeviceDescriptor
        {
            RequiredLimits = ref limits
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