namespace Istok.WebGPU;

public ref struct GPUBindGroupDescriptor()
{
	public string? Label = null;
	public required GPUBindGroupLayout Layout;
	public required ReadOnlySpan<WGPUBindGroupEntry> Entries;
}