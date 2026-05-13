using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Istok.WebGPU.Requests;

public static unsafe class Device
{
	static readonly WGPURequestDeviceCallback pfnRequestCallback;
	static readonly WGPUDeviceLostCallback pfnLostCallback;
	static readonly WGPUUncapturedErrorCallback pfnUncapturedErrorCallback;
	
	static Device()
	{
		pfnRequestCallback =  new WGPURequestDeviceCallback(&RequestCallback);
		pfnLostCallback = new WGPUDeviceLostCallback(&DeviceLost);
		pfnUncapturedErrorCallback = new WGPUUncapturedErrorCallback(&UncapturedErrorCallbackUncapturedErrorCallback);
	}

	struct RequestData(GPUAdapter adapter, string? label, TaskCompletionSource<GPUDevice> taskCompletionSource, TaskCompletionSource<(WGPUDeviceLostReason, string?)> lostDevicePromise)
	{
		public readonly GPUAdapter Adapter = adapter;
		public readonly string? Label = label;
		public readonly TaskCompletionSource<GPUDevice> TaskCompletionSource = taskCompletionSource;
		public readonly TaskCompletionSource<(WGPUDeviceLostReason, string?)> LostDevicePromise = lostDevicePromise;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void DeviceLost(WGPUDevice* device, WGPUDeviceLostReason reason, WGPUStringView message, void* userdata1, void* userdata2)
	{
		var handle = GCHandle.FromIntPtr((IntPtr)userdata1);

		if (Callback<TaskCompletionSource<(WGPUDeviceLostReason, string?)>>.GetResult(handle, out var value))
		{
			value.SetResult((reason, message.ToStr()));
		}
	}
	
	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void RequestCallback(WGPURequestDeviceStatus status, WGPUDevice device, WGPUStringView message, void* userdata1, void* userdata2)
	{
		var handle = GCHandle.FromIntPtr((IntPtr)userdata1);
		if (Callback<RequestData>.GetResult(handle, out var value))
		{
			var gpuDevice = new GPUDevice(device, value.Label, value.Adapter.Info, value.LostDevicePromise.Task);
			value.TaskCompletionSource.SetResult(gpuDevice);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void UncapturedErrorCallbackUncapturedErrorCallback(WGPUDevice* device, WGPUErrorType errorType, WGPUStringView message, void* userdata1, void* userdata2)
	{
		Console.WriteLine($"[Uncaptured Error] {message.ToStr()}");
#if DEBUG// && BREAK_ON_ERROR
		if (System.Diagnostics.Debugger.IsAttached)
		{
			System.Diagnostics.Debugger.Break();
		}
#endif
	}

	public static Task<GPUDevice> Request(GPUAdapter adapter, in GPUDeviceDescriptor descriptor)
	{
	
		var taskCompletionSource = new TaskCompletionSource<GPUDevice>();
		var lostDevicePromise = new TaskCompletionSource<(WGPUDeviceLostReason, string?)>();
		
		var pin = Callback<RequestData>.Register(new RequestData(adapter, descriptor.Label, taskCompletionSource, lostDevicePromise));
		var pinLost = Callback<TaskCompletionSource<(WGPUDeviceLostReason, string?)>>.Register(lostDevicePromise);

		using (descriptor.Label.ToWGPUStringView(out var labelPtr))
		{
			fixed(WGPULimits* requiredLimitsPtr = &descriptor.RequiredLimits)
			fixed(WGPUFeatureName* requiredFeaturesPtr = descriptor.RequiredFeatures)
			{
				WGPUDeviceDescriptor descriptorPtr = new WGPUDeviceDescriptor
				{
					nextInChain = null,
					label = labelPtr,
					requiredFeatureCount = (UIntPtr)descriptor.RequiredFeatures.Length,
					requiredFeatures = requiredFeaturesPtr,
					requiredLimits = requiredLimitsPtr,
					defaultQueue = descriptor.DefaultQueue,
					deviceLostCallbackInfo = new WGPUDeviceLostCallbackInfo()
					{
						mode = WGPUCallbackMode.AllowSpontaneous,
						callback = pfnLostCallback,
						userdata1 = (void*)GCHandle.ToIntPtr(pinLost),
					},
					uncapturedErrorCallbackInfo = new WGPUUncapturedErrorCallbackInfo()
					{
						callback = pfnUncapturedErrorCallback,
					} 
				};
				wgpuAdapterRequestDevice
				(
					adapter._handle,
					&descriptorPtr,
					new WGPURequestDeviceCallbackInfo()
					{
						mode = WGPUCallbackMode.AllowSpontaneous,
						callback = pfnRequestCallback,
						userdata1 = (void*)GCHandle.ToIntPtr(pin)
					}
				);
			}
		}
		
		return taskCompletionSource.Task;
	}
	
}