namespace Istok.WebGPU;

public unsafe class GPUCommandBuffer(WGPUCommandBuffer commandBuffer, string? label) : GPUObjectWithName<WGPUCommandBuffer>(commandBuffer, label)
{
	public override void Dispose()
	{
		wgpuCommandBufferRelease(_handle);
	}
	
	protected override void SetLabel(WGPUStringView label)
	{
		wgpuCommandBufferSetLabel(_handle,label);
	}
}