namespace Istok.WebGPU;

public ref struct GPUSamplerDescriptor()
{
	public string? Label = null;
	public WGPUAddressMode AddressModeU = WGPUAddressMode.ClampToEdge;
	public WGPUAddressMode AddressModeV = WGPUAddressMode.ClampToEdge;
	public WGPUAddressMode AddressModeW = WGPUAddressMode.ClampToEdge;
	public WGPUFilterMode MagFilter = WGPUFilterMode.Nearest;
	public WGPUFilterMode MinFilter = WGPUFilterMode.Nearest;
	public WGPUMipmapFilterMode MipmapFilter = WGPUMipmapFilterMode.Nearest;
	public float LodMinClamp = 0;
	public float LodMaxClamp = 32;
	public WGPUCompareFunction Compare = default;
	public ushort MaxAnisotropy = 1;
}