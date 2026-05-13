using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Istok.WebGPU.View;

public class WebGpuSdlView(IView view) : IWebGpuView
{
	public Vector2D<int> Size => view.Size;
	public Vector2D<int> FramebufferSize => view.FramebufferSize;
	public void SwapBuffers()
	{
		view.SwapBuffers();
	}
	
	public event Action<double>? Render
	{
		add => view.Render += value;
		remove => view.Render -= value;
	}
	
	public event Action<Vector2D<int>>? FramebufferResize
	{
		add => view.FramebufferResize += value;
		remove => view.FramebufferResize -= value;
	}
}