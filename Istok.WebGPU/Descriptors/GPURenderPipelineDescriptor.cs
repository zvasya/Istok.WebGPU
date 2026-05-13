using System.Runtime.CompilerServices;

namespace Istok.WebGPU;

public ref struct GPURenderPipelineDescriptor()
{
	public string? Label = null;
	public required GPUPipelineLayout Layout;
	public required GPUVertexState Vertex;
	public GPUPrimitiveState Primitive = new GPUPrimitiveState();
	public ref WGPUDepthStencilState DepthStencil = ref Unsafe.NullRef<WGPUDepthStencilState>();
    public GPUMultisampleState Multisample = new GPUMultisampleState();
	public OptionalRef<GPUFragmentState> Fragment;
}