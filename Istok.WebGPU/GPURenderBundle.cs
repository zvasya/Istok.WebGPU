namespace Istok.WebGPU;

public unsafe class GPURenderBundle(WGPURenderBundle renderBundle, string? label) : GPUObjectWithName<WGPURenderBundle>(renderBundle, label)
{
	public override void Dispose()
	{
		wgpuRenderBundleRelease(_handle);
	}
	
	protected override void SetLabel(WGPUStringView label)
	{
		wgpuRenderBundleSetLabel(_handle,label);
	}
}