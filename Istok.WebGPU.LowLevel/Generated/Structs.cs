using System.Runtime.InteropServices;
using Silk.NET.Core;

namespace Istok.WebGPU.LowLevel;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUBufferMapCallbackInfo()
{
	public ChainedStruct* nextInChain;
	public WGPUCallbackMode mode;
	public WGPUBufferMapCallback callback;
	public void* userdata1;
	public void* userdata2;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUCompilationInfoCallbackInfo()
{
	public ChainedStruct* nextInChain;
	public WGPUCallbackMode mode;
	public WGPUCompilationInfoCallback callback;
	public void* userdata1;
	public void* userdata2;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUCreateComputePipelineAsyncCallbackInfo()
{
	public ChainedStruct* nextInChain;
	public WGPUCallbackMode mode;
	public WGPUCreateComputePipelineAsyncCallback callback;
	public void* userdata1;
	public void* userdata2;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUCreateRenderPipelineAsyncCallbackInfo()
{
	public ChainedStruct* nextInChain;
	public WGPUCallbackMode mode;
	public WGPUCreateRenderPipelineAsyncCallback callback;
	public void* userdata1;
	public void* userdata2;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUDeviceLostCallbackInfo()
{
	public ChainedStruct* nextInChain;
	public WGPUCallbackMode mode;
	public WGPUDeviceLostCallback callback;
	public void* userdata1;
	public void* userdata2;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUPopErrorScopeCallbackInfo()
{
	public ChainedStruct* nextInChain;
	public WGPUCallbackMode mode;
	public WGPUPopErrorScopeCallback callback;
	public void* userdata1;
	public void* userdata2;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUQueueWorkDoneCallbackInfo()
{
	public ChainedStruct* nextInChain;
	public WGPUCallbackMode mode;
	public WGPUQueueWorkDoneCallback callback;
	public void* userdata1;
	public void* userdata2;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPURequestAdapterCallbackInfo()
{
	public ChainedStruct* nextInChain;
	public WGPUCallbackMode mode;
	public WGPURequestAdapterCallback callback;
	public void* userdata1;
	public void* userdata2;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPURequestDeviceCallbackInfo()
{
	public ChainedStruct* nextInChain;
	public WGPUCallbackMode mode;
	public WGPURequestDeviceCallback callback;
	public void* userdata1;
	public void* userdata2;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUUncapturedErrorCallbackInfo()
{
	public ChainedStruct* nextInChain;
	public WGPUUncapturedErrorCallback callback;
	public void* userdata1;
	public void* userdata2;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUAdapterInfo()
{
	public ChainedStruct* nextInChain;
	public WGPUStringView vendor = new();
	public WGPUStringView architecture = new();
	public WGPUStringView device = new();
	public WGPUStringView description = new();
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
	public WGPUStringView label = new();
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
	public WGPUStringView label = new();
	public UIntPtr entryCount;
	public WGPUBindGroupLayoutEntry* entries;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUBindGroupLayoutEntry()
{
	public ChainedStruct* nextInChain;
	public uint binding;
	public WGPUShaderStage visibility;
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
	public WGPUBufferBindingType type = WGPUBufferBindingType.Undefined;
	public Bool32 hasDynamicOffset;
	public ulong minBindingSize;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUBufferDescriptor()
{
	public ChainedStruct* nextInChain;
	public WGPUStringView label = new();
	public WGPUBufferUsage usage;
	public ulong size;
	public Bool32 mappedAtCreation;
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
	public WGPUStringView label = new();
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUCommandEncoderDescriptor()
{
	public ChainedStruct* nextInChain;
	public WGPUStringView label = new();
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
	public WGPUStringView message = new();
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
	public WGPUStringView label = new();
	public WGPUPassTimestampWrites* timestampWrites;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUComputePipelineDescriptor()
{
	public ChainedStruct* nextInChain;
	public WGPUStringView label = new();
	public WGPUPipelineLayout layout;
	public WGPUComputeState compute = new();
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUComputeState()
{
	public ChainedStruct* nextInChain;
	public WGPUShaderModule module;
	public WGPUStringView entryPoint = new();
	public UIntPtr constantCount;
	public WGPUConstantEntry* constants;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUConstantEntry()
{
	public ChainedStruct* nextInChain;
	public WGPUStringView key = new();
	public double value;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUDepthStencilState()
{
	public ChainedStruct* nextInChain;
	public WGPUTextureFormat format;
	public WGPUOptionalBool depthWriteEnabled = WGPUOptionalBool.Undefined;
	public WGPUCompareFunction depthCompare;
	public WGPUStencilFaceState stencilFront;
	public WGPUStencilFaceState stencilBack;
	public uint stencilReadMask = 0xFFFFFFFF;
	public uint stencilWriteMask = 0xFFFFFFFF;
	public int depthBias;
	public float depthBiasSlopeScale;
	public float depthBiasClamp;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUDeviceDescriptor()
{
	public ChainedStruct* nextInChain;
	public WGPUStringView label = new();
	public UIntPtr requiredFeatureCount;
	public WGPUFeatureName* requiredFeatures;
	public WGPULimits* requiredLimits;
	public WGPUQueueDescriptor defaultQueue = new();
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
public unsafe struct WGPUFragmentState()
{
	public ChainedStruct* nextInChain;
	public WGPUShaderModule module;
	public WGPUStringView entryPoint = new();
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
	public Bool32 alphaToCoverageEnabled;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUOrigin3D()
{
	public uint x;
	public uint y;
	public uint z;
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
	public WGPUStringView label = new();
	public UIntPtr bindGroupLayoutCount;
	public WGPUBindGroupLayout* bindGroupLayouts;
	public uint immediateSize;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUPrimitiveState()
{
	public ChainedStruct* nextInChain;
	public WGPUPrimitiveTopology topology;
	public WGPUIndexFormat stripIndexFormat;
	public WGPUFrontFace frontFace;
	public WGPUCullMode cullMode;
	public Bool32 unclippedDepth;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUQuerySetDescriptor()
{
	public ChainedStruct* nextInChain;
	public WGPUStringView label = new();
	public WGPUQueryType type;
	public uint count;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUQueueDescriptor()
{
	public ChainedStruct* nextInChain;
	public WGPUStringView label = new();
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPURenderBundleDescriptor()
{
	public ChainedStruct* nextInChain;
	public WGPUStringView label = new();
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPURenderBundleEncoderDescriptor()
{
	public ChainedStruct* nextInChain;
	public WGPUStringView label = new();
	public UIntPtr colorFormatCount;
	public WGPUTextureFormat* colorFormats;
	public WGPUTextureFormat depthStencilFormat;
	public uint sampleCount = 1;
	public Bool32 depthReadOnly;
	public Bool32 stencilReadOnly;
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
	public Bool32 depthReadOnly;
	public WGPULoadOp stencilLoadOp;
	public WGPUStoreOp stencilStoreOp;
	public uint stencilClearValue;
	public Bool32 stencilReadOnly;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPURenderPassDescriptor()
{
	public ChainedStruct* nextInChain;
	public WGPUStringView label = new();
	public UIntPtr colorAttachmentCount;
	public WGPURenderPassColorAttachment* colorAttachments;
	public WGPURenderPassDepthStencilAttachment* depthStencilAttachment;
	public WGPUQuerySet occlusionQuerySet;
	public WGPUPassTimestampWrites* timestampWrites;
}

// Can be chained in WGPURenderPassDescriptor
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPURenderPassMaxDrawCount()
{
	public ChainedStruct chain = new() { sType = WGPUSType.RenderPassMaxDrawCount };
	public ulong maxDrawCount = 50000000;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPURenderPipelineDescriptor()
{
	public ChainedStruct* nextInChain;
	public WGPUStringView label = new();
	public WGPUPipelineLayout layout;
	public WGPUVertexState vertex = new();
	public WGPUPrimitiveState primitive;
	public WGPUDepthStencilState* depthStencil;
	public WGPUMultisampleState multisample = new();
	public WGPUFragmentState* fragment;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPURequestAdapterOptions()
{
	public ChainedStruct* nextInChain;
	public WGPUFeatureLevel featureLevel;
	public WGPUPowerPreference powerPreference;
	public Bool32 forceFallbackAdapter;
	public WGPUBackendType backendType;
	public WGPUSurface compatibleSurface;
}

// Can be chained in WGPURequestAdapterOptions
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPURequestAdapterWebXROptions()
{
	public ChainedStruct chain = new() { sType = WGPUSType.RequestAdapterWebXROptions };
	public Bool32 xrCompatible;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSamplerBindingLayout()
{
	public ChainedStruct* nextInChain;
	public WGPUSamplerBindingType type = WGPUSamplerBindingType.Undefined;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSamplerDescriptor()
{
	public ChainedStruct* nextInChain;
	public WGPUStringView label = new();
	public WGPUAddressMode addressModeU;
	public WGPUAddressMode addressModeV;
	public WGPUAddressMode addressModeW;
	public WGPUFilterMode magFilter;
	public WGPUFilterMode minFilter;
	public WGPUMipmapFilterMode mipmapFilter;
	public float lodMinClamp;
	public float lodMaxClamp = 32f;
	public WGPUCompareFunction compare;
	public ushort maxAnisotropy = 1;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUShaderModuleDescriptor()
{
	public ChainedStruct* nextInChain;
	public WGPUStringView label = new();
}

// Can be chained in WGPUShaderModuleDescriptor
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUShaderSourceSPIRV()
{
	public ChainedStruct chain = new() { sType = WGPUSType.ShaderSourceSPIRV };
	public uint codeSize;
	public uint* code;
}

// Can be chained in WGPUShaderModuleDescriptor
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUShaderSourceWGSL()
{
	public ChainedStruct chain = new() { sType = WGPUSType.ShaderSourceWGSL };
	public WGPUStringView code = new();
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
	public WGPUStorageTextureAccess access = WGPUStorageTextureAccess.Undefined;
	public WGPUTextureFormat format;
	public WGPUTextureViewDimension viewDimension;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUStringView()
{
	public byte* data;
	public UIntPtr length = WebGPUNative.Strlen;
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

// Can be chained in WGPUSurfaceDescriptor
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSurfaceColorManagement()
{
	public ChainedStruct chain = new() { sType = WGPUSType.SurfaceColorManagement };
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
	public WGPUCompositeAlphaMode alphaMode;
	public WGPUPresentMode presentMode;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSurfaceDescriptor()
{
	public ChainedStruct* nextInChain;
	public WGPUStringView label = new();
}

// Can be chained in WGPUSurfaceDescriptor
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSurfaceSourceAndroidNativeWindow()
{
	public ChainedStruct chain = new() { sType = WGPUSType.SurfaceSourceAndroidNativeWindow };
	public void* window;
}

// Can be chained in WGPUSurfaceDescriptor
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSurfaceSourceMetalLayer()
{
	public ChainedStruct chain = new() { sType = WGPUSType.SurfaceSourceMetalLayer };
	public void* layer;
}

// Can be chained in WGPUSurfaceDescriptor
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSurfaceSourceWaylandSurface()
{
	public ChainedStruct chain = new() { sType = WGPUSType.SurfaceSourceWaylandSurface };
	public void* display;
	public void* surface;
}

// Can be chained in WGPUSurfaceDescriptor
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSurfaceSourceWindowsHWND()
{
	public ChainedStruct chain = new() { sType = WGPUSType.SurfaceSourceWindowsHWND };
	public void* hinstance;
	public void* hwnd;
}

// Can be chained in WGPUSurfaceDescriptor
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSurfaceSourceXCBWindow()
{
	public ChainedStruct chain = new() { sType = WGPUSType.SurfaceSourceXCBWindow };
	public void* connection;
	public uint window;
}

// Can be chained in WGPUSurfaceDescriptor
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUSurfaceSourceXlibWindow()
{
	public ChainedStruct chain = new() { sType = WGPUSType.SurfaceSourceXlibWindow };
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
	public WGPUTexelCopyBufferLayout layout = new();
	public WGPUBuffer buffer;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUTexelCopyBufferLayout()
{
	public ulong offset;
	public uint bytesPerRow = WebGPUNative.CopyStrideUndefined;
	public uint rowsPerImage = WebGPUNative.CopyStrideUndefined;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUTexelCopyTextureInfo()
{
	public WGPUTexture texture;
	public uint mipLevel;
	public WGPUOrigin3D origin;
	public WGPUTextureAspect aspect;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUTextureBindingLayout()
{
	public ChainedStruct* nextInChain;
	public WGPUTextureSampleType sampleType = WGPUTextureSampleType.Undefined;
	public WGPUTextureViewDimension viewDimension;
	public Bool32 multisampled;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUTextureDescriptor()
{
	public ChainedStruct* nextInChain;
	public WGPUStringView label = new();
	public WGPUTextureUsage usage;
	public WGPUTextureDimension dimension;
	public WGPUExtent3D size = new();
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
	public WGPUStringView label = new();
	public WGPUTextureFormat format;
	public WGPUTextureViewDimension dimension;
	public uint baseMipLevel;
	public uint mipLevelCount = WebGPUNative.MipLevelCountUndefined;
	public uint baseArrayLayer;
	public uint arrayLayerCount = WebGPUNative.ArrayLayerCountUndefined;
	public WGPUTextureAspect aspect;
	public WGPUTextureUsage usage;
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
	public WGPUStringView entryPoint = new();
	public UIntPtr constantCount;
	public WGPUConstantEntry* constants;
	public UIntPtr bufferCount;
	public WGPUVertexBufferLayout* buffers;
}

