using System.Runtime.InteropServices;
using Istok.WebGPU.Requests;

namespace Istok.WebGPU;

public unsafe class GPUBuffer(WGPUBuffer buffer, string? label) : GPUObjectWithName<WGPUBuffer>(buffer, label)
{
	public ulong Size => wgpuBufferGetSize(_handle);
	public WGPUBufferUsage Usage => wgpuBufferGetUsage(_handle);
	public WGPUBufferMapState MapState => wgpuBufferGetMapState(_handle);


	public Task MapAsync(WGPUMapMode mode, UIntPtr offset = 0) => MapAsync(mode, offset, (UIntPtr)(Size - offset));
	public Task MapAsync(WGPUMapMode mode, UIntPtr offset, UIntPtr size)
	{
		return BufferMap.Request(this, mode, offset, size);
	}

	public Span<T> GetMappedRange<T>(UIntPtr offset = 0, int count = 1) where T : unmanaged
	{
		return MemoryMarshal.Cast<byte, T>(GetMappedRange(offset, (UIntPtr)(count * sizeof(T))));
	}

	public Span<byte> GetMappedRange(UIntPtr offset = 0)
	{
		var size = (UIntPtr)(Size - offset);
		return GetMappedRange(offset, size);
	}

	public Span<byte> GetMappedRange(UIntPtr offset, UIntPtr size)
	{
		return new Span<byte>(wgpuBufferGetMappedRange(_handle, offset, size), (int)size);
	}
	
	public Span<T> GetConstMappedRange<T>(UIntPtr offset = 0, int count = 1) where T : unmanaged
	{
		return MemoryMarshal.Cast<byte, T>(GetConstMappedRange(offset, (UIntPtr)(count * sizeof(T))));
	}

	public Span<byte> GetConstMappedRange(UIntPtr offset = 0)
	{
		var size = (UIntPtr)(Size - offset);
		return GetConstMappedRange(offset, size);
	}

	public Span<byte> GetConstMappedRange(UIntPtr offset, UIntPtr size)
	{
		return new Span<byte>(wgpuBufferGetConstMappedRange(_handle, offset, size), (int)size);
	}
	public void Unmap()  => wgpuBufferUnmap(_handle);

	public void Destroy() => wgpuBufferDestroy(Handle);
	
	public override void Dispose()
	{
		wgpuBufferRelease(_handle);
	}
	
	protected override void SetLabel(WGPUStringView label)
	{
		wgpuBufferSetLabel(_handle,label);
	}
}