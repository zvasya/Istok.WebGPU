using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CallbackResult = System.Threading.Tasks.TaskCompletionSource<Istok.WebGPU.LowLevel.WGPUQueueWorkDoneStatus>;

namespace Istok.WebGPU.Requests;

public unsafe class SubmittedWorkDone
{
	static readonly WGPUQueueWorkDoneCallback pfnCallback;
	
	static SubmittedWorkDone()
	{
		pfnCallback = new WGPUQueueWorkDoneCallback(&RequestCallback);
	}
	
	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	static void RequestCallback(WGPUQueueWorkDoneStatus status, WGPUStringView message, void* userdata1, void* userdata2)
	{
		var handle = GCHandle.FromIntPtr((IntPtr)userdata1);
		if (Callback<CallbackResult>.GetResult(handle, out var result))
		{
			result.SetResult(status);
		}
	}
	
	public static Task Request(GPUQueue queue)
	{
		var taskCompletionSource = new CallbackResult();
		var pin = Callback<CallbackResult>.Register(taskCompletionSource);

		wgpuQueueOnSubmittedWorkDone(
			queue._handle,
			new WGPUQueueWorkDoneCallbackInfo()
			{
				mode = WGPUCallbackMode.AllowSpontaneous,
				callback = pfnCallback,
				userdata1 = (void*)GCHandle.ToIntPtr(pin)
			}
		);
		
		return taskCompletionSource.Task;
	}
}