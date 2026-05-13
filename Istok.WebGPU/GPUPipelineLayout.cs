namespace Istok.WebGPU;

public unsafe class GPUPipelineLayout(WGPUPipelineLayout pipelineLayout, string? label) : GPUObjectWithName<WGPUPipelineLayout>(pipelineLayout, label)
{
	public static GPUPipelineLayout Auto = new GPUPipelineLayout(WGPUPipelineLayout.Null, null);
	
	public override void Dispose()
	{
		wgpuPipelineLayoutRelease(_handle);
	}
	
	protected override void SetLabel(WGPUStringView label)
	{
		wgpuPipelineLayoutSetLabel(_handle,label);
	}
}