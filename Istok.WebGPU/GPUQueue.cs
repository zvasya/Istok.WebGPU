using Istok.WebGPU.Requests;

namespace Istok.WebGPU;

public unsafe class GPUQueue(WGPUQueue queue, string? label) : GPUObjectWithName<WGPUQueue>(queue, label)
{
	public void Submit(GPUCommandBuffer commandBuffers)
	{
		var commandBuffersHandle = commandBuffers._handle;
		wgpuQueueSubmit(_handle, 1, &commandBuffersHandle);
	}
	public void Submit(ReadOnlySpan<GPUCommandBuffer> commandBuffers)
	{
		WGPUCommandBuffer* commandBuffersPtr = stackalloc WGPUCommandBuffer[commandBuffers.Length];
		for (int i = 0; i < commandBuffers.Length; i++)
		{
			commandBuffersPtr[i] = commandBuffers[i]._handle;
		}

		wgpuQueueSubmit(_handle, (UIntPtr)commandBuffers.Length, commandBuffersPtr);
	}

	Task OnSubmittedWorkDone()
	{
		return SubmittedWorkDone.Request(this);
	}
	
	public void WriteBuffer(GPUBuffer buffer, ulong bufferOffset, void* data, UIntPtr size)
	{
		wgpuQueueWriteBuffer(_handle, buffer._handle, bufferOffset, data,  size);
	}

	public void WriteTexture(WGPUTexelCopyTextureInfo destination, void* data, UIntPtr dataSize, WGPUTexelCopyBufferLayout dataLayout, WGPUExtent3D size)
	{
		wgpuQueueWriteTexture(_handle, &destination, data, dataSize, &dataLayout, &size);
	}

	public override void Dispose()
	{
		wgpuQueueRelease(_handle);
	}
	protected override void SetLabel(WGPUStringView label)
	{
		wgpuQueueSetLabel(_handle,label);
	}
}