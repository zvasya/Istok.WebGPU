namespace Istok.WebGPU;

public abstract unsafe class GPUObject<T>(T handle) : IDisposable where T : unmanaged
{
	protected internal readonly T _handle = handle;
	public T Handle => _handle;
	// public string HandleToString() => $"{(nuint)_handle:X}";
	
	public abstract void Dispose();
	
	public static implicit operator T(GPUObject<T> gpuObject) =>gpuObject.Handle;
}