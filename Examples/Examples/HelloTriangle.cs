using AssetManager;
using Istok.WebGPU;
using Istok.WebGPU.LowLevel;
using Silk.NET.Maths;
using Istok.WebGPU.View;
using static Istok.WebGPU.LowLevel.WebGPUNative;

namespace Examples;

public class HelloTriangle : ExampleBase
{
	public HelloTriangle(GPUDevice device, IWebGpuView window, GPUSurfaceCapabilities surfaceCapabilities, GPUSurface surface, IResourcesProvider resourcesProvider) : base(device, window, surfaceCapabilities, surface, resourcesProvider)
	{
	}

	GPURenderPipeline pipeline;
	public override async Task OnLoad()
	{
		var triangleVertWGSL = await _ResourcesProvider.LoadTextAsync("WebGPUSamples/Shaders/triangle.vert.wgsl");
		var redFragWGSL = await _ResourcesProvider.LoadTextAsync("WebGPUSamples/Shaders/red.frag.wgsl");
		
		pipeline = _device.CreateRenderPipeline(new GPURenderPipelineDescriptor()
		{
			Layout = GPUPipelineLayout.Auto,
			Vertex = new GPUVertexState
			{
				Module = _device.CreateShaderModule(new GPUShaderModuleDescriptor()
				{
					Code = triangleVertWGSL
				}),
			},
			Fragment = new GPUFragmentState()
			{
				Module = _device.CreateShaderModule(new GPUShaderModuleDescriptor()
				{
					Code = redFragWGSL
				}),
				Targets =
				[
					new GPUColorTargetState
					{
						Format = _SurfaceCapabilities.Formats[0]
					}
				],
			},
			Primitive = new GPUPrimitiveState()
			{
				Topology = WGPUPrimitiveTopology.TriangleList
			},
		});

		CreateSwapchain();
	}

	public override void WindowOnRender(double delta)
	{
		WGPUSurfaceTexture currentSurfaceTexture = _Surface.GetCurrentTexture();
		switch (currentSurfaceTexture.status)
		{
			case WGPUSurfaceGetCurrentTextureStatus.SuccessOptimal:
			case WGPUSurfaceGetCurrentTextureStatus.SuccessSuboptimal:
				break;
            
			case WGPUSurfaceGetCurrentTextureStatus.Timeout:
			case WGPUSurfaceGetCurrentTextureStatus.Outdated:
			case WGPUSurfaceGetCurrentTextureStatus.Lost:
				// Recreate swapchain,
				wgpuTextureRelease(currentSurfaceTexture.texture);
				CreateSwapchain();
				// Skip this frame
				return;
			case WGPUSurfaceGetCurrentTextureStatus.Error:
			default:
				// Recreate swapchain,
				// wgpuTextureRelease(surfaceTexture.texture);
				// CreateSwapchain();
				// Skip this frame
				return;
		}

		var commandEncoder = _device.CreateCommandEncoder();
		
		var surfaceTexture = new GPUTexture(currentSurfaceTexture.texture, null);
		
		var textureView = surfaceTexture.CreateView();

		var renderPassDescriptor = new GPURenderPassDescriptor() 
		{
			ColorAttachments = [ new GPURenderPassColorAttachment() 
			{
				View = textureView,
				ClearValue = new WGPUColor { r = 0, g = 1, b = 0, a = 0 }, // Clear to transparent
				LoadOp = WGPULoadOp.Clear,
				StoreOp = WGPUStoreOp.Store,
			},
			],
		};

		var passEncoder = commandEncoder.BeginRenderPass(renderPassDescriptor);
		passEncoder.SetPipeline(pipeline);
		passEncoder.Draw(3);
		passEncoder.End();

		_device.Queue.Submit(commandEncoder.Finish());
		
		_Surface.Present();
		_Window.SwapBuffers();
	}

	public override void FramebufferResize(Vector2D<int> size)
	{
		CreateSwapchain();
	}

	public override void Dispose()
	{
		pipeline.Dispose();
	}
	
	private void CreateSwapchain()
	{
		int w = 0;
		int h = 0;
		(w,h) = GetFramebufferSizeInPixel();
		Console.WriteLine($"GetFramebufferSizeInPixel {w}, {h}");
		var surfaceConfiguration = new GPUSurfaceConfiguration
		{
			Usage = WGPUTextureUsage.RenderAttachment,
			Device = _device,
			Format = _SurfaceCapabilities.Formats[0],
			// PresentMode = PresentMode.FifoRelaxed,
			PresentMode = WGPUPresentMode.Fifo,
			AlphaMode = _SurfaceCapabilities.AlphaModes[0],
			Width = (uint) w,
			Height = (uint) h,
		};
        
		_Surface.Configure(surfaceConfiguration);
		Console.WriteLine($"Surface Configured");
	}
}