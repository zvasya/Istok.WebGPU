using System.Runtime.InteropServices;
using Silk.NET.Core;

namespace Istok.WebGPU.LowLevel;

public unsafe readonly struct WGPUBufferMapCallback
{
	private readonly void* _handle;
	public delegate* unmanaged[Cdecl]<WGPUMapAsyncStatus, WGPUStringView, void*, void*, void> Handle => (delegate* unmanaged[Cdecl]<WGPUMapAsyncStatus, WGPUStringView, void*, void*, void>) _handle;
	public WGPUBufferMapCallback(delegate* unmanaged[Cdecl]<WGPUMapAsyncStatus, WGPUStringView, void*, void*, void> ptr) => _handle = ptr;
}

public unsafe readonly struct WGPUCompilationInfoCallback
{
	private readonly void* _handle;
	public delegate* unmanaged[Cdecl]<WGPUCompilationInfoRequestStatus, WGPUCompilationInfo*, void*, void*, void> Handle => (delegate* unmanaged[Cdecl]<WGPUCompilationInfoRequestStatus, WGPUCompilationInfo*, void*, void*, void>) _handle;
	public WGPUCompilationInfoCallback(delegate* unmanaged[Cdecl]<WGPUCompilationInfoRequestStatus, WGPUCompilationInfo*, void*, void*, void> ptr) => _handle = ptr;
}

public unsafe readonly struct WGPUCreateComputePipelineAsyncCallback
{
	private readonly void* _handle;
	public delegate* unmanaged[Cdecl]<WGPUCreatePipelineAsyncStatus, WGPUComputePipeline, WGPUStringView, void*, void*, void> Handle => (delegate* unmanaged[Cdecl]<WGPUCreatePipelineAsyncStatus, WGPUComputePipeline, WGPUStringView, void*, void*, void>) _handle;
	public WGPUCreateComputePipelineAsyncCallback(delegate* unmanaged[Cdecl]<WGPUCreatePipelineAsyncStatus, WGPUComputePipeline, WGPUStringView, void*, void*, void> ptr) => _handle = ptr;
}

public unsafe readonly struct WGPUCreateRenderPipelineAsyncCallback
{
	private readonly void* _handle;
	public delegate* unmanaged[Cdecl]<WGPUCreatePipelineAsyncStatus, WGPURenderPipeline, WGPUStringView, void*, void*, void> Handle => (delegate* unmanaged[Cdecl]<WGPUCreatePipelineAsyncStatus, WGPURenderPipeline, WGPUStringView, void*, void*, void>) _handle;
	public WGPUCreateRenderPipelineAsyncCallback(delegate* unmanaged[Cdecl]<WGPUCreatePipelineAsyncStatus, WGPURenderPipeline, WGPUStringView, void*, void*, void> ptr) => _handle = ptr;
}

public unsafe readonly struct WGPUDeviceLostCallback
{
	private readonly void* _handle;
	public delegate* unmanaged[Cdecl]<WGPUDevice*, WGPUDeviceLostReason, WGPUStringView, void*, void*, void> Handle => (delegate* unmanaged[Cdecl]<WGPUDevice*, WGPUDeviceLostReason, WGPUStringView, void*, void*, void>) _handle;
	public WGPUDeviceLostCallback(delegate* unmanaged[Cdecl]<WGPUDevice*, WGPUDeviceLostReason, WGPUStringView, void*, void*, void> ptr) => _handle = ptr;
}

public unsafe readonly struct WGPUPopErrorScopeCallback
{
	private readonly void* _handle;
	public delegate* unmanaged[Cdecl]<WGPUPopErrorScopeStatus, WGPUErrorType, WGPUStringView, void*, void*, void> Handle => (delegate* unmanaged[Cdecl]<WGPUPopErrorScopeStatus, WGPUErrorType, WGPUStringView, void*, void*, void>) _handle;
	public WGPUPopErrorScopeCallback(delegate* unmanaged[Cdecl]<WGPUPopErrorScopeStatus, WGPUErrorType, WGPUStringView, void*, void*, void> ptr) => _handle = ptr;
}

public unsafe readonly struct WGPUQueueWorkDoneCallback
{
	private readonly void* _handle;
	public delegate* unmanaged[Cdecl]<WGPUQueueWorkDoneStatus, WGPUStringView, void*, void*, void> Handle => (delegate* unmanaged[Cdecl]<WGPUQueueWorkDoneStatus, WGPUStringView, void*, void*, void>) _handle;
	public WGPUQueueWorkDoneCallback(delegate* unmanaged[Cdecl]<WGPUQueueWorkDoneStatus, WGPUStringView, void*, void*, void> ptr) => _handle = ptr;
}

public unsafe readonly struct WGPURequestAdapterCallback
{
	private readonly void* _handle;
	public delegate* unmanaged[Cdecl]<WGPURequestAdapterStatus, WGPUAdapter, WGPUStringView, void*, void*, void> Handle => (delegate* unmanaged[Cdecl]<WGPURequestAdapterStatus, WGPUAdapter, WGPUStringView, void*, void*, void>) _handle;
	public WGPURequestAdapterCallback(delegate* unmanaged[Cdecl]<WGPURequestAdapterStatus, WGPUAdapter, WGPUStringView, void*, void*, void> ptr) => _handle = ptr;
}

public unsafe readonly struct WGPURequestDeviceCallback
{
	private readonly void* _handle;
	public delegate* unmanaged[Cdecl]<WGPURequestDeviceStatus, WGPUDevice, WGPUStringView, void*, void*, void> Handle => (delegate* unmanaged[Cdecl]<WGPURequestDeviceStatus, WGPUDevice, WGPUStringView, void*, void*, void>) _handle;
	public WGPURequestDeviceCallback(delegate* unmanaged[Cdecl]<WGPURequestDeviceStatus, WGPUDevice, WGPUStringView, void*, void*, void> ptr) => _handle = ptr;
}

public unsafe readonly struct WGPUUncapturedErrorCallback
{
	private readonly void* _handle;
	public delegate* unmanaged[Cdecl]<WGPUDevice*, WGPUErrorType, WGPUStringView, void*, void*, void> Handle => (delegate* unmanaged[Cdecl]<WGPUDevice*, WGPUErrorType, WGPUStringView, void*, void*, void>) _handle;
	public WGPUUncapturedErrorCallback(delegate* unmanaged[Cdecl]<WGPUDevice*, WGPUErrorType, WGPUStringView, void*, void*, void> ptr) => _handle = ptr;
}

