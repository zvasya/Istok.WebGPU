using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CompilationInfoResult = (Istok.WebGPU.LowLevel.WGPUCompilationInfoRequestStatus status, Istok.WebGPU.LowLevel.WGPUCompilationInfo info);
using CallbackResult = System.Threading.Tasks.TaskCompletionSource<(Istok.WebGPU.LowLevel.WGPUCompilationInfoRequestStatus status, Istok.WebGPU.LowLevel.WGPUCompilationInfo info)>;

namespace Istok.WebGPU.Requests;

public unsafe class CompilationInfo
{
	static readonly WGPUCompilationInfoCallback pfnCallback;
	
	static CompilationInfo()
	{
		pfnCallback = new WGPUCompilationInfoCallback(&RequestCallback);
	}
	
	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	static void RequestCallback(WGPUCompilationInfoRequestStatus wgpuCompilationInfoRequestStatus, WGPUCompilationInfo* compilationInfo, void* userdata1, void* userdata2)
	{
		var handle = GCHandle.FromIntPtr((IntPtr)userdata1);
		if (Callback<CallbackResult>.GetResult(handle, out var result))
		{
			result.SetResult((wgpuCompilationInfoRequestStatus, *compilationInfo));
		}
	}
	
	public static Task<CompilationInfoResult> Request(GPUShaderModule shaderModule)
	{
		var taskCompletionSource = new TaskCompletionSource<CompilationInfoResult>();
		var pin = Callback<CallbackResult>.Register(taskCompletionSource);

		wgpuShaderModuleGetCompilationInfo(
			shaderModule._handle,
			new WGPUCompilationInfoCallbackInfo()
			{
				mode = WGPUCallbackMode.AllowSpontaneous,
				callback = pfnCallback,
				userdata1 = (void*)GCHandle.ToIntPtr(pin)
			}
		);
		
		return taskCompletionSource.Task;
	}
}