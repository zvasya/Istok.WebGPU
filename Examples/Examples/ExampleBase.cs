using AssetManager;
using Istok.WebGPU;
using Silk.NET.Maths;
using Istok.WebGPU.View;

namespace Examples;

public abstract unsafe class ExampleBase : IDisposable
{
	protected readonly GPUDevice _device;
	protected readonly IWebGpuView _Window;
	protected readonly GPUSurface _Surface;
	protected readonly GPUSurfaceCapabilities _SurfaceCapabilities;
	protected readonly IResourcesProvider _ResourcesProvider;

	public ExampleBase(GPUDevice device, IWebGpuView window, GPUSurfaceCapabilities surfaceCapabilities, GPUSurface surface, IResourcesProvider resourcesProvider)
	{
		_device = device;
		_Window = window;
		_SurfaceCapabilities = surfaceCapabilities;
		_Surface = surface;
		_ResourcesProvider = resourcesProvider;
	}

	public abstract Task OnLoad();
	public abstract void WindowOnRender(double delta);

	public abstract void FramebufferResize(Vector2D<int> size);

	public abstract void Dispose();
	
	protected (int, int) GetFramebufferSizeInPixel()
	{
		Vector2D<int> windowSize = _Window.FramebufferSize;
		return (windowSize.X, windowSize.Y);
	}

}