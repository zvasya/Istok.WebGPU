using Silk.NET.Core;

namespace Istok.WebGPU.LowLevel;

public enum WGPUAdapterType
{
	DiscreteGPU = 1,
	IntegratedGPU = 2,
	CPU = 3,
	Unknown = 4,
}

public enum WGPUAddressMode
{
	Undefined = 0,
	ClampToEdge = 1,
	Repeat = 2,
	MirrorRepeat = 3,
}

public enum WGPUBackendType
{
	Undefined = 0,
	Null = 1,
	WebGPU = 2,
	D3D11 = 3,
	D3D12 = 4,
	Metal = 5,
	Vulkan = 6,
	OpenGL = 7,
	OpenGLES = 8,
}

public enum WGPUBlendFactor
{
	Undefined = 0,
	Zero = 1,
	One = 2,
	Src = 3,
	OneMinusSrc = 4,
	SrcAlpha = 5,
	OneMinusSrcAlpha = 6,
	Dst = 7,
	OneMinusDst = 8,
	DstAlpha = 9,
	OneMinusDstAlpha = 10,
	SrcAlphaSaturated = 11,
	Constant = 12,
	OneMinusConstant = 13,
	Src1 = 14,
	OneMinusSrc1 = 15,
	Src1Alpha = 16,
	OneMinusSrc1Alpha = 17,
}

public enum WGPUBlendOperation
{
	Undefined = 0,
	Add = 1,
	Subtract = 2,
	ReverseSubtract = 3,
	Min = 4,
	Max = 5,
}

public enum WGPUBufferBindingType
{
	BindingNotUsed = 0,
	Undefined = 1,
	Uniform = 2,
	Storage = 3,
	ReadOnlyStorage = 4,
}

public enum WGPUBufferMapState
{
	Unmapped = 1,
	Pending = 2,
	Mapped = 3,
}

public enum WGPUCallbackMode
{
	WaitAnyOnly = 1,
	AllowProcessEvents = 2,
	AllowSpontaneous = 3,
}

public enum WGPUCompareFunction
{
	Undefined = 0,
	Never = 1,
	Less = 2,
	Equal = 3,
	LessEqual = 4,
	Greater = 5,
	NotEqual = 6,
	GreaterEqual = 7,
	Always = 8,
}

public enum WGPUCompilationInfoRequestStatus
{
	Success = 1,
	CallbackCancelled = 2,
}

public enum WGPUCompilationMessageType
{
	Error = 1,
	Warning = 2,
	Info = 3,
}

public enum WGPUComponentSwizzle
{
	Undefined = 0,
	Zero = 1,
	One = 2,
	R = 3,
	G = 4,
	B = 5,
	A = 6,
}

public enum WGPUCompositeAlphaMode
{
	Auto = 0,
	Opaque = 1,
	Premultiplied = 2,
	Unpremultiplied = 3,
	Inherit = 4,
}

public enum WGPUCreatePipelineAsyncStatus
{
	Success = 1,
	CallbackCancelled = 2,
	ValidationError = 3,
	InternalError = 4,
}

public enum WGPUCullMode
{
	Undefined = 0,
	None = 1,
	Front = 2,
	Back = 3,
}

public enum WGPUDeviceLostReason
{
	Unknown = 1,
	Destroyed = 2,
	CallbackCancelled = 3,
	FailedCreation = 4,
}

public enum WGPUErrorFilter
{
	Validation = 1,
	OutOfMemory = 2,
	Internal = 3,
}

public enum WGPUErrorType
{
	NoError = 1,
	Validation = 2,
	OutOfMemory = 3,
	Internal = 4,
	Unknown = 5,
}

public enum WGPUFeatureLevel
{
	Undefined = 0,
	Compatibility = 1,
	Core = 2,
}

public enum WGPUFeatureName
{
	CoreFeaturesAndLimits = 1,
	DepthClipControl = 2,
	Depth32FloatStencil8 = 3,
	TextureCompressionBC = 4,
	TextureCompressionBCSliced3D = 5,
	TextureCompressionETC2 = 6,
	TextureCompressionASTC = 7,
	TextureCompressionASTCSliced3D = 8,
	TimestampQuery = 9,
	IndirectFirstInstance = 10,
	ShaderF16 = 11,
	RG11B10UfloatRenderable = 12,
	BGRA8UnormStorage = 13,
	Float32Filterable = 14,
	Float32Blendable = 15,
	ClipDistances = 16,
	DualSourceBlending = 17,
	Subgroups = 18,
	TextureFormatsTier1 = 19,
	TextureFormatsTier2 = 20,
	PrimitiveIndex = 21,
	TextureComponentSwizzle = 22,
}

