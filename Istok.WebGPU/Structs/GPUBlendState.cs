namespace Istok.WebGPU;

public struct GPUBlendState()
{
	public required GPUBlendComponent Color;
	public required GPUBlendComponent Alpha;
	
	public static implicit operator WGPUBlendState(GPUBlendState value) =>
		new WGPUBlendState
		{
			color = value.Color,
			alpha = value.Alpha,
		};
}