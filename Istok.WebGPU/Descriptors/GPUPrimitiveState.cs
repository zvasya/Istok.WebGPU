namespace Istok.WebGPU;

public ref struct GPUPrimitiveState()
{
	public WGPUPrimitiveTopology Topology = WGPUPrimitiveTopology.TriangleList;
	public WGPUIndexFormat StripIndexFormat;
	public WGPUFrontFace FrontFace = WGPUFrontFace.CCW;
	public WGPUCullMode CullMode = WGPUCullMode.None;
	public bool UnclippedDepth = false;
	
	public static implicit operator WGPUPrimitiveState(GPUPrimitiveState value) =>
		new WGPUPrimitiveState
		{
			topology = value.Topology,
			stripIndexFormat = value.StripIndexFormat,
			frontFace = value.FrontFace,
			cullMode = value.CullMode,
			unclippedDepth = value.UnclippedDepth,
		};
}