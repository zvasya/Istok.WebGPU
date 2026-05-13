namespace Istok.WebGPU;

public struct GPUBlendComponent()
{
	public WGPUBlendOperation Operation = WGPUBlendOperation.Add;
	public WGPUBlendFactor SrcFactor = WGPUBlendFactor.Zero;
	public WGPUBlendFactor DstFactor = WGPUBlendFactor.One;
	
	public static implicit operator WGPUBlendComponent(GPUBlendComponent value) =>
		new WGPUBlendComponent
		{
			operation = value.Operation,
			srcFactor = value.SrcFactor,
			dstFactor = value.DstFactor,
		};
}