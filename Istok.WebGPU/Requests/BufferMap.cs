using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CallbackResult = System.Threading.Tasks.TaskCompletionSource<Istok.WebGPU.LowLevel.WGPUMapAsyncStatus>;

namespace Istok.WebGPU.Requests;

public static unsafe class BufferMap
{
	private static readonly WGPUBufferMapCallback pfnCallback;
	
	static BufferMap()
	{
		pfnCallback = new WGPUBufferMapCallback(&RequestCallback);
	}

	
	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void RequestCallback(WGPUMapAsyncStatus bufferMapAsyncStatus, WGPUStringView message, void* userdata1, void* userdata2)
	{ 
		GCHandle handle = GCHandle.FromIntPtr((IntPtr)userdata1);
		if (Callback<CallbackResult>.GetResult(handle, out var result))
		{
			result.SetResult(bufferMapAsyncStatus);
		}
	}
	
	public static Task<WGPUMapAsyncStatus> Request(GPUBuffer buffer, WGPUMapMode mode, UIntPtr offset, UIntPtr size)
	{
		var taskCompletionSource = new CallbackResult();
		var pin = Callback<CallbackResult>.Register(taskCompletionSource);
		
		wgpuBufferMapAsync(
			buffer._handle,
			mode,
			offset,
			size,
			new WGPUBufferMapCallbackInfo()
			{
				callback = pfnCallback,
				
				userdata1 = (void*)GCHandle.ToIntPtr(pin)
			}
		);
		
		return taskCompletionSource.Task;
	}
}