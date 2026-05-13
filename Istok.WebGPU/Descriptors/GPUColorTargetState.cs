namespace Istok.WebGPU;

public struct GPUColorTargetState()
{
	public required WGPUTextureFormat Format;
	public GPUBlendState? Blend;
	public WGPUColorWriteMask WriteMask = WGPUColorWriteMask.All;
}