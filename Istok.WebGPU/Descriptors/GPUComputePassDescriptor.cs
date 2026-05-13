namespace Istok.WebGPU;

public ref struct GPUComputePassDescriptor()
{
	public string? Label = null;
	public ref WGPUPassTimestampWrites TimestampWrites;
}