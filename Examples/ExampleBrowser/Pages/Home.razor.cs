using Examples;
using Istok.WebGPU;
using Istok.WebGPU.View;
using Silk.NET.Maths;

namespace ExampleBrowser.Pages;

public partial class Home
{
    private WebGPUCanvas CanvasView;
    private Vector2D<int> Size;
        
    private GPU _Instance = null!;
    private GPUSurface _Surface = null!;
    private GPUSurfaceCapabilities _SurfaceCapabilities;
    private GPUAdapter _Adapter;
    private GPUDevice _Device;
    private ExampleBase _Example;
        
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await CanvasView.Initialize();
                
            Logger.LogInformation($"Try get Instance");
            _Instance = GPU.Create();
            Logger.LogInformation($"Get Instance: {_Instance.InstanceToString()}");
            Logger.LogInformation($"Try get Surface");
            _Surface = _Instance.CreateWebGPUSurfaceBrowser();
            Logger.LogInformation($"Get Surface: {(UIntPtr)_Surface.Handle.Handle}");
            {
                //Get adapter\
                var requestAdapterOptions = _Surface.RequestAdapterOptions;

                _Adapter = await _Instance.RequestAdapter(requestAdapterOptions);

                Logger.LogInformation($"Got adapter {(UIntPtr)_Adapter.Handle.Handle}");

            } //Get adapter
            Logger.LogInformation($"Try get SurfaceCapabilities");
                
            _SurfaceCapabilities = _Surface.GetCapabilities(_Adapter);
                
                
            Logger.LogInformation($"Got SurfaceCapabilities Formats [{string.Join(" ,", _SurfaceCapabilities.Formats)}] PresentModes [{string.Join(" ,", _SurfaceCapabilities.PresentModes)}] AlphaModes [{string.Join(" ,", _SurfaceCapabilities.AlphaModes)}]");
            {
                //Get device
                var deviceDescriptor = new GPUDeviceDescriptor
                {
                };
                Logger.LogInformation($"Try get Device");
                _Device = await _Adapter.RequestDevice(deviceDescriptor);

                Logger.LogInformation($"Got device {_Device.Handle.Handle}");

            } //Get device
                
            _Example = new ExampleComputeBoids(_Device, CanvasView, _SurfaceCapabilities, _Surface, new ResourcesProvider(Http));
            await _Example.OnLoad();
            Logger.LogInformation($"Loaded");
            CanvasView.Render += _Example.WindowOnRender;
            CanvasView.FramebufferResize += _Example.FramebufferResize;
            Logger.LogInformation($"Subscribed");
            Logger.LogInformation($"Started");
        }
    }
        
        
    void PrintAdapterProperties()
    {
        var properties = _Adapter.Info;

        Logger.LogInformation($"Name: {properties.Device}, Vendor name: {properties.Vendor} backend: {properties.Description} Arch: {properties.Architecture}");
    }

    private void PrintAdapterFeatures()
    {
        var features = _Adapter.Features;

        Logger.LogInformation("Adapter features:");

        for (var i = 0; i < features.Length; i++)
        {
            Logger.LogInformation($"\t{features[i]}");
        }
    }
}