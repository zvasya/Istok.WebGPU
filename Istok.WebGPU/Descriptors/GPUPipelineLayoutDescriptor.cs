namespace Istok.WebGPU;

public ref struct GPUPipelineLayoutDescriptor()
{
	public string? Label = null;
	public required ReadOnlySpan<GPUBindGroupLayout> BindGroupLayouts;
	public uint ImmediateSize = 0;
}