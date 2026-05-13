namespace Istok.WebGPU;

public class GPUSurfaceCapabilities
{
	public WGPUTextureFormat[] Formats;
	public WGPUPresentMode[] PresentModes;
	public WGPUCompositeAlphaMode[] AlphaModes;

	public unsafe GPUSurfaceCapabilities(ReadOnlySpan<WGPUTextureFormat> formats, ReadOnlySpan<WGPUPresentMode> presentModes, ReadOnlySpan<WGPUCompositeAlphaMode> alphaModes)
	{
		Formats = formats.ToArray();
		PresentModes = presentModes.ToArray();
		AlphaModes = alphaModes.ToArray();
	}

	public unsafe GPUSurfaceCapabilities(WGPUSurfaceCapabilities capabilities) : this(
		new ReadOnlySpan<WGPUTextureFormat>(capabilities.formats, (int)capabilities.formatCount),
		new ReadOnlySpan<WGPUPresentMode>(capabilities.presentModes, (int)capabilities.presentModeCount),
		new ReadOnlySpan<WGPUCompositeAlphaMode>(capabilities.alphaModes, (int)capabilities.alphaModeCount)
	)
	{
	}

	public static implicit operator GPUSurfaceCapabilities(WGPUSurfaceCapabilities capabilities) => new GPUSurfaceCapabilities(capabilities);
}