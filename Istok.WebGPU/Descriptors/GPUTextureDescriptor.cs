namespace Istok.WebGPU;

public ref struct GPUTextureDescriptor()
{
	public string? Label = null;
	public required WGPUTextureUsage Usage = default;
	public WGPUTextureDimension Dimension = WGPUTextureDimension.D2D;
	public required WGPUExtent3D Size = default;
	public required WGPUTextureFormat Format = default;
	public uint MipLevelCount = 1;
	public uint SampleCount = 1;
	public ReadOnlySpan<WGPUTextureFormat> ViewFormats = [];
}