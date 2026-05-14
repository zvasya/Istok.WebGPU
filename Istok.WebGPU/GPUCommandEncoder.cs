using System.Runtime.CompilerServices;

namespace Istok.WebGPU;

public unsafe class GPUCommandEncoder(WGPUCommandEncoder commandEncoder, string? label) : GPUObjectWithName<WGPUCommandEncoder>(commandEncoder, label)
{
	public GPURenderPassEncoder BeginRenderPass(GPURenderPassDescriptor descriptor)
	{
		// IntPtr labelPtr = SilkMarshal.StringToPtr(descriptor.Label);
		using (descriptor.Label.ToWGPUStringView( out WGPUStringView labelPtr))
		{
			WGPURenderPassEncoder renderPassEncoder;
			fixed (WGPURenderPassColorAttachment* colorAttachmentsPtr = descriptor.ColorAttachments)
			{
				var renderPassDescriptor = new WGPURenderPassDescriptor
				{
					label = labelPtr,
					colorAttachmentCount = (UIntPtr)descriptor.ColorAttachments.Length,
					colorAttachments = colorAttachmentsPtr,
					depthStencilAttachment = Unsafe.IsNullRef(ref descriptor.DepthStencilAttachment) ? null : &descriptor.DepthStencilAttachment,
					occlusionQuerySet = descriptor.OcclusionQuerySet?._handle ?? WGPUQuerySet.Null,
					timestampWrites = &descriptor.TimestampWrites
				};
				renderPassEncoder = wgpuCommandEncoderBeginRenderPass(_handle, &renderPassDescriptor);
				
				return new GPURenderPassEncoder(renderPassEncoder, descriptor.Label);
			}
		}
	}

	public GPUComputePassEncoder BeginComputePass()
	{
		var computePassEncoder = wgpuCommandEncoderBeginComputePass(_handle, null);
		return new GPUComputePassEncoder(computePassEncoder, null);
	}

	public GPUComputePassEncoder BeginComputePass(GPUComputePassDescriptor descriptor)
	{
		using (descriptor.Label.ToWGPUStringView(out WGPUStringView descriptorLabelPtr))
		{
				
			var renderPassDescriptor = new WGPUComputePassDescriptor() with
			{
				label = descriptorLabelPtr,
				timestampWrites = &descriptor.TimestampWrites,
			};
			var computePassEncoder = wgpuCommandEncoderBeginComputePass(_handle, &renderPassDescriptor);
			return new GPUComputePassEncoder(computePassEncoder, descriptor.Label);
		}
	}

	public void CopyBufferToBuffer(GPUBuffer source, GPUBuffer destination, ulong? size = null)
	{
		CopyBufferToBuffer(source, 0, destination, 0, size);
	}

	public void CopyBufferToBuffer(GPUBuffer source, ulong sourceOffset, GPUBuffer destination, ulong destinationOffset, ulong? size = null)
	{
		wgpuCommandEncoderCopyBufferToBuffer(_handle, source._handle, sourceOffset, destination._handle, destinationOffset, size ?? source.Size - sourceOffset);
	}

	public void CopyBufferToTexture(WGPUTexelCopyBufferInfo source, WGPUTexelCopyTextureInfo destination, WGPUExtent3D copySize)
	{
		wgpuCommandEncoderCopyBufferToTexture(_handle, &source, &destination, &copySize);
	}

	public void CopyTextureToBuffer(WGPUTexelCopyTextureInfo source, WGPUTexelCopyBufferInfo destination, WGPUExtent3D copySize)
	{
		wgpuCommandEncoderCopyTextureToBuffer(_handle, &source, &destination, &copySize);
	}

	public void CopyTextureToTexture(WGPUTexelCopyTextureInfo source, WGPUTexelCopyTextureInfo destination, WGPUExtent3D copySize)
	{
		wgpuCommandEncoderCopyTextureToTexture(_handle, &source, &destination, &copySize);
	}

	public void ClearBuffer(GPUBuffer buffer, ulong offset = 0)
	{
		ClearBuffer(buffer, offset, buffer.Size - offset);
	}

	public void ClearBuffer(GPUBuffer buffer, ulong offset, ulong size)
	{
		wgpuCommandEncoderClearBuffer(_handle, buffer._handle, offset, size);
	}

	public void ResolveQuerySet(GPUQuerySet querySet, uint firstQuery, uint queryCount, GPUBuffer destination, ulong destinationOffset)
	{
		wgpuCommandEncoderResolveQuerySet(_handle, querySet._handle, firstQuery, queryCount, destination._handle, destinationOffset);
	}

	public GPUCommandBuffer Finish()
	{
		var commandBuffer = wgpuCommandEncoderFinish(_handle, null);
		return new GPUCommandBuffer(commandBuffer, null);
	}

	public GPUCommandBuffer Finish(GPUCommandBufferDescriptor descriptor)
	{
		using (descriptor.Label.ToWGPUStringView(out WGPUStringView descriptorLabelPtr))
		{
			WGPUCommandBufferDescriptor commandBufferDescriptor = new WGPUCommandBufferDescriptor() with
			{
				label = descriptorLabelPtr
			};
			WGPUCommandBuffer commandBuffer = wgpuCommandEncoderFinish(_handle, &commandBufferDescriptor);
		
			return new GPUCommandBuffer(commandBuffer, null);
		}
	}

	public void PushDebugGroup(string groupLabel)
	{
		using(groupLabel.ToWGPUStringView(out WGPUStringView groupLabelPtr))
			wgpuCommandEncoderPushDebugGroup(_handle, groupLabelPtr);
	}

	public void PopDebugGroup()
	{
		wgpuCommandEncoderPopDebugGroup(_handle);
	}

	public void InsertDebugMarker(string markerLabel)
	{
		using (markerLabel.ToWGPUStringView(out WGPUStringView markerLabelPtr))
			wgpuCommandEncoderInsertDebugMarker(_handle, markerLabelPtr);
	}

	public override void Dispose()
	{
		wgpuCommandEncoderRelease(_handle);
	}
	
	protected override void SetLabel(WGPUStringView label)
	{
		wgpuCommandEncoderSetLabel(_handle,label);
	}
}