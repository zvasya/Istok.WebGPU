namespace Istok.WebGPU;

public unsafe class GPUComputePassEncoder(WGPUComputePassEncoder computePassEncoder, string? label) : GPUObjectWithName<WGPUComputePassEncoder>(computePassEncoder, label)
{
	public void SetPipeline(GPUComputePipeline pipeline)
	{
		wgpuComputePassEncoderSetPipeline(_handle, pipeline._handle);
	}

	public void DispatchWorkgroups(uint workgroupCountX, uint workgroupCountY = 1, uint workgroupCountZ = 1)
	{
		wgpuComputePassEncoderDispatchWorkgroups(_handle, workgroupCountX, workgroupCountY, workgroupCountZ);
	}

	public void DispatchWorkgroupsIndirect(GPUBuffer indirectBuffer, ulong indirectOffset)
	{
		wgpuComputePassEncoderDispatchWorkgroupsIndirect(_handle, indirectBuffer._handle, indirectOffset);
	}

	public void End()
	{
		wgpuComputePassEncoderEnd(_handle);
	}

	public void PushDebugGroup(string groupLabel)
	{
		using(groupLabel.ToWGPUStringView(out WGPUStringView groupLabelPtr))
			wgpuComputePassEncoderPushDebugGroup(_handle, groupLabelPtr);
	}

	public void PopDebugGroup()
	{
		wgpuComputePassEncoderPopDebugGroup(_handle);
	}

	public void InsertDebugMarker(string markerLabel)
	{
		using (markerLabel.ToWGPUStringView(out WGPUStringView markerLabelPtr))
			wgpuComputePassEncoderInsertDebugMarker(_handle, markerLabelPtr);
	}

	public void SetBindGroup(uint index, GPUBindGroup bindGroup)
	{
		wgpuComputePassEncoderSetBindGroup(_handle, index, bindGroup._handle, 0, null);
	}

	public void SetBindGroup(uint index, GPUBindGroup bindGroup, ReadOnlySpan<uint> dynamicOffsets)
	{
		fixed (uint* dynamicOffsetsPtr = dynamicOffsets)
		{
			wgpuComputePassEncoderSetBindGroup(_handle, index, bindGroup._handle, (nuint)dynamicOffsets.Length, dynamicOffsetsPtr);
		}
	}

	public void SetImmediates(uint offset, void* data, UIntPtr size)
	{
		wgpuComputePassEncoderSetImmediates(_handle, offset, data, size);
	}
	
	public void SetImmediates<T>(in T data) where T : unmanaged
	{
		fixed(void* ptr = &data)
			wgpuComputePassEncoderSetImmediates(_handle, 0, ptr, (UIntPtr)sizeof(T));
	}
	
	public void SetImmediates<T>(in Span<T> data) where T : unmanaged
	{
		fixed(void* ptr = data)
			wgpuComputePassEncoderSetImmediates(_handle, 0, ptr, (UIntPtr)(sizeof(T) * data.Length));
	}

	// public void SetBindGroup(uint index, GPUBindGroup? bindGroup, Uint32Array dynamicOffsetsData, GPUSize64 dynamicOffsetsDataStart, GPUSize32 dynamicOffsetsDataLength)
	// {
	// 	wgpuComputePassEncoderSetBindGroup(_handle, index, bindGroup._handle, )
	// }
	
	public override void Dispose()
	{
		wgpuComputePassEncoderRelease(_handle);
	}
	
	protected override void SetLabel(WGPUStringView label)
	{
		wgpuComputePassEncoderSetLabel(_handle,label);
	}
}