public enum WGPUFilterMode
{
	Undefined = 0,
	Nearest = 1,
	Linear = 2,
}

public enum WGPUFrontFace
{
	Undefined = 0,
	CCW = 1,
	CW = 2,
}

public enum WGPUIndexFormat
{
	Undefined = 0,
	Uint16 = 1,
	Uint32 = 2,
}

public enum WGPUInstanceFeatureName
{
	TimedWaitAny = 1,
	ShaderSourceSPIRV = 2,
	MultipleDevicesPerAdapter = 3,
}

public enum WGPULoadOp
{
	Undefined = 0,
	Load = 1,
	Clear = 2,
}

public enum WGPUMapAsyncStatus
{
	Success = 1,
	CallbackCancelled = 2,
	Error = 3,
	Aborted = 4,
}

public enum WGPUMipmapFilterMode
{
	Undefined = 0,
	Nearest = 1,
	Linear = 2,
}

public enum WGPUOptionalBool
{
	False = 0,
	True = 1,
	Undefined = 2,
}

public enum WGPUPopErrorScopeStatus
{
	Success = 1,
	CallbackCancelled = 2,
	Error = 3,
}

public enum WGPUPowerPreference
{
	Undefined = 0,
	LowPower = 1,
	HighPerformance = 2,
}

public enum WGPUPredefinedColorSpace
{
	SRGB = 1,
	DisplayP3 = 2,
}

public enum WGPUPresentMode
{
	Undefined = 0,
	Fifo = 1,
	FifoRelaxed = 2,
	Immediate = 3,
	Mailbox = 4,
}

public enum WGPUPrimitiveTopology
{
	Undefined = 0,
	PointList = 1,
	LineList = 2,
	LineStrip = 3,
	TriangleList = 4,
	TriangleStrip = 5,
}

public enum WGPUQueryType
{
	Occlusion = 1,
	Timestamp = 2,
}

public enum WGPUQueueWorkDoneStatus
{
	Success = 1,
	CallbackCancelled = 2,
	Error = 3,
}

public enum WGPURequestAdapterStatus
{
	Success = 1,
	CallbackCancelled = 2,
	Unavailable = 3,
	Error = 4,
}

public enum WGPURequestDeviceStatus
{
	Success = 1,
	CallbackCancelled = 2,
	Error = 3,
}

public enum WGPUSType
{
	ShaderSourceSPIRV = 1,
	ShaderSourceWGSL = 2,
	RenderPassMaxDrawCount = 3,
	SurfaceSourceMetalLayer = 4,
	SurfaceSourceWindowsHWND = 5,
	SurfaceSourceXlibWindow = 6,
	SurfaceSourceWaylandSurface = 7,
	SurfaceSourceAndroidNativeWindow = 8,
	SurfaceSourceXCBWindow = 9,
	SurfaceColorManagement = 10,
	RequestAdapterWebXROptions = 11,
	TextureComponentSwizzleDescriptor = 12,
	ExternalTextureBindingLayout = 13,
	ExternalTextureBindingEntry = 14,
	CompatibilityModeLimits = 15,
	TextureBindingViewDimension = 16,
}

public enum WGPUSamplerBindingType
{
	BindingNotUsed = 0,
	Undefined = 1,
	Filtering = 2,
	NonFiltering = 3,
	Comparison = 4,
}

public enum WGPUStatus
{
	Success = 1,
	Error = 2,
}

public enum WGPUStencilOperation
{
	Undefined = 0,
	Keep = 1,
	Zero = 2,
	Replace = 3,
	Invert = 4,
	IncrementClamp = 5,
	DecrementClamp = 6,
	IncrementWrap = 7,
	DecrementWrap = 8,
}

public enum WGPUStorageTextureAccess
{
	BindingNotUsed = 0,
	Undefined = 1,
	WriteOnly = 2,
	ReadOnly = 3,
	ReadWrite = 4,
}

