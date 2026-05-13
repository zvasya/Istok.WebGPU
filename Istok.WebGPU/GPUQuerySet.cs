namespace Istok.WebGPU;

public unsafe class GPUQuerySet(WGPUQuerySet querySet, string? label) : GPUObjectWithName<WGPUQuerySet>(querySet, label)
{
	public void Destroy()
	{
		wgpuQuerySetDestroy(_handle);
	}

	public WGPUQueryType Type => wgpuQuerySetGetType(_handle);

	public uint Count => wgpuQuerySetGetCount(_handle);
	
	public override void Dispose()
	{
		wgpuQuerySetRelease(_handle);
	}
	
	protected override void SetLabel(WGPUStringView label)
	{
		wgpuQuerySetSetLabel(_handle,label);
	}
}