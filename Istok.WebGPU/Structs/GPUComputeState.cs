namespace Istok.WebGPU;

public ref struct GPUComputeState()
{
	public required GPUShaderModule Module;
	public string? EntryPoint;
	public Span<WGPUConstantEntry> Constants;
}