using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CallbackResult = System.Threading.Tasks.TaskCompletionSource<Istok.WebGPU.GPUAdapter>;

namespace Istok.WebGPU.Requests;

public static unsafe class Adapter
{
	private static readonly WGPURequestAdapterCallback PfnCallback;
	
	static Adapter()
	{
		PfnCallback = new WGPURequestAdapterCallback(&RequestCallback);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void RequestCallback(WGPURequestAdapterStatus status, WGPUAdapter adapter, WGPUStringView message, void* userdata1, void* userdata2)
	{
		var handle = GCHandle.FromIntPtr((IntPtr)userdata1);
		if (Callback<CallbackResult>.GetResult(handle, out var result))
		{
			result.SetResult(new GPUAdapter(adapter));
		}
	}
	
	public static Task<GPUAdapter> Request(GPU gpu, in WGPURequestAdapterOptions options)
	{
		var taskCompletionSource = new CallbackResult();
		var pin = Callback<CallbackResult>.Register(taskCompletionSource);
		
		fixed (WGPURequestAdapterOptions* optionsPtr = &options)
		{
			wgpuInstanceRequestAdapter(
				gpu._instance,
				optionsPtr,
				new WGPURequestAdapterCallbackInfo()
				{
					callback = PfnCallback,
					mode = WGPUCallbackMode.AllowSpontaneous,
					userdata1 = (void*)GCHandle.ToIntPtr(pin)		
				}
			);
		}
		
		return taskCompletionSource.Task;
	}
}