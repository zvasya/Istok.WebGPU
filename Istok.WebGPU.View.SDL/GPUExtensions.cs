using Istok.WebGPU.LowLevel;
using Silk.NET.SDL;
using Silk.NET.Windowing;
using static Istok.WebGPU.LowLevel.WebGPUNative;
	
namespace Istok.WebGPU.View;

public static class GPUExtensions
{
	extension(GPU gpu)
	{
		public unsafe GPUSurface CreateWebGPUSurfaceIOS(IView view)
		{
			Sdl? sdl = Sdl.GetApi();
			WGPUSurfaceDescriptor descriptor = new WGPUSurfaceDescriptor()
			{
				label = WGPUStringView.Empty,
			};
			void* metalView = sdl.MetalCreateView((Silk.NET.SDL.Window*)view.Handle);
			void* layer = sdl.MetalGetLayer(metalView);
        
			WGPUSurfaceSourceMetalLayer descriptorFromMetalLayer = new WGPUSurfaceSourceMetalLayer()
			{
				chain = new ChainedStruct()
				{
					next = null,
					sType = WGPUSType.SurfaceSourceMetalLayer
				},
				layer = layer
			};
			descriptor.nextInChain = (ChainedStruct*) (&descriptorFromMetalLayer);
            
			WGPUSurface surface = wgpuInstanceCreateSurface(gpu.Instance, &descriptor);
			return new GPUSurface(surface, null, false);
		}
	}
}