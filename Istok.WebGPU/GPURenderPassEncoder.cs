namespace Istok.WebGPU;

public unsafe class GPURenderPassEncoder(WGPURenderPassEncoder renderPassEncoder, string? label) : GPUObjectWithName<WGPURenderPassEncoder>(renderPassEncoder, label)
{
	public void SetViewport(float x, float y, float width, float height, float minDepth, float maxDepth)
	{
		wgpuRenderPassEncoderSetViewport(_handle, x, y, width, height, minDepth, maxDepth);
	}

	public void SetScissorRect(uint x, uint y, uint width, uint height)
	{
		wgpuRenderPassEncoderSetScissorRect(_handle, x, y, width, height);
	}

	public void SetBlendConstant(WGPUColor color)
	{
		wgpuRenderPassEncoderSetBlendConstant(_handle, &color);
	}

	public void SetStencilReference(uint reference)
	{
		wgpuRenderPassEncoderSetStencilReference(_handle, reference);
	}

	public void BeginOcclusionQuery(uint queryIndex)
	{
		wgpuRenderPassEncoderBeginOcclusionQuery(_handle, queryIndex);
	}

	public void EndOcclusionQuery()
	{
		wgpuRenderPassEncoderEndOcclusionQuery(_handle);
	}

	public void ExecuteBundles(ReadOnlySpan<GPURenderBundle> bundles)
	{
		WGPURenderBundle* bundlesPtr = stackalloc WGPURenderBundle[bundles.Length];
		for (var i = 0; i < bundles.Length; i++)
		{
			bundlesPtr[i] = bundles[i]._handle;
		}

		wgpuRenderPassEncoderExecuteBundles(_handle,(UIntPtr)bundles.Length, bundlesPtr);
	}

	public void End()
	{
		wgpuRenderPassEncoderEnd(_handle);
	}

	public void PushDebugGroup(string groupLabel)
	{
		using (groupLabel.ToWGPUStringView(out var groupLabelPtr))
			wgpuRenderPassEncoderPushDebugGroup(_handle, groupLabelPtr);
	}

	public void PopDebugGroup()
	{
		wgpuRenderPassEncoderPopDebugGroup(_handle);
	}

	public void InsertDebugMarker(string markerLabel)
	{
		using (markerLabel.ToWGPUStringView(out var labelPtr))
			wgpuRenderPassEncoderInsertDebugMarker(_handle, labelPtr);
	}

	public void SetBindGroup(uint index, GPUBindGroup bindGroup)
	{
		wgpuRenderPassEncoderSetBindGroup(_handle, index, bindGroup._handle, 0, null);
	}

	public void SetBindGroup(uint index, GPUBindGroup bindGroup, ReadOnlySpan<uint> dynamicOffsets)
	{
		fixed (uint* dynamicOffsetsPtr = dynamicOffsets)
		{
			wgpuRenderPassEncoderSetBindGroup(_handle, index, bindGroup._handle, (UIntPtr)dynamicOffsets.Length, dynamicOffsetsPtr);
		}
	}

	// public void SetBindGroup(uint index, GPUBindGroup? bindGroup, Uint32Array dynamicOffsetsData, ulong dynamicOffsetsDataStart, uint dynamicOffsetsDataLength)
	// {
	// 	
	// }

	public void SetPipeline(GPURenderPipeline pipeline)
	{
		wgpuRenderPassEncoderSetPipeline(_handle, pipeline._handle);
	}

	public void SetIndexBuffer(GPUBuffer buffer, WGPUIndexFormat indexFormat, ulong offset = 0, ulong? size = null)
	{
		wgpuRenderPassEncoderSetIndexBuffer(_handle, buffer._handle, indexFormat, offset, size ?? buffer.Size - offset);
	}

	public void SetVertexBuffer(uint slot, GPUBuffer buffer, ulong offset = 0, ulong? size = null)
	{
		wgpuRenderPassEncoderSetVertexBuffer(_handle, slot, buffer._handle, offset, size ?? buffer.Size - offset);
	}

	public void Draw(uint vertexCount, uint instanceCount = 1, uint firstVertex = 0, uint firstInstance = 0)
	{
		wgpuRenderPassEncoderDraw(_handle, vertexCount, instanceCount, firstVertex, firstInstance);
	}
	
	public void DrawIndexed(uint indexCount,  uint instanceCount = 1,  uint firstIndex = 0,  int baseVertex = 0,  uint firstInstance = 0)
	{
		wgpuRenderPassEncoderDrawIndexed(_handle, indexCount, instanceCount, firstIndex, baseVertex, firstInstance);
	}

	public void DrawIndirect(GPUBuffer indirectBuffer, ulong indirectOffset)
	{
		wgpuRenderPassEncoderDrawIndirect(_handle, indirectBuffer._handle, indirectOffset);
	}
	public void DrawIndexedIndirect(GPUBuffer indirectBuffer, ulong indirectOffset)
	{
		wgpuRenderPassEncoderDrawIndexedIndirect(_handle, indirectBuffer._handle, indirectOffset);
	}

	public void SetImmediates(uint offset, void* data, UIntPtr size)
	{
		wgpuRenderPassEncoderSetImmediates(_handle, offset, data, size);
	}
		
	public void SetImmediates<T>(in T data) where T : unmanaged
	{
		fixed(void* ptr = &data)
			wgpuRenderPassEncoderSetImmediates(_handle, 0, ptr, (UIntPtr)sizeof(T));
	}
	
	public void SetImmediates<T>(in Span<T> data) where T : unmanaged
	{
		fixed(void* ptr = data)
			wgpuRenderPassEncoderSetImmediates(_handle, 0, ptr, (UIntPtr)(sizeof(T) * data.Length));
	}
	
	public override void Dispose()
	{
		wgpuRenderPassEncoderRelease(_handle);
	}
	
	protected override void SetLabel(WGPUStringView label)
	{
		wgpuRenderPassEncoderSetLabel(_handle,label);
	}
}