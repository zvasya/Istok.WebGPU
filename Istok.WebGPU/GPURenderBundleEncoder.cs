namespace Istok.WebGPU;

public unsafe class GPURenderBundleEncoder(WGPURenderBundleEncoder renderBundleEncoder, string? label) : GPUObjectWithName<WGPURenderBundleEncoder>(renderBundleEncoder, label)
{
	public GPURenderBundle Finish()
	{
		var renderBundle = wgpuRenderBundleEncoderFinish(_handle, null);
		return new GPURenderBundle(renderBundle, null);
	}

	public GPURenderBundle Finish(GPURenderBundleDescriptor descriptor)
	{
		using (descriptor.Label.ToWGPUStringView(out var descriptorLabelPtr))
		{
			WGPURenderBundleDescriptor renderBundleDescriptor = new WGPURenderBundleDescriptor() with
			{
				label = descriptorLabelPtr,
			};
			WGPURenderBundle renderBundle = wgpuRenderBundleEncoderFinish(_handle, &renderBundleDescriptor);
			return new GPURenderBundle(renderBundle, null);
		}
	}

	public void PushDebugGroup(string groupLabel)
	{
		using (groupLabel.ToWGPUStringView(out var groupLabelPtr))
			wgpuRenderBundleEncoderPushDebugGroup(_handle, groupLabelPtr);
	}

	public void PopDebugGroup()
	{
		wgpuRenderBundleEncoderPopDebugGroup(_handle);
	}

	public void InsertDebugMarker(string markerLabel)
	{
		using (markerLabel.ToWGPUStringView(out var markerLabelPtr))
			wgpuRenderBundleEncoderInsertDebugMarker(_handle, markerLabelPtr);
	}

	public void SetBindGroup(uint index, GPUBindGroup bindGroup)
	{
		wgpuRenderBundleEncoderSetBindGroup(_handle, index, bindGroup._handle, 0, null);
	}

	public void SetBindGroup(uint index, GPUBindGroup bindGroup, ReadOnlySpan<uint> dynamicOffsets)
	{
		fixed (uint* dynamicOffsetsPtr = dynamicOffsets)
		{
			wgpuRenderBundleEncoderSetBindGroup(_handle, index, bindGroup._handle, (UIntPtr)dynamicOffsets.Length, dynamicOffsetsPtr);
		}
	}

	// public void SetBindGroup(uint index, GPUBindGroup? bindGroup, Uint32Array dynamicOffsetsData, ulong dynamicOffsetsDataStart, uint dynamicOffsetsDataLength)
	// {
	// 	wgpuRenderBundleEncoderSetBindGroup(_handle, index, );
	// }

	public void SetPipeline(GPURenderPipeline pipeline)
	{
		wgpuRenderBundleEncoderSetPipeline(_handle, pipeline._handle);
	}

	public void SetIndexBuffer(GPUBuffer buffer, WGPUIndexFormat indexFormat, uint offset = 0, uint? size = null)
	{
		wgpuRenderBundleEncoderSetIndexBuffer(_handle, buffer._handle, indexFormat, offset, size ?? buffer.Size - offset);
	}
	
	public void SetVertexBuffer(uint slot, GPUBuffer buffer, uint offset = 0, uint? size = null)
	{
		wgpuRenderBundleEncoderSetVertexBuffer(_handle, slot, buffer._handle, offset, size ?? buffer.Size - offset);
	}

	public void Draw(uint vertexCount, uint instanceCount = 1, uint firstVertex = 0, uint firstInstance = 0)
	{
		wgpuRenderBundleEncoderDraw(_handle, vertexCount, instanceCount, firstVertex, firstInstance);
	}
	
	public void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int baseVertex = 0, uint firstInstance = 0)
	{
		wgpuRenderBundleEncoderDrawIndexed(_handle, indexCount, instanceCount, firstIndex, baseVertex, firstInstance);
	}

	public void DrawIndirect(GPUBuffer indirectBuffer, ulong indirectOffset)
	{
		wgpuRenderBundleEncoderDrawIndirect(_handle, indirectBuffer._handle, indirectOffset);
	}

	public void DrawIndexedIndirect(GPUBuffer indirectBuffer, ulong indirectOffset)
	{
		wgpuRenderBundleEncoderDrawIndexedIndirect(_handle, indirectBuffer._handle, indirectOffset);
	}
	
	public void SetImmediates(uint offset, void* data, UIntPtr size)
	{
		wgpuRenderBundleEncoderSetImmediates(_handle, offset, data, size);
	}
		
	public void SetImmediates<T>(in T data) where T : unmanaged
	{
		fixed(void* ptr = &data)
			wgpuRenderBundleEncoderSetImmediates(_handle, 0, ptr, (UIntPtr)sizeof(T));
	}
	
	public void SetImmediates<T>(in Span<T> data) where T : unmanaged
	{
		fixed(void* ptr = data)
			wgpuRenderBundleEncoderSetImmediates(_handle, 0, ptr, (UIntPtr)(sizeof(T) * data.Length));
	}
	
	public override void Dispose()
	{
		wgpuRenderBundleEncoderRelease(_handle);
	}
	
	protected override void SetLabel(WGPUStringView label)
	{
		wgpuRenderBundleEncoderSetLabel(_handle,label);
	}
}