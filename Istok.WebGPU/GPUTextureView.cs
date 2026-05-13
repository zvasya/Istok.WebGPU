namespace Istok.WebGPU;

public unsafe class GPUTextureView(WGPUTextureView texture, string? label) : GPUObjectWithName<WGPUTextureView>(texture, label)
{
	public override void Dispose()
	{
		wgpuTextureViewRelease(_handle);
	}
	
	protected override void SetLabel(WGPUStringView labelPtr)
	{
		wgpuTextureViewSetLabel(_handle,labelPtr);
	}
}