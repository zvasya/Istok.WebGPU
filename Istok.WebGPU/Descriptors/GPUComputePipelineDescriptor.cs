namespace Istok.WebGPU;

public ref struct GPUComputePipelineDescriptor()
{
	public string? Label = null;
	public required GPUPipelineLayout Layout;
	public required GPUComputeState Compute;
}