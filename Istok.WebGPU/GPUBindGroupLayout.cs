namespace Istok.WebGPU;

public unsafe class GPUBindGroupLayout(WGPUBindGroupLayout bindGroupLayout, string? label) : GPUObjectWithName<WGPUBindGroupLayout>(bindGroupLayout, label)
{
	public override void Dispose()
	{
		wgpuBindGroupLayoutRelease(_handle);
	}
	
	protected override void SetLabel(WGPUStringView label)
	{
		wgpuBindGroupLayoutSetLabel(_handle,label);
	}
}