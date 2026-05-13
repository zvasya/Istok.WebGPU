using Silk.NET.Maths;

namespace Istok.WebGPU.View;

public interface IWebGpuView
{
	Vector2D<int> Size { get; }
	Vector2D<int> FramebufferSize { get; }
	void SwapBuffers();

	event Action<double>? Render;
	event Action<Vector2D<int>>? FramebufferResize;
}