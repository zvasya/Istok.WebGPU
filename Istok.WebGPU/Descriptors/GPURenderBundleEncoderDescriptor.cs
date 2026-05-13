namespace Istok.WebGPU;

public ref struct GPURenderBundleEncoderDescriptor()
{
	public string? Label = null;
	public required ReadOnlySpan<WGPUTextureFormat> ColorFormats;
	public WGPUTextureFormat DepthStencilFormat;
	public uint SampleCount = 1;
	public bool DepthReadOnly = false;
	public bool StencilReadOnly = false;
}