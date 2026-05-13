namespace Istok.WebGPU;

public ref struct GPUBufferDescriptor()
{
	public string? Label = null;
	public required WGPUBufferUsage Usage = WGPUBufferUsage.None;
	public required ulong Size = 0;
	public bool MappedAtCreation = false;
}