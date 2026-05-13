namespace Istok.WebGPU;

public ref struct GPURenderPassDescriptor()
{
	public string? Label = null;
	public required ReadOnlySpan<WGPURenderPassColorAttachment> ColorAttachments;
	public ref WGPURenderPassDepthStencilAttachment DepthStencilAttachment;
	public GPUQuerySet? OcclusionQuerySet;
	public ref WGPUPassTimestampWrites TimestampWrites;
}