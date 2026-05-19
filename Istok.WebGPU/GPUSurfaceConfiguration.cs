namespace Istok.WebGPU;

public ref struct GPUSurfaceConfiguration
{
	public required GPUDevice Device;
	public WGPUTextureFormat Format;
	public WGPUTextureUsage Usage;
	public ReadOnlySpan<WGPUTextureFormat> ViewFormats;
	public WGPUCompositeAlphaMode AlphaMode;
	public uint Width;
	public uint Height;
	public WGPUPresentMode PresentMode;
}