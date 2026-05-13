global using static Istok.WebGPU.LowLevel.WebGPUNative;
global using Istok.WebGPU.LowLevel;

namespace Istok.WebGPU;

public unsafe partial class GPU : IDisposable
{
	// public static WebGPU wgpu;
	internal readonly WGPUInstance _instance;
	public WGPUInstance Instance => _instance;

	public string InstanceToString() => $"{_instance.Handle:X}";

	public static unsafe void InitExtension(GPUDevice device)
	{
		// WebGPUNative.c
		// if (!WebGPU.TryGetDeviceExtension<Wgpu>(device.Handle, out var wGPU))
		// {
		// 	throw new Exception("Unable to find Extension");
		// }
		//
		// WGPU = wGPU;
	}
	
	private GPU(WGPUInstance instance)
	{
		_instance = instance;
	}

	public static GPU Create()
	{
		// wgpu = WebGPU.GetApi();
		return new GPU(wgpuCreateInstance(null));
	}

	public static GPU Create(in WGPUInstanceDescriptor* descriptor)
	{
		return new GPU(wgpuCreateInstance(descriptor));
	}
	
	public unsafe Task<GPUAdapter> RequestAdapter(in WGPURequestAdapterOptions options)
	{
		return Requests.Adapter.Request(this, options);
	}

	public void Dispose()
	{
		wgpuInstanceRelease(_instance);
	}
}