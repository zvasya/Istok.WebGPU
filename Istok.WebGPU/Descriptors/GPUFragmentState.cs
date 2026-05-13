namespace Istok.WebGPU;

public ref struct GPUFragmentState
{
	public GPUShaderModule Module;
	public string EntryPoint;
	public Span<WGPUConstantEntry> Constants;
	public Span<GPUColorTargetState> Targets;
}

