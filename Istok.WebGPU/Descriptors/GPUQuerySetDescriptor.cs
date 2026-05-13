namespace Istok.WebGPU;

public ref struct GPUQuerySetDescriptor()
{
	public string? Label = null;
	public required WGPUQueryType Type;
	public required uint Count;
}