using Istok.WebGPU.LowLevel;
using static Istok.WebGPU.LowLevel.WebGPUNative;

namespace Istok.WebGPU.View;

public static class GPUExtensions
{
	extension(GPU gpu)
	{
		public unsafe GPUSurface CreateWebGPUSurfaceBrowser()
		{
			WGPUStringViewExtension.Scope scope = default;
			WGPUSurfaceDescriptor descriptor = new WGPUSurfaceDescriptor()
			{
				label = new WGPUStringView()
				{
					data = null,
					length = Strlen
				}
			};

			scope = "canvas".ToWGPUStringView(out var selectorPtr);
			WGPUSurfaceSourceCanvasHTMLSelector canvasHtmlSelector = new WGPUSurfaceSourceCanvasHTMLSelector()
			{
				chain = new ChainedStruct()
				{
					next = null,
					sType = (WGPUSType)0x40000
				},
				selector = selectorPtr
			};
			descriptor.nextInChain = (ChainedStruct*)&canvasHtmlSelector;
			WGPUSurface surface = wgpuInstanceCreateSurface(gpu.Instance, &descriptor);

			scope.Dispose();

			return new GPUSurface(surface, null, true);
		}
	}
}