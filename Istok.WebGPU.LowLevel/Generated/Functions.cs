using System.Runtime.InteropServices;
using Silk.NET.Core;

namespace Istok.WebGPU.LowLevel;

public static unsafe partial class WebGPUNative
{
	[LibraryImport(WebGPULib, EntryPoint = "wgpuAdapterInfoFreeMembers")]
	public static partial void wgpuAdapterInfoFreeMembers(WGPUAdapterInfo adapterInfo);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuSupportedFeaturesFreeMembers")]
	public static partial void wgpuSupportedFeaturesFreeMembers(WGPUSupportedFeatures supportedFeatures);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuSupportedInstanceFeaturesFreeMembers")]
	public static partial void wgpuSupportedInstanceFeaturesFreeMembers(WGPUSupportedInstanceFeatures supportedInstanceFeatures);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuSupportedWGSLLanguageFeaturesFreeMembers")]
	public static partial void wgpuSupportedWGSLLanguageFeaturesFreeMembers(WGPUSupportedWGSLLanguageFeatures supportedWGSLLanguageFeatures);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuSurfaceCapabilitiesFreeMembers")]
	public static partial void wgpuSurfaceCapabilitiesFreeMembers(WGPUSurfaceCapabilities surfaceCapabilities);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuAdapterGetLimits")]
	public static partial WGPUStatus wgpuAdapterGetLimits(WGPUAdapter adapter, WGPULimits* limits);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuAdapterHasFeature")]
	public static partial Bool32 wgpuAdapterHasFeature(WGPUAdapter adapter, WGPUFeatureName feature);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuAdapterGetFeatures")]
	public static partial void wgpuAdapterGetFeatures(WGPUAdapter adapter, WGPUSupportedFeatures* features);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuAdapterGetInfo")]
	public static partial WGPUStatus wgpuAdapterGetInfo(WGPUAdapter adapter, WGPUAdapterInfo* info);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuAdapterRequestDevice")]
	public static partial WGPUFuture wgpuAdapterRequestDevice(WGPUAdapter adapter, WGPUDeviceDescriptor* descriptor, WGPURequestDeviceCallbackInfo callbackInfo);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuAdapterAddRef")]
	public static partial void wgpuAdapterAddRef(WGPUAdapter adapter);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuAdapterRelease")]
	public static partial void wgpuAdapterRelease(WGPUAdapter adapter);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuBindGroupSetLabel")]
	public static partial void wgpuBindGroupSetLabel(WGPUBindGroup bindGroup, WGPUStringView label);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuBindGroupAddRef")]
	public static partial void wgpuBindGroupAddRef(WGPUBindGroup bindGroup);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuBindGroupRelease")]
	public static partial void wgpuBindGroupRelease(WGPUBindGroup bindGroup);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuBindGroupLayoutSetLabel")]
	public static partial void wgpuBindGroupLayoutSetLabel(WGPUBindGroupLayout bindGroupLayout, WGPUStringView label);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuBindGroupLayoutAddRef")]
	public static partial void wgpuBindGroupLayoutAddRef(WGPUBindGroupLayout bindGroupLayout);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuBindGroupLayoutRelease")]
	public static partial void wgpuBindGroupLayoutRelease(WGPUBindGroupLayout bindGroupLayout);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuBufferMapAsync")]
	public static partial WGPUFuture wgpuBufferMapAsync(WGPUBuffer buffer, WGPUMapMode mode, UIntPtr offset, UIntPtr size, WGPUBufferMapCallbackInfo callbackInfo);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuBufferGetMappedRange")]
	public static partial void* wgpuBufferGetMappedRange(WGPUBuffer buffer, UIntPtr offset, UIntPtr size);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuBufferGetConstMappedRange")]
	public static partial void* wgpuBufferGetConstMappedRange(WGPUBuffer buffer, UIntPtr offset, UIntPtr size);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuBufferReadMappedRange")]
	public static partial WGPUStatus wgpuBufferReadMappedRange(WGPUBuffer buffer, UIntPtr offset, void* data, UIntPtr size);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuBufferWriteMappedRange")]
	public static partial WGPUStatus wgpuBufferWriteMappedRange(WGPUBuffer buffer, UIntPtr offset, void* data, UIntPtr size);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuBufferSetLabel")]
	public static partial void wgpuBufferSetLabel(WGPUBuffer buffer, WGPUStringView label);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuBufferGetUsage")]
	public static partial WGPUBufferUsage wgpuBufferGetUsage(WGPUBuffer buffer);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuBufferGetSize")]
	public static partial ulong wgpuBufferGetSize(WGPUBuffer buffer);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuBufferGetMapState")]
	public static partial WGPUBufferMapState wgpuBufferGetMapState(WGPUBuffer buffer);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuBufferUnmap")]
	public static partial void wgpuBufferUnmap(WGPUBuffer buffer);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuBufferDestroy")]
	public static partial void wgpuBufferDestroy(WGPUBuffer buffer);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuBufferAddRef")]
	public static partial void wgpuBufferAddRef(WGPUBuffer buffer);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuBufferRelease")]
	public static partial void wgpuBufferRelease(WGPUBuffer buffer);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuCommandBufferSetLabel")]
	public static partial void wgpuCommandBufferSetLabel(WGPUCommandBuffer commandBuffer, WGPUStringView label);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuCommandBufferAddRef")]
	public static partial void wgpuCommandBufferAddRef(WGPUCommandBuffer commandBuffer);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuCommandBufferRelease")]
	public static partial void wgpuCommandBufferRelease(WGPUCommandBuffer commandBuffer);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuCommandEncoderFinish")]
	public static partial WGPUCommandBuffer wgpuCommandEncoderFinish(WGPUCommandEncoder commandEncoder, WGPUCommandBufferDescriptor* descriptor);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuCommandEncoderBeginComputePass")]
	public static partial WGPUComputePassEncoder wgpuCommandEncoderBeginComputePass(WGPUCommandEncoder commandEncoder, WGPUComputePassDescriptor* descriptor);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuCommandEncoderBeginRenderPass")]
	public static partial WGPURenderPassEncoder wgpuCommandEncoderBeginRenderPass(WGPUCommandEncoder commandEncoder, WGPURenderPassDescriptor* descriptor);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuCommandEncoderCopyBufferToBuffer")]
	public static partial void wgpuCommandEncoderCopyBufferToBuffer(WGPUCommandEncoder commandEncoder, WGPUBuffer source, ulong sourceOffset, WGPUBuffer destination, ulong destinationOffset, ulong size);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuCommandEncoderCopyBufferToTexture")]
	public static partial void wgpuCommandEncoderCopyBufferToTexture(WGPUCommandEncoder commandEncoder, WGPUTexelCopyBufferInfo* source, WGPUTexelCopyTextureInfo* destination, WGPUExtent3D* copySize);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuCommandEncoderCopyTextureToBuffer")]
	public static partial void wgpuCommandEncoderCopyTextureToBuffer(WGPUCommandEncoder commandEncoder, WGPUTexelCopyTextureInfo* source, WGPUTexelCopyBufferInfo* destination, WGPUExtent3D* copySize);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuCommandEncoderCopyTextureToTexture")]
	public static partial void wgpuCommandEncoderCopyTextureToTexture(WGPUCommandEncoder commandEncoder, WGPUTexelCopyTextureInfo* source, WGPUTexelCopyTextureInfo* destination, WGPUExtent3D* copySize);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuCommandEncoderClearBuffer")]
	public static partial void wgpuCommandEncoderClearBuffer(WGPUCommandEncoder commandEncoder, WGPUBuffer buffer, ulong offset, ulong size);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuCommandEncoderInsertDebugMarker")]
	public static partial void wgpuCommandEncoderInsertDebugMarker(WGPUCommandEncoder commandEncoder, WGPUStringView markerLabel);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuCommandEncoderPopDebugGroup")]
	public static partial void wgpuCommandEncoderPopDebugGroup(WGPUCommandEncoder commandEncoder);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuCommandEncoderPushDebugGroup")]
	public static partial void wgpuCommandEncoderPushDebugGroup(WGPUCommandEncoder commandEncoder, WGPUStringView groupLabel);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuCommandEncoderResolveQuerySet")]
	public static partial void wgpuCommandEncoderResolveQuerySet(WGPUCommandEncoder commandEncoder, WGPUQuerySet querySet, uint firstQuery, uint queryCount, WGPUBuffer destination, ulong destinationOffset);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuCommandEncoderWriteTimestamp")]
	public static partial void wgpuCommandEncoderWriteTimestamp(WGPUCommandEncoder commandEncoder, WGPUQuerySet querySet, uint queryIndex);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuCommandEncoderSetLabel")]
	public static partial void wgpuCommandEncoderSetLabel(WGPUCommandEncoder commandEncoder, WGPUStringView label);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuCommandEncoderAddRef")]
	public static partial void wgpuCommandEncoderAddRef(WGPUCommandEncoder commandEncoder);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuCommandEncoderRelease")]
	public static partial void wgpuCommandEncoderRelease(WGPUCommandEncoder commandEncoder);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuComputePassEncoderInsertDebugMarker")]
	public static partial void wgpuComputePassEncoderInsertDebugMarker(WGPUComputePassEncoder computePassEncoder, WGPUStringView markerLabel);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuComputePassEncoderPopDebugGroup")]
	public static partial void wgpuComputePassEncoderPopDebugGroup(WGPUComputePassEncoder computePassEncoder);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuComputePassEncoderPushDebugGroup")]
	public static partial void wgpuComputePassEncoderPushDebugGroup(WGPUComputePassEncoder computePassEncoder, WGPUStringView groupLabel);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuComputePassEncoderSetPipeline")]
	public static partial void wgpuComputePassEncoderSetPipeline(WGPUComputePassEncoder computePassEncoder, WGPUComputePipeline pipeline);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuComputePassEncoderSetBindGroup")]
	public static partial void wgpuComputePassEncoderSetBindGroup(WGPUComputePassEncoder computePassEncoder, uint groupIndex, WGPUBindGroup group, UIntPtr dynamicOffsetCount, uint* dynamicOffsets);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuComputePassEncoderDispatchWorkgroups")]
	public static partial void wgpuComputePassEncoderDispatchWorkgroups(WGPUComputePassEncoder computePassEncoder, uint workgroupCountX, uint workgroupCountY, uint workgroupCountZ);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuComputePassEncoderDispatchWorkgroupsIndirect")]
	public static partial void wgpuComputePassEncoderDispatchWorkgroupsIndirect(WGPUComputePassEncoder computePassEncoder, WGPUBuffer indirectBuffer, ulong indirectOffset);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuComputePassEncoderEnd")]
	public static partial void wgpuComputePassEncoderEnd(WGPUComputePassEncoder computePassEncoder);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuComputePassEncoderSetLabel")]
	public static partial void wgpuComputePassEncoderSetLabel(WGPUComputePassEncoder computePassEncoder, WGPUStringView label);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuComputePassEncoderAddRef")]
	public static partial void wgpuComputePassEncoderAddRef(WGPUComputePassEncoder computePassEncoder);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuComputePassEncoderRelease")]
	public static partial void wgpuComputePassEncoderRelease(WGPUComputePassEncoder computePassEncoder);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuComputePipelineGetBindGroupLayout")]
	public static partial WGPUBindGroupLayout wgpuComputePipelineGetBindGroupLayout(WGPUComputePipeline computePipeline, uint groupIndex);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuComputePipelineSetLabel")]
	public static partial void wgpuComputePipelineSetLabel(WGPUComputePipeline computePipeline, WGPUStringView label);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuComputePipelineAddRef")]
	public static partial void wgpuComputePipelineAddRef(WGPUComputePipeline computePipeline);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuComputePipelineRelease")]
	public static partial void wgpuComputePipelineRelease(WGPUComputePipeline computePipeline);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDeviceCreateBindGroup")]
	public static partial WGPUBindGroup wgpuDeviceCreateBindGroup(WGPUDevice device, WGPUBindGroupDescriptor* descriptor);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDeviceCreateBindGroupLayout")]
	public static partial WGPUBindGroupLayout wgpuDeviceCreateBindGroupLayout(WGPUDevice device, WGPUBindGroupLayoutDescriptor* descriptor);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDeviceCreateBuffer")]
	public static partial WGPUBuffer wgpuDeviceCreateBuffer(WGPUDevice device, WGPUBufferDescriptor* descriptor);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDeviceCreateCommandEncoder")]
	public static partial WGPUCommandEncoder wgpuDeviceCreateCommandEncoder(WGPUDevice device, WGPUCommandEncoderDescriptor* descriptor);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDeviceCreateComputePipeline")]
	public static partial WGPUComputePipeline wgpuDeviceCreateComputePipeline(WGPUDevice device, WGPUComputePipelineDescriptor* descriptor);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDeviceCreateComputePipelineAsync")]
	public static partial WGPUFuture wgpuDeviceCreateComputePipelineAsync(WGPUDevice device, WGPUComputePipelineDescriptor* descriptor, WGPUCreateComputePipelineAsyncCallbackInfo callbackInfo);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDeviceCreatePipelineLayout")]
	public static partial WGPUPipelineLayout wgpuDeviceCreatePipelineLayout(WGPUDevice device, WGPUPipelineLayoutDescriptor* descriptor);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDeviceCreateQuerySet")]
	public static partial WGPUQuerySet wgpuDeviceCreateQuerySet(WGPUDevice device, WGPUQuerySetDescriptor* descriptor);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDeviceCreateRenderPipelineAsync")]
	public static partial WGPUFuture wgpuDeviceCreateRenderPipelineAsync(WGPUDevice device, WGPURenderPipelineDescriptor* descriptor, WGPUCreateRenderPipelineAsyncCallbackInfo callbackInfo);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDeviceCreateRenderBundleEncoder")]
	public static partial WGPURenderBundleEncoder wgpuDeviceCreateRenderBundleEncoder(WGPUDevice device, WGPURenderBundleEncoderDescriptor* descriptor);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDeviceCreateRenderPipeline")]
	public static partial WGPURenderPipeline wgpuDeviceCreateRenderPipeline(WGPUDevice device, WGPURenderPipelineDescriptor* descriptor);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDeviceCreateSampler")]
	public static partial WGPUSampler wgpuDeviceCreateSampler(WGPUDevice device, WGPUSamplerDescriptor* descriptor);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDeviceCreateShaderModule")]
	public static partial WGPUShaderModule wgpuDeviceCreateShaderModule(WGPUDevice device, WGPUShaderModuleDescriptor* descriptor);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDeviceCreateTexture")]
	public static partial WGPUTexture wgpuDeviceCreateTexture(WGPUDevice device, WGPUTextureDescriptor* descriptor);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDeviceDestroy")]
	public static partial void wgpuDeviceDestroy(WGPUDevice device);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDeviceGetLostFuture")]
	public static partial WGPUFuture wgpuDeviceGetLostFuture(WGPUDevice device);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDeviceGetLimits")]
	public static partial WGPUStatus wgpuDeviceGetLimits(WGPUDevice device, WGPULimits* limits);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDeviceHasFeature")]
	public static partial Bool32 wgpuDeviceHasFeature(WGPUDevice device, WGPUFeatureName feature);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDeviceGetFeatures")]
	public static partial void wgpuDeviceGetFeatures(WGPUDevice device, WGPUSupportedFeatures* features);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDeviceGetAdapterInfo")]
	public static partial WGPUStatus wgpuDeviceGetAdapterInfo(WGPUDevice device, WGPUAdapterInfo* adapterInfo);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDeviceGetQueue")]
	public static partial WGPUQueue wgpuDeviceGetQueue(WGPUDevice device);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDevicePushErrorScope")]
	public static partial void wgpuDevicePushErrorScope(WGPUDevice device, WGPUErrorFilter filter);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDevicePopErrorScope")]
	public static partial WGPUFuture wgpuDevicePopErrorScope(WGPUDevice device, WGPUPopErrorScopeCallbackInfo callbackInfo);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDeviceSetLabel")]
	public static partial void wgpuDeviceSetLabel(WGPUDevice device, WGPUStringView label);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDeviceAddRef")]
	public static partial void wgpuDeviceAddRef(WGPUDevice device);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuDeviceRelease")]
	public static partial void wgpuDeviceRelease(WGPUDevice device);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuExternalTextureSetLabel")]
	public static partial void wgpuExternalTextureSetLabel(WGPUExternalTexture externalTexture, WGPUStringView label);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuExternalTextureAddRef")]
	public static partial void wgpuExternalTextureAddRef(WGPUExternalTexture externalTexture);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuExternalTextureRelease")]
	public static partial void wgpuExternalTextureRelease(WGPUExternalTexture externalTexture);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuInstanceCreateSurface")]
	public static partial WGPUSurface wgpuInstanceCreateSurface(WGPUInstance instance, WGPUSurfaceDescriptor* descriptor);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuInstanceGetWGSLLanguageFeatures")]
	public static partial void wgpuInstanceGetWGSLLanguageFeatures(WGPUInstance instance, WGPUSupportedWGSLLanguageFeatures* features);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuInstanceHasWGSLLanguageFeature")]
	public static partial Bool32 wgpuInstanceHasWGSLLanguageFeature(WGPUInstance instance, WGPUWGSLLanguageFeatureName feature);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuInstanceProcessEvents")]
	public static partial void wgpuInstanceProcessEvents(WGPUInstance instance);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuInstanceRequestAdapter")]
	public static partial WGPUFuture wgpuInstanceRequestAdapter(WGPUInstance instance, WGPURequestAdapterOptions* options, WGPURequestAdapterCallbackInfo callbackInfo);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuInstanceWaitAny")]
	public static partial WGPUWaitStatus wgpuInstanceWaitAny(WGPUInstance instance, UIntPtr futureCount, WGPUFutureWaitInfo* futures, ulong timeoutNS);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuInstanceAddRef")]
	public static partial void wgpuInstanceAddRef(WGPUInstance instance);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuInstanceRelease")]
	public static partial void wgpuInstanceRelease(WGPUInstance instance);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuPipelineLayoutSetLabel")]
	public static partial void wgpuPipelineLayoutSetLabel(WGPUPipelineLayout pipelineLayout, WGPUStringView label);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuPipelineLayoutAddRef")]
	public static partial void wgpuPipelineLayoutAddRef(WGPUPipelineLayout pipelineLayout);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuPipelineLayoutRelease")]
	public static partial void wgpuPipelineLayoutRelease(WGPUPipelineLayout pipelineLayout);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuQuerySetSetLabel")]
	public static partial void wgpuQuerySetSetLabel(WGPUQuerySet querySet, WGPUStringView label);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuQuerySetGetType")]
	public static partial WGPUQueryType wgpuQuerySetGetType(WGPUQuerySet querySet);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuQuerySetGetCount")]
	public static partial uint wgpuQuerySetGetCount(WGPUQuerySet querySet);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuQuerySetDestroy")]
	public static partial void wgpuQuerySetDestroy(WGPUQuerySet querySet);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuQuerySetAddRef")]
	public static partial void wgpuQuerySetAddRef(WGPUQuerySet querySet);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuQuerySetRelease")]
	public static partial void wgpuQuerySetRelease(WGPUQuerySet querySet);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuQueueSubmit")]
	public static partial void wgpuQueueSubmit(WGPUQueue queue, UIntPtr commandCount, WGPUCommandBuffer* commands);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuQueueOnSubmittedWorkDone")]
	public static partial WGPUFuture wgpuQueueOnSubmittedWorkDone(WGPUQueue queue, WGPUQueueWorkDoneCallbackInfo callbackInfo);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuQueueWriteBuffer")]
	public static partial void wgpuQueueWriteBuffer(WGPUQueue queue, WGPUBuffer buffer, ulong bufferOffset, void* data, UIntPtr size);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuQueueWriteTexture")]
	public static partial void wgpuQueueWriteTexture(WGPUQueue queue, WGPUTexelCopyTextureInfo* destination, void* data, UIntPtr dataSize, WGPUTexelCopyBufferLayout* dataLayout, WGPUExtent3D* writeSize);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuQueueSetLabel")]
	public static partial void wgpuQueueSetLabel(WGPUQueue queue, WGPUStringView label);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuQueueAddRef")]
	public static partial void wgpuQueueAddRef(WGPUQueue queue);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuQueueRelease")]
	public static partial void wgpuQueueRelease(WGPUQueue queue);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderBundleSetLabel")]
	public static partial void wgpuRenderBundleSetLabel(WGPURenderBundle renderBundle, WGPUStringView label);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderBundleAddRef")]
	public static partial void wgpuRenderBundleAddRef(WGPURenderBundle renderBundle);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderBundleRelease")]
	public static partial void wgpuRenderBundleRelease(WGPURenderBundle renderBundle);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderBundleEncoderSetPipeline")]
	public static partial void wgpuRenderBundleEncoderSetPipeline(WGPURenderBundleEncoder renderBundleEncoder, WGPURenderPipeline pipeline);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderBundleEncoderSetBindGroup")]
	public static partial void wgpuRenderBundleEncoderSetBindGroup(WGPURenderBundleEncoder renderBundleEncoder, uint groupIndex, WGPUBindGroup group, UIntPtr dynamicOffsetCount, uint* dynamicOffsets);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderBundleEncoderDraw")]
	public static partial void wgpuRenderBundleEncoderDraw(WGPURenderBundleEncoder renderBundleEncoder, uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderBundleEncoderDrawIndexed")]
	public static partial void wgpuRenderBundleEncoderDrawIndexed(WGPURenderBundleEncoder renderBundleEncoder, uint indexCount, uint instanceCount, uint firstIndex, int baseVertex, uint firstInstance);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderBundleEncoderDrawIndirect")]
	public static partial void wgpuRenderBundleEncoderDrawIndirect(WGPURenderBundleEncoder renderBundleEncoder, WGPUBuffer indirectBuffer, ulong indirectOffset);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderBundleEncoderDrawIndexedIndirect")]
	public static partial void wgpuRenderBundleEncoderDrawIndexedIndirect(WGPURenderBundleEncoder renderBundleEncoder, WGPUBuffer indirectBuffer, ulong indirectOffset);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderBundleEncoderInsertDebugMarker")]
	public static partial void wgpuRenderBundleEncoderInsertDebugMarker(WGPURenderBundleEncoder renderBundleEncoder, WGPUStringView markerLabel);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderBundleEncoderPopDebugGroup")]
	public static partial void wgpuRenderBundleEncoderPopDebugGroup(WGPURenderBundleEncoder renderBundleEncoder);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderBundleEncoderPushDebugGroup")]
	public static partial void wgpuRenderBundleEncoderPushDebugGroup(WGPURenderBundleEncoder renderBundleEncoder, WGPUStringView groupLabel);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderBundleEncoderSetVertexBuffer")]
	public static partial void wgpuRenderBundleEncoderSetVertexBuffer(WGPURenderBundleEncoder renderBundleEncoder, uint slot, WGPUBuffer buffer, ulong offset, ulong size);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderBundleEncoderSetIndexBuffer")]
	public static partial void wgpuRenderBundleEncoderSetIndexBuffer(WGPURenderBundleEncoder renderBundleEncoder, WGPUBuffer buffer, WGPUIndexFormat format, ulong offset, ulong size);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderBundleEncoderFinish")]
	public static partial WGPURenderBundle wgpuRenderBundleEncoderFinish(WGPURenderBundleEncoder renderBundleEncoder, WGPURenderBundleDescriptor* descriptor);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderBundleEncoderSetLabel")]
	public static partial void wgpuRenderBundleEncoderSetLabel(WGPURenderBundleEncoder renderBundleEncoder, WGPUStringView label);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderBundleEncoderAddRef")]
	public static partial void wgpuRenderBundleEncoderAddRef(WGPURenderBundleEncoder renderBundleEncoder);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderBundleEncoderRelease")]
	public static partial void wgpuRenderBundleEncoderRelease(WGPURenderBundleEncoder renderBundleEncoder);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPassEncoderSetPipeline")]
	public static partial void wgpuRenderPassEncoderSetPipeline(WGPURenderPassEncoder renderPassEncoder, WGPURenderPipeline pipeline);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPassEncoderSetBindGroup")]
	public static partial void wgpuRenderPassEncoderSetBindGroup(WGPURenderPassEncoder renderPassEncoder, uint groupIndex, WGPUBindGroup group, UIntPtr dynamicOffsetCount, uint* dynamicOffsets);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPassEncoderDraw")]
	public static partial void wgpuRenderPassEncoderDraw(WGPURenderPassEncoder renderPassEncoder, uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPassEncoderDrawIndexed")]
	public static partial void wgpuRenderPassEncoderDrawIndexed(WGPURenderPassEncoder renderPassEncoder, uint indexCount, uint instanceCount, uint firstIndex, int baseVertex, uint firstInstance);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPassEncoderDrawIndirect")]
	public static partial void wgpuRenderPassEncoderDrawIndirect(WGPURenderPassEncoder renderPassEncoder, WGPUBuffer indirectBuffer, ulong indirectOffset);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPassEncoderDrawIndexedIndirect")]
	public static partial void wgpuRenderPassEncoderDrawIndexedIndirect(WGPURenderPassEncoder renderPassEncoder, WGPUBuffer indirectBuffer, ulong indirectOffset);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPassEncoderExecuteBundles")]
	public static partial void wgpuRenderPassEncoderExecuteBundles(WGPURenderPassEncoder renderPassEncoder, UIntPtr bundleCount, WGPURenderBundle* bundles);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPassEncoderInsertDebugMarker")]
	public static partial void wgpuRenderPassEncoderInsertDebugMarker(WGPURenderPassEncoder renderPassEncoder, WGPUStringView markerLabel);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPassEncoderPopDebugGroup")]
	public static partial void wgpuRenderPassEncoderPopDebugGroup(WGPURenderPassEncoder renderPassEncoder);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPassEncoderPushDebugGroup")]
	public static partial void wgpuRenderPassEncoderPushDebugGroup(WGPURenderPassEncoder renderPassEncoder, WGPUStringView groupLabel);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPassEncoderSetStencilReference")]
	public static partial void wgpuRenderPassEncoderSetStencilReference(WGPURenderPassEncoder renderPassEncoder, uint reference);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPassEncoderSetBlendConstant")]
	public static partial void wgpuRenderPassEncoderSetBlendConstant(WGPURenderPassEncoder renderPassEncoder, WGPUColor* color);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPassEncoderSetViewport")]
	public static partial void wgpuRenderPassEncoderSetViewport(WGPURenderPassEncoder renderPassEncoder, float x, float y, float width, float height, float minDepth, float maxDepth);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPassEncoderSetScissorRect")]
	public static partial void wgpuRenderPassEncoderSetScissorRect(WGPURenderPassEncoder renderPassEncoder, uint x, uint y, uint width, uint height);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPassEncoderSetVertexBuffer")]
	public static partial void wgpuRenderPassEncoderSetVertexBuffer(WGPURenderPassEncoder renderPassEncoder, uint slot, WGPUBuffer buffer, ulong offset, ulong size);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPassEncoderSetIndexBuffer")]
	public static partial void wgpuRenderPassEncoderSetIndexBuffer(WGPURenderPassEncoder renderPassEncoder, WGPUBuffer buffer, WGPUIndexFormat format, ulong offset, ulong size);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPassEncoderBeginOcclusionQuery")]
	public static partial void wgpuRenderPassEncoderBeginOcclusionQuery(WGPURenderPassEncoder renderPassEncoder, uint queryIndex);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPassEncoderEndOcclusionQuery")]
	public static partial void wgpuRenderPassEncoderEndOcclusionQuery(WGPURenderPassEncoder renderPassEncoder);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPassEncoderEnd")]
	public static partial void wgpuRenderPassEncoderEnd(WGPURenderPassEncoder renderPassEncoder);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPassEncoderSetLabel")]
	public static partial void wgpuRenderPassEncoderSetLabel(WGPURenderPassEncoder renderPassEncoder, WGPUStringView label);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPassEncoderAddRef")]
	public static partial void wgpuRenderPassEncoderAddRef(WGPURenderPassEncoder renderPassEncoder);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPassEncoderRelease")]
	public static partial void wgpuRenderPassEncoderRelease(WGPURenderPassEncoder renderPassEncoder);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPipelineGetBindGroupLayout")]
	public static partial WGPUBindGroupLayout wgpuRenderPipelineGetBindGroupLayout(WGPURenderPipeline renderPipeline, uint groupIndex);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPipelineSetLabel")]
	public static partial void wgpuRenderPipelineSetLabel(WGPURenderPipeline renderPipeline, WGPUStringView label);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPipelineAddRef")]
	public static partial void wgpuRenderPipelineAddRef(WGPURenderPipeline renderPipeline);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuRenderPipelineRelease")]
	public static partial void wgpuRenderPipelineRelease(WGPURenderPipeline renderPipeline);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuSamplerSetLabel")]
	public static partial void wgpuSamplerSetLabel(WGPUSampler sampler, WGPUStringView label);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuSamplerAddRef")]
	public static partial void wgpuSamplerAddRef(WGPUSampler sampler);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuSamplerRelease")]
	public static partial void wgpuSamplerRelease(WGPUSampler sampler);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuShaderModuleGetCompilationInfo")]
	public static partial WGPUFuture wgpuShaderModuleGetCompilationInfo(WGPUShaderModule shaderModule, WGPUCompilationInfoCallbackInfo callbackInfo);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuShaderModuleSetLabel")]
	public static partial void wgpuShaderModuleSetLabel(WGPUShaderModule shaderModule, WGPUStringView label);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuShaderModuleAddRef")]
	public static partial void wgpuShaderModuleAddRef(WGPUShaderModule shaderModule);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuShaderModuleRelease")]
	public static partial void wgpuShaderModuleRelease(WGPUShaderModule shaderModule);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuSurfaceConfigure")]
	public static partial void wgpuSurfaceConfigure(WGPUSurface surface, WGPUSurfaceConfiguration* config);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuSurfaceGetCapabilities")]
	public static partial WGPUStatus wgpuSurfaceGetCapabilities(WGPUSurface surface, WGPUAdapter adapter, WGPUSurfaceCapabilities* capabilities);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuSurfaceGetCurrentTexture")]
	public static partial void wgpuSurfaceGetCurrentTexture(WGPUSurface surface, WGPUSurfaceTexture* surfaceTexture);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuSurfacePresent")]
	public static partial WGPUStatus wgpuSurfacePresent(WGPUSurface surface);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuSurfaceUnconfigure")]
	public static partial void wgpuSurfaceUnconfigure(WGPUSurface surface);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuSurfaceSetLabel")]
	public static partial void wgpuSurfaceSetLabel(WGPUSurface surface, WGPUStringView label);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuSurfaceAddRef")]
	public static partial void wgpuSurfaceAddRef(WGPUSurface surface);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuSurfaceRelease")]
	public static partial void wgpuSurfaceRelease(WGPUSurface surface);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuTextureCreateView")]
	public static partial WGPUTextureView wgpuTextureCreateView(WGPUTexture texture, WGPUTextureViewDescriptor* descriptor);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuTextureSetLabel")]
	public static partial void wgpuTextureSetLabel(WGPUTexture texture, WGPUStringView label);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuTextureGetWidth")]
	public static partial uint wgpuTextureGetWidth(WGPUTexture texture);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuTextureGetHeight")]
	public static partial uint wgpuTextureGetHeight(WGPUTexture texture);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuTextureGetDepthOrArrayLayers")]
	public static partial uint wgpuTextureGetDepthOrArrayLayers(WGPUTexture texture);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuTextureGetMipLevelCount")]
	public static partial uint wgpuTextureGetMipLevelCount(WGPUTexture texture);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuTextureGetSampleCount")]
	public static partial uint wgpuTextureGetSampleCount(WGPUTexture texture);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuTextureGetDimension")]
	public static partial WGPUTextureDimension wgpuTextureGetDimension(WGPUTexture texture);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuTextureGetTextureBindingViewDimension")]
	public static partial WGPUTextureViewDimension wgpuTextureGetTextureBindingViewDimension(WGPUTexture texture);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuTextureGetFormat")]
	public static partial WGPUTextureFormat wgpuTextureGetFormat(WGPUTexture texture);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuTextureGetUsage")]
	public static partial WGPUTextureUsage wgpuTextureGetUsage(WGPUTexture texture);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuTextureDestroy")]
	public static partial void wgpuTextureDestroy(WGPUTexture texture);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuTextureAddRef")]
	public static partial void wgpuTextureAddRef(WGPUTexture texture);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuTextureRelease")]
	public static partial void wgpuTextureRelease(WGPUTexture texture);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuTextureViewSetLabel")]
	public static partial void wgpuTextureViewSetLabel(WGPUTextureView textureView, WGPUStringView label);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuTextureViewAddRef")]
	public static partial void wgpuTextureViewAddRef(WGPUTextureView textureView);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuTextureViewRelease")]
	public static partial void wgpuTextureViewRelease(WGPUTextureView textureView);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuCreateInstance")]
	public static partial WGPUInstance wgpuCreateInstance(WGPUInstanceDescriptor* descriptor);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuGetInstanceFeatures")]
	public static partial void wgpuGetInstanceFeatures(WGPUSupportedInstanceFeatures* features);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuGetInstanceLimits")]
	public static partial WGPUStatus wgpuGetInstanceLimits(WGPUInstanceLimits* limits);
	[LibraryImport(WebGPULib, EntryPoint = "wgpuHasInstanceFeature")]
	public static partial Bool32 wgpuHasInstanceFeature(WGPUInstanceFeatureName feature);
}