public enum WGPUStoreOp
{
	Undefined = 0,
	Store = 1,
	Discard = 2,
}

public enum WGPUSurfaceGetCurrentTextureStatus
{
	SuccessOptimal = 1,
	SuccessSuboptimal = 2,
	Timeout = 3,
	Outdated = 4,
	Lost = 5,
	Error = 6,
}

public enum WGPUTextureAspect
{
	Undefined = 0,
	All = 1,
	StencilOnly = 2,
	DepthOnly = 3,
}

public enum WGPUTextureDimension
{
	Undefined = 0,
	D1D = 1,
	D2D = 2,
	D3D = 3,
}

public enum WGPUTextureFormat
{
	Undefined = 0,
	R8Unorm = 1,
	R8Snorm = 2,
	R8Uint = 3,
	R8Sint = 4,
	R16Unorm = 5,
	R16Snorm = 6,
	R16Uint = 7,
	R16Sint = 8,
	R16Float = 9,
	RG8Unorm = 10,
	RG8Snorm = 11,
	RG8Uint = 12,
	RG8Sint = 13,
	R32Float = 14,
	R32Uint = 15,
	R32Sint = 16,
	RG16Unorm = 17,
	RG16Snorm = 18,
	RG16Uint = 19,
	RG16Sint = 20,
	RG16Float = 21,
	RGBA8Unorm = 22,
	RGBA8UnormSrgb = 23,
	RGBA8Snorm = 24,
	RGBA8Uint = 25,
	RGBA8Sint = 26,
	BGRA8Unorm = 27,
	BGRA8UnormSrgb = 28,
	RGB10A2Uint = 29,
	RGB10A2Unorm = 30,
	RG11B10Ufloat = 31,
	RGB9E5Ufloat = 32,
	RG32Float = 33,
	RG32Uint = 34,
	RG32Sint = 35,
	RGBA16Unorm = 36,
	RGBA16Snorm = 37,
	RGBA16Uint = 38,
	RGBA16Sint = 39,
	RGBA16Float = 40,
	RGBA32Float = 41,
	RGBA32Uint = 42,
	RGBA32Sint = 43,
	Stencil8 = 44,
	Depth16Unorm = 45,
	Depth24Plus = 46,
	Depth24PlusStencil8 = 47,
	Depth32Float = 48,
	Depth32FloatStencil8 = 49,
	BC1RGBAUnorm = 50,
	BC1RGBAUnormSrgb = 51,
	BC2RGBAUnorm = 52,
	BC2RGBAUnormSrgb = 53,
	BC3RGBAUnorm = 54,
	BC3RGBAUnormSrgb = 55,
	BC4RUnorm = 56,
	BC4RSnorm = 57,
	BC5RGUnorm = 58,
	BC5RGSnorm = 59,
	BC6HRGBUfloat = 60,
	BC6HRGBFloat = 61,
	BC7RGBAUnorm = 62,
	BC7RGBAUnormSrgb = 63,
	ETC2RGB8Unorm = 64,
	ETC2RGB8UnormSrgb = 65,
	ETC2RGB8A1Unorm = 66,
	ETC2RGB8A1UnormSrgb = 67,
	ETC2RGBA8Unorm = 68,
	ETC2RGBA8UnormSrgb = 69,
	EACR11Unorm = 70,
	EACR11Snorm = 71,
	EACRG11Unorm = 72,
	EACRG11Snorm = 73,
	ASTC4X4Unorm = 74,
	ASTC4X4UnormSrgb = 75,
	ASTC5X4Unorm = 76,
	ASTC5X4UnormSrgb = 77,
	ASTC5X5Unorm = 78,
	ASTC5X5UnormSrgb = 79,
	ASTC6X5Unorm = 80,
	ASTC6X5UnormSrgb = 81,
	ASTC6X6Unorm = 82,
	ASTC6X6UnormSrgb = 83,
	ASTC8X5Unorm = 84,
	ASTC8X5UnormSrgb = 85,
	ASTC8X6Unorm = 86,
	ASTC8X6UnormSrgb = 87,
	ASTC8X8Unorm = 88,
	ASTC8X8UnormSrgb = 89,
	ASTC10X5Unorm = 90,
	ASTC10X5UnormSrgb = 91,
	ASTC10X6Unorm = 92,
	ASTC10X6UnormSrgb = 93,
	ASTC10X8Unorm = 94,
	ASTC10X8UnormSrgb = 95,
	ASTC10X10Unorm = 96,
	ASTC10X10UnormSrgb = 97,
	ASTC12X10Unorm = 98,
	ASTC12X10UnormSrgb = 99,
	ASTC12X12Unorm = 100,
	ASTC12X12UnormSrgb = 101,
}

