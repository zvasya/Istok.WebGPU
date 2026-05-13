namespace Istok.WebGPU;

public unsafe class GPUSampler(WGPUSampler sampler, string? label) : GPUObjectWithName<WGPUSampler>(sampler,  label)
{
	public override void Dispose()
	{
		wgpuSamplerRelease(_handle);
	}
	
	protected override void SetLabel(WGPUStringView label)
	{
		wgpuSamplerSetLabel(_handle,label);
	}
}