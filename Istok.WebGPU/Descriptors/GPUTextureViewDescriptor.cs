namespace Istok.WebGPU;

public ref struct GPUTextureViewDescriptor()
{
	public string? Label = null;
	public WGPUTextureFormat Format;
	public WGPUTextureViewDimension Dimension;
	public uint BaseMipLevel = 0;
	public uint MipLevelCount = MipLevelCountUndefined;
	public uint BaseArrayLayer = 0;
	public uint ArrayLayerCount = ArrayLayerCountUndefined;
	public WGPUTextureAspect Aspect = WGPUTextureAspect.All;
	public WGPUTextureUsage Usage = WGPUTextureUsage.None;
}