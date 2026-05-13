namespace Istok.WebGPU;

public unsafe class GPURenderPipeline(WGPURenderPipeline renderPipeline, string? label) : GPUObjectWithName<WGPURenderPipeline>(renderPipeline, label)
{
	public GPUBindGroupLayout GetBindGroupLayout(uint index)
	{
		var bindGroupLayout = wgpuRenderPipelineGetBindGroupLayout(_handle, index);
		return new GPUBindGroupLayout(bindGroupLayout, null);
	}
	
	public override void Dispose()
	{
		wgpuRenderPipelineRelease(_handle);
	}
	
	protected override void SetLabel(WGPUStringView label)
	{
		wgpuRenderPipelineSetLabel(_handle,label);
	}
}