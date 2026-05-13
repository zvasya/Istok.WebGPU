namespace Istok.WebGPU;

public ref struct GPUDeviceDescriptor()
{
	public string? Label = null;
	public ReadOnlySpan<WGPUFeatureName> RequiredFeatures;
	public ref WGPULimits RequiredLimits;
	public WGPUQueueDescriptor DefaultQueue;
}