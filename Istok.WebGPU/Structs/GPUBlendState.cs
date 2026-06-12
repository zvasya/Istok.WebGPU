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

	public static GPUBlendState Opaque => new GPUBlendState()
	{
		Color = new GPUBlendComponent
		{
			Operation = WGPUBlendOperation.Add,
			SrcFactor = WGPUBlendFactor.One,
			DstFactor = WGPUBlendFactor.Zero
		},
		Alpha = new GPUBlendComponent
		{
			Operation = WGPUBlendOperation.Add,
			SrcFactor = WGPUBlendFactor.One,
			DstFactor = WGPUBlendFactor.Zero
		}
	};

	public static GPUBlendState Transparency => new GPUBlendState()
	{
		Color = new GPUBlendComponent
		{
			Operation = WGPUBlendOperation.Add,
			SrcFactor = WGPUBlendFactor.SrcAlpha,
			DstFactor = WGPUBlendFactor.OneMinusSrcAlpha
		},
		Alpha = new GPUBlendComponent
		{
			Operation = WGPUBlendOperation.Add,
			SrcFactor = WGPUBlendFactor.One,
			DstFactor = WGPUBlendFactor.OneMinusSrcAlpha
		}
	};

	public static GPUBlendState PremultipliedAlpha => new GPUBlendState()
	{
		Color = new GPUBlendComponent
		{
			Operation = WGPUBlendOperation.Add,
			SrcFactor = WGPUBlendFactor.One,
			DstFactor = WGPUBlendFactor.OneMinusSrcAlpha
		},
		Alpha = new GPUBlendComponent
		{
			Operation = WGPUBlendOperation.Add,
			SrcFactor = WGPUBlendFactor.One,
			DstFactor = WGPUBlendFactor.OneMinusSrcAlpha
		}
	};

	public static GPUBlendState Additive => new GPUBlendState()
	{
		Color = new GPUBlendComponent
		{
			Operation = WGPUBlendOperation.Add,
			SrcFactor = WGPUBlendFactor.SrcAlpha,
			DstFactor = WGPUBlendFactor.One
		},
		Alpha = new GPUBlendComponent
		{
			Operation = WGPUBlendOperation.Add,
			SrcFactor = WGPUBlendFactor.Zero,
			DstFactor = WGPUBlendFactor.One
		}
	};
}
