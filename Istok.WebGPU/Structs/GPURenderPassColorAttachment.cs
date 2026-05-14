namespace Istok.WebGPU;

public ref struct GPURenderPassColorAttachment()
{
	public WGPUTextureView View;
	public uint DepthSlice = DepthSliceUndefined;
	public WGPUTextureView ResolveTarget;
	public WGPULoadOp LoadOp;
	public WGPUStoreOp StoreOp;
	public WGPUColor ClearValue;
	
	
	public static implicit operator WGPURenderPassColorAttachment(GPURenderPassColorAttachment value) =>
		new WGPURenderPassColorAttachment
		{
			view = value.View,
			depthSlice = value.DepthSlice,
			resolveTarget = value.ResolveTarget,
			loadOp = value.LoadOp,
			storeOp = value.StoreOp,
			clearValue = value.ClearValue
		};
}