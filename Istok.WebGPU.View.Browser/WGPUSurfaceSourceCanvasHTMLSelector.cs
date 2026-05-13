using System.Runtime.InteropServices;
using Istok.WebGPU.LowLevel;
using static Istok.WebGPU.LowLevel.WebGPUNative;

namespace Istok.WebGPU.View;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSurfaceSourceCanvasHTMLSelector
{
	public ChainedStruct chain;
	public WGPUStringView selector;
}