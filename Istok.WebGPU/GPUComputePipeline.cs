namespace Istok.WebGPU;

public unsafe class GPUComputePipeline(WGPUComputePipeline computePipeline, string? label) : GPUObjectWithName<WGPUComputePipeline>(computePipeline, label)
{
	public GPUBindGroupLayout GetBindGroupLayout(uint index)
	{
		var bindGroupLayout = wgpuComputePipelineGetBindGroupLayout(_handle, index);
		return new GPUBindGroupLayout(bindGroupLayout, null);
	}

	public override void Dispose()
	{
		wgpuComputePipelineRelease(_handle);
	}
	
	protected override void SetLabel(WGPUStringView label)
	{
		wgpuComputePipelineSetLabel(_handle,label);
	}
}