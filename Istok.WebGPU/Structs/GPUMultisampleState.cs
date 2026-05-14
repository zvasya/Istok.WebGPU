namespace Istok.WebGPU;

public ref struct GPUMultisampleState()
{
	public uint Count = 1;
	public uint Mask = ~0u;
	public bool AlphaToCoverageEnabled = false;

	public static implicit operator WGPUMultisampleState(GPUMultisampleState value) =>
		new WGPUMultisampleState
		{
			count = value.Count,
			mask = value.Mask,
			alphaToCoverageEnabled = value.AlphaToCoverageEnabled,
		};
}