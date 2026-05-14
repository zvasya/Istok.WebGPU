using System.Runtime.InteropServices;
using Silk.NET.Core;

namespace Istok.WebGPU.LowLevel;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUAdapterInfo()
{
		public ChainedStruct* nextInChain;
		public WGPUStringView vendor;
		public WGPUStringView architecture;
		public WGPUStringView device;
		public WGPUStringView description;
		public WGPUBackendType backendType;
		public WGPUAdapterType adapterType;
		public uint vendorID;
		public uint deviceID;
		public uint subgroupMinSize;
		public uint subgroupMaxSize;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUBindGroupDescriptor()
{
		public ChainedStruct* nextInChain;
		public WGPUStringView label;
		public WGPUBindGroupLayout layout;
		public UIntPtr entryCount;
		public WGPUBindGroupEntry* entries;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUBindGroupEntry()
{
		public ChainedStruct* nextInChain;
		public uint binding;
		public WGPUBuffer buffer;
		public ulong offset;
		public ulong size = WebGPUNative.WholeSize;
		public WGPUSampler sampler;
		public WGPUTextureView textureView;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUBindGroupLayoutDescriptor()
{
		public ChainedStruct* nextInChain;
		public WGPUStringView label;
		public UIntPtr entryCount;
		public WGPUBindGroupLayoutEntry* entries;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUBindGroupLayoutEntry()
{
		public ChainedStruct* nextInChain;
		public uint binding;
		public WGPUShaderStage visibility = WGPUShaderStage.None;
		public uint bindingArraySize;
		public WGPUBufferBindingLayout buffer;
		public WGPUSamplerBindingLayout sampler;
		public WGPUTextureBindingLayout texture;
		public WGPUStorageTextureBindingLayout storageTexture;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUBlendComponent()
{
		public WGPUBlendOperation operation;
		public WGPUBlendFactor srcFactor;
		public WGPUBlendFactor dstFactor;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUBlendState()
{
		public WGPUBlendComponent color;
		public WGPUBlendComponent alpha;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUBufferBindingLayout()
{
		public ChainedStruct* nextInChain;
		public WGPUBufferBindingType type;
		public Bool32 hasDynamicOffset = false;
		public ulong minBindingSize = 0;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUBufferDescriptor()
{
		public ChainedStruct* nextInChain;
		public WGPUStringView label;
		public WGPUBufferUsage usage = WGPUBufferUsage.None;
		public ulong size;
		public Bool32 mappedAtCreation = false;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUColor()
{
		public double r;
		public double g;
		public double b;
		public double a;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUColorTargetState()
{
		public ChainedStruct* nextInChain;
		public WGPUTextureFormat format;
		public WGPUBlendState* blend;
		public WGPUColorWriteMask writeMask = WGPUColorWriteMask.All;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUCommandBufferDescriptor()
{
		public ChainedStruct* nextInChain;
		public WGPUStringView label;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUCommandEncoderDescriptor()
{
		public ChainedStruct* nextInChain;
		public WGPUStringView label;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUCompatibilityModeLimits()
{
		public ChainedStruct chain;
		public uint maxStorageBuffersInVertexStage = WebGPUNative.LimitU32Undefined;
		public uint maxStorageTexturesInVertexStage = WebGPUNative.LimitU32Undefined;
		public uint maxStorageBuffersInFragmentStage = WebGPUNative.LimitU32Undefined;
		public uint maxStorageTexturesInFragmentStage = WebGPUNative.LimitU32Undefined;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUCompilationInfo()
{
		public ChainedStruct* nextInChain;
		public UIntPtr messageCount;
		public WGPUCompilationMessage* messages;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUCompilationMessage()
{
		public ChainedStruct* nextInChain;
		public WGPUStringView message;
		public WGPUCompilationMessageType type;
		public ulong lineNum;
		public ulong linePos;
		public ulong offset;
		public ulong length;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUComputePassDescriptor()
{
		public ChainedStruct* nextInChain;
		public WGPUStringView label;
		public WGPUPassTimestampWrites* timestampWrites;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUComputePipelineDescriptor()
{
		public ChainedStruct* nextInChain;
		public WGPUStringView label;
		public WGPUPipelineLayout layout;
		public WGPUComputeState compute;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUComputeState()
{
		public ChainedStruct* nextInChain;
		public WGPUShaderModule module;
		public WGPUStringView entryPoint;
		public UIntPtr constantCount;
		public WGPUConstantEntry* constants;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUConstantEntry()
{
		public ChainedStruct* nextInChain;
		public WGPUStringView key;
		public double value;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUDepthStencilState()
{
		public ChainedStruct* nextInChain;
		public WGPUTextureFormat format;
		public WGPUOptionalBool depthWriteEnabled;
		public WGPUCompareFunction depthCompare;
		public WGPUStencilFaceState stencilFront;
		public WGPUStencilFaceState stencilBack;
		public uint stencilReadMask = 0xFFFFFFFF;
		public uint stencilWriteMask = 0xFFFFFFFF;
		public int depthBias = 0;
		public float depthBiasSlopeScale = 0;
		public float depthBiasClamp = 0;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUDeviceDescriptor()
{
		public ChainedStruct* nextInChain;
		public WGPUStringView label;
		public UIntPtr requiredFeatureCount;
		public WGPUFeatureName* requiredFeatures;
		public WGPULimits* requiredLimits;
		public WGPUQueueDescriptor defaultQueue;
		public WGPUDeviceLostCallbackInfo deviceLostCallbackInfo;
		public WGPUUncapturedErrorCallbackInfo uncapturedErrorCallbackInfo;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUExtent3D()
{
		public uint width;
		public uint height = 1;
		public uint depthOrArrayLayers = 1;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUExternalTextureBindingEntry()
{
		public ChainedStruct chain;
		public WGPUExternalTexture externalTexture;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUExternalTextureBindingLayout()
{
		public ChainedStruct chain;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUFragmentState()
{
		public ChainedStruct* nextInChain;
		public WGPUShaderModule module;
		public WGPUStringView entryPoint;
		public UIntPtr constantCount;
		public WGPUConstantEntry* constants;
		public UIntPtr targetCount;
		public WGPUColorTargetState* targets;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUFuture()
{
		public ulong id;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUFutureWaitInfo()
{
		public WGPUFuture future;
		public Bool32 completed;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUInstanceDescriptor()
{
		public ChainedStruct* nextInChain;
		public UIntPtr requiredFeatureCount;
		public WGPUInstanceFeatureName* requiredFeatures;
		public WGPUInstanceLimits* requiredLimits;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUInstanceLimits()
{
		public ChainedStruct* nextInChain;
		public UIntPtr timedWaitAnyMaxCount;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPULimits()
{
		public ChainedStruct* nextInChain;
		public uint maxTextureDimension1D = WebGPUNative.LimitU32Undefined;
		public uint maxTextureDimension2D = WebGPUNative.LimitU32Undefined;
		public uint maxTextureDimension3D = WebGPUNative.LimitU32Undefined;
		public uint maxTextureArrayLayers = WebGPUNative.LimitU32Undefined;
		public uint maxBindGroups = WebGPUNative.LimitU32Undefined;
		public uint maxBindGroupsPlusVertexBuffers = WebGPUNative.LimitU32Undefined;
		public uint maxBindingsPerBindGroup = WebGPUNative.LimitU32Undefined;
		public uint maxDynamicUniformBuffersPerPipelineLayout = WebGPUNative.LimitU32Undefined;
		public uint maxDynamicStorageBuffersPerPipelineLayout = WebGPUNative.LimitU32Undefined;
		public uint maxSampledTexturesPerShaderStage = WebGPUNative.LimitU32Undefined;
		public uint maxSamplersPerShaderStage = WebGPUNative.LimitU32Undefined;
		public uint maxStorageBuffersPerShaderStage = WebGPUNative.LimitU32Undefined;
		public uint maxStorageTexturesPerShaderStage = WebGPUNative.LimitU32Undefined;
		public uint maxUniformBuffersPerShaderStage = WebGPUNative.LimitU32Undefined;
		public ulong maxUniformBufferBindingSize = WebGPUNative.LimitU64Undefined;
		public ulong maxStorageBufferBindingSize = WebGPUNative.LimitU64Undefined;
		public uint minUniformBufferOffsetAlignment = WebGPUNative.LimitU32Undefined;
		public uint minStorageBufferOffsetAlignment = WebGPUNative.LimitU32Undefined;
		public uint maxVertexBuffers = WebGPUNative.LimitU32Undefined;
		public ulong maxBufferSize = WebGPUNative.LimitU64Undefined;
		public uint maxVertexAttributes = WebGPUNative.LimitU32Undefined;
		public uint maxVertexBufferArrayStride = WebGPUNative.LimitU32Undefined;
		public uint maxInterStageShaderVariables = WebGPUNative.LimitU32Undefined;
		public uint maxColorAttachments = WebGPUNative.LimitU32Undefined;
		public uint maxColorAttachmentBytesPerSample = WebGPUNative.LimitU32Undefined;
		public uint maxComputeWorkgroupStorageSize = WebGPUNative.LimitU32Undefined;
		public uint maxComputeInvocationsPerWorkgroup = WebGPUNative.LimitU32Undefined;
		public uint maxComputeWorkgroupSizeX = WebGPUNative.LimitU32Undefined;
		public uint maxComputeWorkgroupSizeY = WebGPUNative.LimitU32Undefined;
		public uint maxComputeWorkgroupSizeZ = WebGPUNative.LimitU32Undefined;
		public uint maxComputeWorkgroupsPerDimension = WebGPUNative.LimitU32Undefined;
		public uint maxImmediateSize = WebGPUNative.LimitU32Undefined;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUMultisampleState()
{
		public ChainedStruct* nextInChain;
		public uint count = 1;
		public uint mask = 0xFFFFFFFF;
		public Bool32 alphaToCoverageEnabled = false;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUOrigin3D()
{
		public uint x = 0;
		public uint y = 0;
		public uint z = 0;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUPassTimestampWrites()
{
		public ChainedStruct* nextInChain;
		public WGPUQuerySet querySet;
		public uint beginningOfPassWriteIndex = WebGPUNative.QuerySetIndexUndefined;
		public uint endOfPassWriteIndex = WebGPUNative.QuerySetIndexUndefined;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUPipelineLayoutDescriptor()
{
		public ChainedStruct* nextInChain;
		public WGPUStringView label;
		public UIntPtr bindGroupLayoutCount;
		public WGPUBindGroupLayout* bindGroupLayouts;
		public uint immediateSize = 0;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUPrimitiveState()
{
		public ChainedStruct* nextInChain;
		public WGPUPrimitiveTopology topology;
		public WGPUIndexFormat stripIndexFormat;
		public WGPUFrontFace frontFace;
		public WGPUCullMode cullMode;
		public Bool32 unclippedDepth = false;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUQuerySetDescriptor()
{
		public ChainedStruct* nextInChain;
		public WGPUStringView label;
		public WGPUQueryType type;
		public uint count;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUQueueDescriptor()
{
		public ChainedStruct* nextInChain;
		public WGPUStringView label;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPURenderBundleDescriptor()
{
		public ChainedStruct* nextInChain;
		public WGPUStringView label;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPURenderBundleEncoderDescriptor()
{
		public ChainedStruct* nextInChain;
		public WGPUStringView label;
		public UIntPtr colorFormatCount;
		public WGPUTextureFormat* colorFormats;
		public WGPUTextureFormat depthStencilFormat;
		public uint sampleCount = 1;
		public Bool32 depthReadOnly = false;
		public Bool32 stencilReadOnly = false;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPURenderPassColorAttachment()
{
		public ChainedStruct* nextInChain;
		public WGPUTextureView view;
		public uint depthSlice = WebGPUNative.DepthSliceUndefined;
		public WGPUTextureView resolveTarget;
		public WGPULoadOp loadOp;
		public WGPUStoreOp storeOp;
		public WGPUColor clearValue;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPURenderPassDepthStencilAttachment()
{
		public ChainedStruct* nextInChain;
		public WGPUTextureView view;
		public WGPULoadOp depthLoadOp;
		public WGPUStoreOp depthStoreOp;
		public float depthClearValue = WebGPUNative.DepthClearValueUndefined;
		public Bool32 depthReadOnly = false;
		public WGPULoadOp stencilLoadOp;
		public WGPUStoreOp stencilStoreOp;
		public uint stencilClearValue;
		public Bool32 stencilReadOnly = false;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPURenderPassDescriptor()
{
		public ChainedStruct* nextInChain;
		public WGPUStringView label;
		public UIntPtr colorAttachmentCount;
		public WGPURenderPassColorAttachment* colorAttachments;
		public WGPURenderPassDepthStencilAttachment* depthStencilAttachment;
		public WGPUQuerySet occlusionQuerySet;
		public WGPUPassTimestampWrites* timestampWrites;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPURenderPassMaxDrawCount()
{
		public ChainedStruct chain;
		public ulong maxDrawCount = 50000000;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPURenderPipelineDescriptor()
{
		public ChainedStruct* nextInChain;
		public WGPUStringView label;
		public WGPUPipelineLayout layout;
		public WGPUVertexState vertex;
		public WGPUPrimitiveState primitive;
		public WGPUDepthStencilState* depthStencil;
		public WGPUMultisampleState multisample;
		public WGPUFragmentState* fragment;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPURequestAdapterOptions()
{
		public ChainedStruct* nextInChain;
		public WGPUFeatureLevel featureLevel;
		public WGPUPowerPreference powerPreference;
		public Bool32 forceFallbackAdapter = false;
		public WGPUBackendType backendType;
		public WGPUSurface compatibleSurface;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPURequestAdapterWebXROptions()
{
		public ChainedStruct chain;
		public Bool32 xrCompatible = false;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSamplerBindingLayout()
{
		public ChainedStruct* nextInChain;
		public WGPUSamplerBindingType type;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSamplerDescriptor()
{
		public ChainedStruct* nextInChain;
		public WGPUStringView label;
		public WGPUAddressMode addressModeU;
		public WGPUAddressMode addressModeV;
		public WGPUAddressMode addressModeW;
		public WGPUFilterMode magFilter;
		public WGPUFilterMode minFilter;
		public WGPUMipmapFilterMode mipmapFilter;
		public float lodMinClamp = 0;
		public float lodMaxClamp = 32;
		public WGPUCompareFunction compare;
		public ushort maxAnisotropy = 1;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUShaderModuleDescriptor()
{
		public ChainedStruct* nextInChain;
		public WGPUStringView label;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUShaderSourceSPIRV()
{
		public ChainedStruct chain;
		public uint codeSize = 0;
		public uint* code;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUShaderSourceWGSL()
{
		public ChainedStruct chain;
		public WGPUStringView code;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUStencilFaceState()
{
		public WGPUCompareFunction compare;
		public WGPUStencilOperation failOp;
		public WGPUStencilOperation depthFailOp;
		public WGPUStencilOperation passOp;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUStorageTextureBindingLayout()
{
		public ChainedStruct* nextInChain;
		public WGPUStorageTextureAccess access;
		public WGPUTextureFormat format;
		public WGPUTextureViewDimension viewDimension;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSupportedFeatures()
{
		public UIntPtr featureCount;
		public WGPUFeatureName* features;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSupportedInstanceFeatures()
{
		public UIntPtr featureCount;
		public WGPUInstanceFeatureName* features;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSupportedWGSLLanguageFeatures()
{
		public UIntPtr featureCount;
		public WGPUWGSLLanguageFeatureName* features;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSurfaceCapabilities()
{
		public ChainedStruct* nextInChain;
		public WGPUTextureUsage usages;
		public UIntPtr formatCount;
		public WGPUTextureFormat* formats;
		public UIntPtr presentModeCount;
		public WGPUPresentMode* presentModes;
		public UIntPtr alphaModeCount;
		public WGPUCompositeAlphaMode* alphaModes;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSurfaceColorManagement()
{
		public ChainedStruct chain;
		public WGPUPredefinedColorSpace colorSpace;
		public WGPUToneMappingMode toneMappingMode;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSurfaceConfiguration()
{
		public ChainedStruct* nextInChain;
		public WGPUDevice device;
		public WGPUTextureFormat format;
		public WGPUTextureUsage usage = WGPUTextureUsage.RenderAttachment;
		public uint width;
		public uint height;
		public UIntPtr viewFormatCount;
		public WGPUTextureFormat* viewFormats;
		public WGPUCompositeAlphaMode alphaMode = WGPUCompositeAlphaMode.Auto;
		public WGPUPresentMode presentMode;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSurfaceDescriptor()
{
		public ChainedStruct* nextInChain;
		public WGPUStringView label;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSurfaceSourceAndroidNativeWindow()
{
		public ChainedStruct chain;
		public void* window;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSurfaceSourceMetalLayer()
{
		public ChainedStruct chain;
		public void* layer;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSurfaceSourceWaylandSurface()
{
		public ChainedStruct chain;
		public void* display;
		public void* surface;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSurfaceSourceWindowsHWND()
{
		public ChainedStruct chain;
		public void* hinstance;
		public void* hwnd;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSurfaceSourceXCBWindow()
{
		public ChainedStruct chain;
		public void* connection;
		public uint window;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSurfaceSourceXlibWindow()
{
		public ChainedStruct chain;
		public void* display;
		public ulong window;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSurfaceTexture()
{
		public ChainedStruct* nextInChain;
		public WGPUTexture texture;
		public WGPUSurfaceGetCurrentTextureStatus status;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUTexelCopyBufferInfo()
{
		public WGPUTexelCopyBufferLayout layout;
		public WGPUBuffer buffer;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUTexelCopyBufferLayout()
{
		public ulong offset = 0;
		public uint bytesPerRow = WebGPUNative.CopyStrideUndefined;
		public uint rowsPerImage = WebGPUNative.CopyStrideUndefined;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUTexelCopyTextureInfo()
{
		public WGPUTexture texture;
		public uint mipLevel = 0;
		public WGPUOrigin3D origin;
		public WGPUTextureAspect aspect;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUTextureBindingLayout()
{
		public ChainedStruct* nextInChain;
		public WGPUTextureSampleType sampleType;
		public WGPUTextureViewDimension viewDimension;
		public Bool32 multisampled = false;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUTextureBindingViewDimension()
{
		public ChainedStruct chain;
		public WGPUTextureViewDimension textureBindingViewDimension;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUTextureComponentSwizzle()
{
		public WGPUComponentSwizzle r;
		public WGPUComponentSwizzle g;
		public WGPUComponentSwizzle b;
		public WGPUComponentSwizzle a;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUTextureComponentSwizzleDescriptor()
{
		public ChainedStruct chain;
		public WGPUTextureComponentSwizzle swizzle;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUTextureDescriptor()
{
		public ChainedStruct* nextInChain;
		public WGPUStringView label;
		public WGPUTextureUsage usage = WGPUTextureUsage.None;
		public WGPUTextureDimension dimension;
		public WGPUExtent3D size;
		public WGPUTextureFormat format;
		public uint mipLevelCount = 1;
		public uint sampleCount = 1;
		public UIntPtr viewFormatCount;
		public WGPUTextureFormat* viewFormats;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUTextureViewDescriptor()
{
		public ChainedStruct* nextInChain;
		public WGPUStringView label;
		public WGPUTextureFormat format;
		public WGPUTextureViewDimension dimension;
		public uint baseMipLevel = 0;
		public uint mipLevelCount = WebGPUNative.MipLevelCountUndefined;
		public uint baseArrayLayer = 0;
		public uint arrayLayerCount = WebGPUNative.ArrayLayerCountUndefined;
		public WGPUTextureAspect aspect;
		public WGPUTextureUsage usage = WGPUTextureUsage.None;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUVertexAttribute()
{
		public ChainedStruct* nextInChain;
		public WGPUVertexFormat format;
		public ulong offset;
		public uint shaderLocation;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUVertexBufferLayout()
{
		public ChainedStruct* nextInChain;
		public WGPUVertexStepMode stepMode;
		public ulong arrayStride;
		public UIntPtr attributeCount;
		public WGPUVertexAttribute* attributes;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUVertexState()
{
		public ChainedStruct* nextInChain;
		public WGPUShaderModule module;
		public WGPUStringView entryPoint;
		public UIntPtr constantCount;
		public WGPUConstantEntry* constants;
		public UIntPtr bufferCount;
		public WGPUVertexBufferLayout* buffers;
}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct WGPUBufferMapCallbackInfo
	{
		public ChainedStruct* nextInChain;
		public WGPUCallbackMode mode;
		public WGPUBufferMapCallback callback;
		public void* userdata1;
		public void* userdata2;
	}
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct WGPUCompilationInfoCallbackInfo
	{
		public ChainedStruct* nextInChain;
		public WGPUCallbackMode mode;
		public WGPUCompilationInfoCallback callback;
		public void* userdata1;
		public void* userdata2;
	}
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct WGPUCreateComputePipelineAsyncCallbackInfo
	{
		public ChainedStruct* nextInChain;
		public WGPUCallbackMode mode;
		public WGPUCreateComputePipelineAsyncCallback callback;
		public void* userdata1;
		public void* userdata2;
	}
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct WGPUCreateRenderPipelineAsyncCallbackInfo
	{
		public ChainedStruct* nextInChain;
		public WGPUCallbackMode mode;
		public WGPUCreateRenderPipelineAsyncCallback callback;
		public void* userdata1;
		public void* userdata2;
	}
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct WGPUDeviceLostCallbackInfo
	{
		public ChainedStruct* nextInChain;
		public WGPUCallbackMode mode;
		public WGPUDeviceLostCallback callback;
		public void* userdata1;
		public void* userdata2;
	}
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct WGPUPopErrorScopeCallbackInfo
	{
		public ChainedStruct* nextInChain;
		public WGPUCallbackMode mode;
		public WGPUPopErrorScopeCallback callback;
		public void* userdata1;
		public void* userdata2;
	}
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct WGPUQueueWorkDoneCallbackInfo
	{
		public ChainedStruct* nextInChain;
		public WGPUCallbackMode mode;
		public WGPUQueueWorkDoneCallback callback;
		public void* userdata1;
		public void* userdata2;
	}
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct WGPURequestAdapterCallbackInfo
	{
		public ChainedStruct* nextInChain;
		public WGPUCallbackMode mode;
		public WGPURequestAdapterCallback callback;
		public void* userdata1;
		public void* userdata2;
	}
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct WGPURequestDeviceCallbackInfo
	{
		public ChainedStruct* nextInChain;
		public WGPUCallbackMode mode;
		public WGPURequestDeviceCallback callback;
		public void* userdata1;
		public void* userdata2;
	}
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct WGPUUncapturedErrorCallbackInfo
	{
		public ChainedStruct* nextInChain;
		public WGPUUncapturedErrorCallback callback;
		public void* userdata1;
		public void* userdata2;
	}
