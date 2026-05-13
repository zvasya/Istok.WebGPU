// using Istok.WebGPU.LowLevel;
//
// namespace WebGpu;
//
// public unsafe class NativeSurface(WGPUSurface handle, string? label) : GPUObjectWithName<WGPUSurface>(handle, label), GPUSurface
// {
// 	public uint DepthSliceUndefined => WebGPUNative.DepthSliceUndefined;
// 	public override void Dispose()
// 	{
// 		wgpuSurfaceRelease(_handle);
// 	}
//
// 	protected override unsafe void SetLabel(WGPUStringView label)
// 	{
// 		wgpuSurfaceSetLabel(_handle, label);
// 	}
//
// 	public unsafe void Configure(GPUSurfaceConfiguration config)
// 	{
// 		fixed (WGPUTextureFormat* formatPtr = config.ViewFormats)
// 		{
// 			WGPUSurfaceConfiguration wgpuconfig = new WGPUSurfaceConfiguration
// 			{
// 				device = config.Device._handle,
// 				format = config.Format,
// 				usage = config.Usage,
// 				viewFormatCount = (UIntPtr)config.ViewFormats.Length,
// 				viewFormats = formatPtr,
// 				alphaMode = config.AlphaMode,
// 				width = config.Width,
// 				height = config.Height,
// 				presentMode = config.PresentMode
// 			};
// 			wgpuSurfaceConfigure(_handle, &wgpuconfig);
// 		}
// 	}
// 	
// 	public unsafe GPUSurfaceCapabilities GetCapabilities(GPUAdapter adapter)
// 	{
// 		WGPUSurfaceCapabilities capabilities;
// 		wgpuSurfaceGetCapabilities(_handle, adapter._handle, &capabilities);
// 		return capabilities;
// 	}
// 	
// 	public unsafe WGPUSurfaceTexture GetCurrentTexture()
// 	{
// 		WGPUSurfaceTexture surfaceTexture;
// 		wgpuSurfaceGetCurrentTexture(_handle, &surfaceTexture);
// 		return surfaceTexture;
// 	}
// 	
// 	public void Present()
// 	{
// 		wgpuSurfacePresent(_handle);
// 	}
// 	
// 	public void Unconfigure()
// 	{
// 		wgpuSurfaceUnconfigure(_handle);
// 	}
// 	
// 	// public void Reference()
// 	// {
// 	// 	wgpuSurfaceReference(_handle);
// 	// }
//
// 	public void CapabilitiesFreeMembers(WGPUSurfaceCapabilities surfaceCapabilities)
// 	{
// 		wgpuSurfaceCapabilitiesFreeMembers(surfaceCapabilities);
// 	}
// 	
// 	public WGPURequestAdapterOptions RequestAdapterOptions => new WGPURequestAdapterOptions
// 	{
// 		compatibleSurface = _handle,
// 	};
// }