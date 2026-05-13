using Istok.WebGPU.LowLevel;
using Examples;
using Examples.GpuLife;
using Istok.WebGPU;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl.Android;
using Istok.WebGPU.View;
using Xamarin.Essentials;
using static Istok.WebGPU.LowLevel.WebGPUNative;

namespace ExampleAndroid;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : SilkActivity
{
	GPU _Instance;
	GPUSurface _Surface;
	GPUSurfaceCapabilities _SurfaceCapabilities;
	GPUAdapter _Adapter;
	GPUDevice _Device;
	ExampleBase _Example;
	
	protected override void OnRun()
	{
		const bool colorSrgb = true;
		
		IView window = Silk.NET.Windowing.Window.GetView(ViewOptions.DefaultVulkan); // note also GetView, instead of Window.Create.
		window.Initialize();
		
		_Instance = GPU.Create();
     
		_Surface = _Instance.CreateWebGPUSurface(window);
		
		{
			//Get adapter
			var requestAdapterOptions = new WGPURequestAdapterOptions
			{
				compatibleSurface = _Surface.Handle,
			};
		
			_Adapter = _Instance.RequestAdapter(requestAdapterOptions).Result;
		
			Console.WriteLine($"Got adapter {(nuint)_Adapter.Handle.Handle:X}");
		} //Get adapter
		
		_SurfaceCapabilities = _Surface.GetCapabilities(_Adapter);
		
		{
			//Get device
			var deviceDescriptor = new GPUDeviceDescriptor();
		
			_Device = _Adapter.RequestDevice(deviceDescriptor).Result;
		
			Console.WriteLine($"Got device {(nuint)_Device.Handle.Handle:X}");
		} //Get device
		
		IWebGpuView webGpuView = new WebGpuSdlView(window);
		
		_Example = new ExampleComputeBoids(_Device, webGpuView, _SurfaceCapabilities, _Surface,  new ResourcesProvider());
		_Example.OnLoad().Wait();
		webGpuView.Render += _Example.WindowOnRender;
		webGpuView.FramebufferResize += _Example.FramebufferResize;
		
		window.Run();
	}

}