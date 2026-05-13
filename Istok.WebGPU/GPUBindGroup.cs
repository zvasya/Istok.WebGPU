namespace Istok.WebGPU;

public unsafe class GPUBindGroup(WGPUBindGroup bindGroup, string? label) : GPUObjectWithName<WGPUBindGroup>(bindGroup, label)
{
	public override void Dispose()
	{
		wgpuBindGroupRelease(_handle);
	}

	protected override void SetLabel(WGPUStringView label)
	{
		wgpuBindGroupSetLabel(_handle,label);
	}
}