public enum WGPUTextureSampleType
{
	BindingNotUsed = 0,
	Undefined = 1,
	Float = 2,
	UnfilterableFloat = 3,
	Depth = 4,
	Sint = 5,
	Uint = 6,
}

public enum WGPUTextureViewDimension
{
	Undefined = 0,
	D1D = 1,
	D2D = 2,
	D2DArray = 3,
	Cube = 4,
	CubeArray = 5,
	D3D = 6,
}

public enum WGPUToneMappingMode
{
	Standard = 1,
	Extended = 2,
}

public enum WGPUVertexFormat
{
	Uint8 = 1,
	Uint8X2 = 2,
	Uint8X4 = 3,
	Sint8 = 4,
	Sint8X2 = 5,
	Sint8X4 = 6,
	Unorm8 = 7,
	Unorm8X2 = 8,
	Unorm8X4 = 9,
	Snorm8 = 10,
	Snorm8X2 = 11,
	Snorm8X4 = 12,
	Uint16 = 13,
	Uint16X2 = 14,
	Uint16X4 = 15,
	Sint16 = 16,
	Sint16X2 = 17,
	Sint16X4 = 18,
	Unorm16 = 19,
	Unorm16X2 = 20,
	Unorm16X4 = 21,
	Snorm16 = 22,
	Snorm16X2 = 23,
	Snorm16X4 = 24,
	Float16 = 25,
	Float16X2 = 26,
	Float16X4 = 27,
	Float32 = 28,
	Float32X2 = 29,
	Float32X3 = 30,
	Float32X4 = 31,
	Uint32 = 32,
	Uint32X2 = 33,
	Uint32X3 = 34,
	Uint32X4 = 35,
	Sint32 = 36,
	Sint32X2 = 37,
	Sint32X3 = 38,
	Sint32X4 = 39,
	Unorm1010102 = 40,
	Unorm8X4BGRA = 41,
}

public enum WGPUVertexStepMode
{
	Undefined = 0,
	Vertex = 1,
	Instance = 2,
}

public enum WGPUWaitStatus
{
	Success = 1,
	TimedOut = 2,
	Error = 3,
}

public enum WGPUWGSLLanguageFeatureName
{
	ReadonlyAndReadwriteStorageTextures = 1,
	Packed4X8IntegerDotProduct = 2,
	UnrestrictedPointerParameters = 3,
	PointerCompositeAccess = 4,
	UniformBufferStandardLayout = 5,
	SubgroupId = 6,
	TextureAndSamplerLet = 7,
	SubgroupUniformity = 8,
	TextureFormatsTier1 = 9,
	LinearIndexing = 10,
}

[Flags]
public enum WGPUBufferUsage : ulong
{
	None = 0,
	MapRead = 1,
	MapWrite = 2,
	CopySrc = 4,
	CopyDst = 8,
	Index = 16,
	Vertex = 32,
	Uniform = 64,
	Storage = 128,
	Indirect = 256,
	QueryResolve = 512,
}

[Flags]
public enum WGPUColorWriteMask : ulong
{
	None = 0,
	Red = 1,
	Green = 2,
	Blue = 4,
	Alpha = 8,
	All = Red | Green | Blue | Alpha,
}

[Flags]
public enum WGPUMapMode : ulong
{
	None = 0,
	Read = 1,
	Write = 2,
}

[Flags]
public enum WGPUShaderStage : ulong
{
	None = 0,
	Vertex = 1,
	Fragment = 2,
	Compute = 4,
}

[Flags]
public enum WGPUTextureUsage : ulong
{
	None = 0,
	CopySrc = 1,
	CopyDst = 2,
	TextureBinding = 4,
	StorageBinding = 8,
	RenderAttachment = 16,
	TransientAttachment = 32,
}

