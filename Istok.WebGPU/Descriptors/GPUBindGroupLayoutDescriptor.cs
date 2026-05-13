namespace Istok.WebGPU;

public ref struct GPUBindGroupLayoutDescriptor()
{
	public string? Label = null;
	public required ReadOnlySpan<WGPUBindGroupLayoutEntry> Entries;
}