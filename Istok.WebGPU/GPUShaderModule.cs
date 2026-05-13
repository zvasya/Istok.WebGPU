namespace Istok.WebGPU;

public unsafe class GPUShaderModule(WGPUShaderModule shaderModule, string? label) : GPUObjectWithName<WGPUShaderModule>(shaderModule, label)
{
	public Task<WGPUCompilationInfo> GetCompilationInfo()
	{
		return Requests.CompilationInfo.Request(this).ContinueWith(r => r.Result.info);
	}
	
	public override void Dispose()
	{
		wgpuShaderModuleRelease(_handle);
	}
	
	protected override void SetLabel(WGPUStringView label)
	{
		wgpuShaderModuleSetLabel(_handle,label);
	}
}