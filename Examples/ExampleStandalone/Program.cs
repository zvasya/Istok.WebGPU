using Examples;
using Istok.WebGPU;
using Istok.WebGPU.LowLevel;
using Istok.WebGPU.View;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Window = Silk.NET.Windowing.Window;

namespace ExampleStandalone;

// ReSharper disable once InconsistentNaming
public static unsafe class WebGPUTexturedQuad
{
	private static IWindow _Window = null!;

	private static GPU _Instance;
	private static GPUSurface _Surface;
	private static GPUSurfaceCapabilities _SurfaceCapabilities;
	private static GPUAdapter _Adapter;
	private static GPUDevice _Device;
	private static ExampleBase _Example;

	public static void Main(string[] args)
	{
		//Create window
		WindowOptions options = WindowOptions.Default with
		{
			API = GraphicsAPI.None,
			Size = new Vector2D<int>(800, 600),
			FramesPerSecond = 60,
			UpdatesPerSecond = 60,
			Position = new Vector2D<int>(0, 0),
			Title = "WebGPU Examples",
			IsVisible = true,
			ShouldSwapAutomatically = false,
			IsContextControlDisabled = true,
		};

		_Window = Window.Create(options);

		_Window.Load += WindowOnLoad;
		_Window.Closing += WindowClosing;
		_Window.Update += WindowOnUpdate;

		_Window.Run();
	}

	private static void WindowOnUpdate(double delta)
	{
	}


	private static void WindowOnLoad()
	{
		_Instance = GPU.Create();
		
		_Surface = _Instance.CreateWebGPUSurface(_Window);

		{
			//Get adapter
			var requestAdapterOptions = new WGPURequestAdapterOptions
			{
				compatibleSurface = _Surface.Handle,
			};

			_Adapter = _Instance.RequestAdapter(requestAdapterOptions).Result;

			Console.WriteLine($"Got adapter {(nuint)_Adapter.Handle.Handle:X}");

			PrintAdapterProperties();
			PrintAdapterFeatures();
		} //Get adapter

		_SurfaceCapabilities = _Surface.GetCapabilities(_Adapter);

		{
			//Get device
			GPUDeviceDescriptor deviceDescriptor = new GPUDeviceDescriptor();

			_Device = _Adapter.RequestDevice(deviceDescriptor).Result;

			Console.WriteLine($"Got device {(nuint)_Device.Handle.Handle:X}");
		} //Get device

		IWebGpuView webGpuView = new WebGpuSdlView(_Window);
		_Example = new ExampleCubeMap(_Device, webGpuView, _SurfaceCapabilities, _Surface, new ResourcesProvider());
		_Example.OnLoad().Wait();
		webGpuView.Render += _Example.WindowOnRender;
		webGpuView.FramebufferResize += _Example.FramebufferResize;
	}

	private static void WindowClosing()
	{
		_Example.Dispose();
		_Device.Dispose();
		_Adapter.Dispose();
		_Surface.Dispose();
		_Instance.Dispose();
	}

	static void PrintAdapterProperties()
	{
		var properties = _Adapter.Info;

		Console.WriteLine($"Name: {properties.Device}, Vendor name: {properties.Vendor} backend: {properties.Description} Arch: {properties.Architecture}");
	}

	private static void PrintAdapterFeatures()
	{
		var features = _Adapter.Features;

		Console.WriteLine("Adapter features:");

		for (var i = 0; i < features.Length; i++)
		{
			Console.WriteLine($"\t{features[i]}");
		}
	}
}