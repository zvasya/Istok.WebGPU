namespace Istok.WebGPU;

public ref struct GPUShaderModuleDescriptor()
{
	public string? Label = null;
	public required string Code;
}