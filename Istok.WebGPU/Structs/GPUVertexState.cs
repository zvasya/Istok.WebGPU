namespace Istok.WebGPU;

public ref struct GPUVertexState()
{
	public required GPUShaderModule Module;
	public string? EntryPoint;
	public Span<WGPUConstantEntry> Constants;
	public Span<WGPUVertexBufferLayout> Buffers;
}