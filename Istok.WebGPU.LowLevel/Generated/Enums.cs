using Silk.NET.Core;

namespace Istok.WebGPU.LowLevel;

public enum WGPUAdapterType
{
	DiscreteGPU = 0x00000001,
	IntegratedGPU = 0x00000002,
	CPU = 0x00000003,
	Unknown = 0x00000004,
}

public enum WGPUAddressMode
{
	Undefined = 0x00000000,
	ClampToEdge = 0x00000001,
	Repeat = 0x00000002,
	MirrorRepeat = 0x00000003,
}

public enum WGPUBackendType
{
	Undefined = 0x00000000,
	Null = 0x00000001,
	WebGPU = 0x00000002,
	D3D11 = 0x00000003,
	D3D12 = 0x00000004,
	Metal = 0x00000005,
	Vulkan = 0x00000006,
	OpenGL = 0x00000007,
	OpenGLES = 0x00000008,
}

public enum WGPUBlendFactor
{
	Undefined = 0x00000000,
	Zero = 0x00000001,
	One = 0x00000002,
	Src = 0x00000003,
	OneMinusSrc = 0x00000004,
	SrcAlpha = 0x00000005,
	OneMinusSrcAlpha = 0x00000006,
	Dst = 0x00000007,
	OneMinusDst = 0x00000008,
	DstAlpha = 0x00000009,
	OneMinusDstAlpha = 0x0000000A,
	SrcAlphaSaturated = 0x0000000B,
	Constant = 0x0000000C,
	OneMinusConstant = 0x0000000D,
	Src1 = 0x0000000E,
	OneMinusSrc1 = 0x0000000F,
	Src1Alpha = 0x00000010,
	OneMinusSrc1Alpha = 0x00000011,
}

public enum WGPUBlendOperation
{
	Undefined = 0x00000000,
	Add = 0x00000001,
	Subtract = 0x00000002,
	ReverseSubtract = 0x00000003,
	Min = 0x00000004,
	Max = 0x00000005,
}

public enum WGPUBufferBindingType
{
	BindingNotUsed = 0x00000000,
	Undefined = 0x00000001,
	Uniform = 0x00000002,
	Storage = 0x00000003,
	ReadOnlyStorage = 0x00000004,
}

public enum WGPUBufferMapState
{
	Unmapped = 0x00000001,
	Pending = 0x00000002,
	Mapped = 0x00000003,
}

public enum WGPUCallbackMode
{
	WaitAnyOnly = 0x00000001,
	AllowProcessEvents = 0x00000002,
	AllowSpontaneous = 0x00000003,
}

public enum WGPUCompareFunction
{
	Undefined = 0x00000000,
	Never = 0x00000001,
	Less = 0x00000002,
	Equal = 0x00000003,
	LessEqual = 0x00000004,
	Greater = 0x00000005,
	NotEqual = 0x00000006,
	GreaterEqual = 0x00000007,
	Always = 0x00000008,
}

public enum WGPUCompilationInfoRequestStatus
{
	Success = 0x00000001,
	CallbackCancelled = 0x00000002,
}

public enum WGPUCompilationMessageType
{
	Error = 0x00000001,
	Warning = 0x00000002,
	Info = 0x00000003,
}

public enum WGPUComponentSwizzle
{
	Undefined = 0x00000000,
	Zero = 0x00000001,
	One = 0x00000002,
	R = 0x00000003,
	G = 0x00000004,
	B = 0x00000005,
	A = 0x00000006,
}

public enum WGPUCompositeAlphaMode
{
	Auto = 0x00000000,
	Opaque = 0x00000001,
	Premultiplied = 0x00000002,
	Unpremultiplied = 0x00000003,
	Inherit = 0x00000004,
}

public enum WGPUCreatePipelineAsyncStatus
{
	Success = 0x00000001,
	CallbackCancelled = 0x00000002,
	ValidationError = 0x00000003,
	InternalError = 0x00000004,
}

public enum WGPUCullMode
{
	Undefined = 0x00000000,
	None = 0x00000001,
	Front = 0x00000002,
	Back = 0x00000003,
}

public enum WGPUDeviceLostReason
{
	Unknown = 0x00000001,
	Destroyed = 0x00000002,
	CallbackCancelled = 0x00000003,
	FailedCreation = 0x00000004,
}

public enum WGPUErrorFilter
{
	Validation = 0x00000001,
	OutOfMemory = 0x00000002,
	Internal = 0x00000003,
}

public enum WGPUErrorType
{
	NoError = 0x00000001,
	Validation = 0x00000002,
	OutOfMemory = 0x00000003,
	Internal = 0x00000004,
	Unknown = 0x00000005,
}

public enum WGPUFeatureLevel
{
	Undefined = 0x00000000,
	Compatibility = 0x00000001,
	Core = 0x00000002,
}

public enum WGPUFeatureName
{
	CoreFeaturesAndLimits = 0x00000001,
	DepthClipControl = 0x00000002,
	Depth32FloatStencil8 = 0x00000003,
	TextureCompressionBC = 0x00000004,
	TextureCompressionBCSliced3D = 0x00000005,
	TextureCompressionETC2 = 0x00000006,
	TextureCompressionASTC = 0x00000007,
	TextureCompressionASTCSliced3D = 0x00000008,
	TimestampQuery = 0x00000009,
	IndirectFirstInstance = 0x0000000A,
	ShaderF16 = 0x0000000B,
	RG11B10UfloatRenderable = 0x0000000C,
	BGRA8UnormStorage = 0x0000000D,
	Float32Filterable = 0x0000000E,
	Float32Blendable = 0x0000000F,
	ClipDistances = 0x00000010,
	DualSourceBlending = 0x00000011,
	Subgroups = 0x00000012,
	TextureFormatsTier1 = 0x00000013,
	TextureFormatsTier2 = 0x00000014,
	PrimitiveIndex = 0x00000015,
	TextureComponentSwizzle = 0x00000016,
	SubgroupSizeControl = 0x00000017,
	TextureCompressionUnaligned = 0x00000018,
}

public enum WGPUFilterMode
{
	Undefined = 0x00000000,
	Nearest = 0x00000001,
	Linear = 0x00000002,
}

public enum WGPUFrontFace
{
	Undefined = 0x00000000,
	CCW = 0x00000001,
	CW = 0x00000002,
}

public enum WGPUIndexFormat
{
	Undefined = 0x00000000,
	Uint16 = 0x00000001,
	Uint32 = 0x00000002,
}

public enum WGPUInstanceFeatureName
{
	TimedWaitAny = 0x00000001,
	ShaderSourceSPIRV = 0x00000002,
	MultipleDevicesPerAdapter = 0x00000003,
}

public enum WGPULoadOp
{
	Undefined = 0x00000000,
	Load = 0x00000001,
	Clear = 0x00000002,
}

public enum WGPUMapAsyncStatus
{
	Success = 0x00000001,
	CallbackCancelled = 0x00000002,
	Error = 0x00000003,
	Aborted = 0x00000004,
}

public enum WGPUMipmapFilterMode
{
	Undefined = 0x00000000,
	Nearest = 0x00000001,
	Linear = 0x00000002,
}

public enum WGPUOptionalBool
{
	False = 0x00000000,
	True = 0x00000001,
	Undefined = 0x00000002,
}

public enum WGPUPopErrorScopeStatus
{
	Success = 0x00000001,
	CallbackCancelled = 0x00000002,
	Error = 0x00000003,
}

public enum WGPUPowerPreference
{
	Undefined = 0x00000000,
	LowPower = 0x00000001,
	HighPerformance = 0x00000002,
}

public enum WGPUPredefinedColorSpace
{
	SRGB = 0x00000001,
	DisplayP3 = 0x00000002,
}

public enum WGPUPresentMode
{
	Undefined = 0x00000000,
	Fifo = 0x00000001,
	FifoRelaxed = 0x00000002,
	Immediate = 0x00000003,
	Mailbox = 0x00000004,
}

public enum WGPUPrimitiveTopology
{
	Undefined = 0x00000000,
	PointList = 0x00000001,
	LineList = 0x00000002,
	LineStrip = 0x00000003,
	TriangleList = 0x00000004,
	TriangleStrip = 0x00000005,
}

public enum WGPUQueryType
{
	Occlusion = 0x00000001,
	Timestamp = 0x00000002,
}

public enum WGPUQueueWorkDoneStatus
{
	Success = 0x00000001,
	CallbackCancelled = 0x00000002,
	Error = 0x00000003,
}

public enum WGPURequestAdapterStatus
{
	Success = 0x00000001,
	CallbackCancelled = 0x00000002,
	Unavailable = 0x00000003,
	Error = 0x00000004,
}

public enum WGPURequestDeviceStatus
{
	Success = 0x00000001,
	CallbackCancelled = 0x00000002,
	Error = 0x00000003,
}

public enum WGPUSamplerBindingType
{
	BindingNotUsed = 0x00000000,
	Undefined = 0x00000001,
	Filtering = 0x00000002,
	NonFiltering = 0x00000003,
	Comparison = 0x00000004,
}

public enum WGPUStatus
{
	Success = 0x00000001,
	Error = 0x00000002,
}

public enum WGPUStencilOperation
{
	Undefined = 0x00000000,
	Keep = 0x00000001,
	Zero = 0x00000002,
	Replace = 0x00000003,
	Invert = 0x00000004,
	IncrementClamp = 0x00000005,
	DecrementClamp = 0x00000006,
	IncrementWrap = 0x00000007,
	DecrementWrap = 0x00000008,
}

public enum WGPUStorageTextureAccess
{
	BindingNotUsed = 0x00000000,
	Undefined = 0x00000001,
	WriteOnly = 0x00000002,
	ReadOnly = 0x00000003,
	ReadWrite = 0x00000004,
}

public enum WGPUStoreOp
{
	Undefined = 0x00000000,
	Store = 0x00000001,
	Discard = 0x00000002,
}

public enum WGPUSType
{
	ShaderSourceSPIRV = 0x00000001,
	ShaderSourceWGSL = 0x00000002,
	RenderPassMaxDrawCount = 0x00000003,
	SurfaceSourceMetalLayer = 0x00000004,
	SurfaceSourceWindowsHWND = 0x00000005,
	SurfaceSourceXlibWindow = 0x00000006,
	SurfaceSourceWaylandSurface = 0x00000007,
	SurfaceSourceAndroidNativeWindow = 0x00000008,
	SurfaceSourceXCBWindow = 0x00000009,
	SurfaceColorManagement = 0x0000000A,
	RequestAdapterWebXROptions = 0x0000000B,
	TextureComponentSwizzleDescriptor = 0x0000000C,
	ExternalTextureBindingLayout = 0x0000000D,
	ExternalTextureBindingEntry = 0x0000000E,
	CompatibilityModeLimits = 0x0000000F,
	TextureBindingViewDimension = 0x00000010,
}

public enum WGPUSurfaceGetCurrentTextureStatus
{
	SuccessOptimal = 0x00000001,
	SuccessSuboptimal = 0x00000002,
	Timeout = 0x00000003,
	Outdated = 0x00000004,
	Lost = 0x00000005,
	Error = 0x00000006,
}

public enum WGPUTextureAspect
{
	Undefined = 0x00000000,
	All = 0x00000001,
	StencilOnly = 0x00000002,
	DepthOnly = 0x00000003,
}

public enum WGPUTextureDimension
{
	Undefined = 0x00000000,
	D1D = 0x00000001,
	D2D = 0x00000002,
	D3D = 0x00000003,
}

public enum WGPUTextureFormat
{
	Undefined = 0x00000000,
	R8Unorm = 0x00000001,
	R8Snorm = 0x00000002,
	R8Uint = 0x00000003,
	R8Sint = 0x00000004,
	R16Unorm = 0x00000005,
	R16Snorm = 0x00000006,
	R16Uint = 0x00000007,
	R16Sint = 0x00000008,
	R16Float = 0x00000009,
	RG8Unorm = 0x0000000A,
	RG8Snorm = 0x0000000B,
	RG8Uint = 0x0000000C,
	RG8Sint = 0x0000000D,
	R32Float = 0x0000000E,
	R32Uint = 0x0000000F,
	R32Sint = 0x00000010,
	RG16Unorm = 0x00000011,
	RG16Snorm = 0x00000012,
	RG16Uint = 0x00000013,
	RG16Sint = 0x00000014,
	RG16Float = 0x00000015,
	RGBA8Unorm = 0x00000016,
	RGBA8UnormSrgb = 0x00000017,
	RGBA8Snorm = 0x00000018,
	RGBA8Uint = 0x00000019,
	RGBA8Sint = 0x0000001A,
	BGRA8Unorm = 0x0000001B,
	BGRA8UnormSrgb = 0x0000001C,
	RGB10A2Uint = 0x0000001D,
	RGB10A2Unorm = 0x0000001E,
	RG11B10Ufloat = 0x0000001F,
	RGB9E5Ufloat = 0x00000020,
	RG32Float = 0x00000021,
	RG32Uint = 0x00000022,
	RG32Sint = 0x00000023,
	RGBA16Unorm = 0x00000024,
	RGBA16Snorm = 0x00000025,
	RGBA16Uint = 0x00000026,
	RGBA16Sint = 0x00000027,
	RGBA16Float = 0x00000028,
	RGBA32Float = 0x00000029,
	RGBA32Uint = 0x0000002A,
	RGBA32Sint = 0x0000002B,
	Stencil8 = 0x0000002C,
	Depth16Unorm = 0x0000002D,
	Depth24Plus = 0x0000002E,
	Depth24PlusStencil8 = 0x0000002F,
	Depth32Float = 0x00000030,
	Depth32FloatStencil8 = 0x00000031,
	BC1RGBAUnorm = 0x00000032,
	BC1RGBAUnormSrgb = 0x00000033,
	BC2RGBAUnorm = 0x00000034,
	BC2RGBAUnormSrgb = 0x00000035,
	BC3RGBAUnorm = 0x00000036,
	BC3RGBAUnormSrgb = 0x00000037,
	BC4RUnorm = 0x00000038,
	BC4RSnorm = 0x00000039,
	BC5RGUnorm = 0x0000003A,
	BC5RGSnorm = 0x0000003B,
	BC6HRGBUfloat = 0x0000003C,
	BC6HRGBFloat = 0x0000003D,
	BC7RGBAUnorm = 0x0000003E,
	BC7RGBAUnormSrgb = 0x0000003F,
	ETC2RGB8Unorm = 0x00000040,
	ETC2RGB8UnormSrgb = 0x00000041,
	ETC2RGB8A1Unorm = 0x00000042,
	ETC2RGB8A1UnormSrgb = 0x00000043,
	ETC2RGBA8Unorm = 0x00000044,
	ETC2RGBA8UnormSrgb = 0x00000045,
	EACR11Unorm = 0x00000046,
	EACR11Snorm = 0x00000047,
	EACRG11Unorm = 0x00000048,
	EACRG11Snorm = 0x00000049,
	ASTC4X4Unorm = 0x0000004A,
	ASTC4X4UnormSrgb = 0x0000004B,
	ASTC5X4Unorm = 0x0000004C,
	ASTC5X4UnormSrgb = 0x0000004D,
	ASTC5X5Unorm = 0x0000004E,
	ASTC5X5UnormSrgb = 0x0000004F,
	ASTC6X5Unorm = 0x00000050,
	ASTC6X5UnormSrgb = 0x00000051,
	ASTC6X6Unorm = 0x00000052,
	ASTC6X6UnormSrgb = 0x00000053,
	ASTC8X5Unorm = 0x00000054,
	ASTC8X5UnormSrgb = 0x00000055,
	ASTC8X6Unorm = 0x00000056,
	ASTC8X6UnormSrgb = 0x00000057,
	ASTC8X8Unorm = 0x00000058,
	ASTC8X8UnormSrgb = 0x00000059,
	ASTC10X5Unorm = 0x0000005A,
	ASTC10X5UnormSrgb = 0x0000005B,
	ASTC10X6Unorm = 0x0000005C,
	ASTC10X6UnormSrgb = 0x0000005D,
	ASTC10X8Unorm = 0x0000005E,
	ASTC10X8UnormSrgb = 0x0000005F,
	ASTC10X10Unorm = 0x00000060,
	ASTC10X10UnormSrgb = 0x00000061,
	ASTC12X10Unorm = 0x00000062,
	ASTC12X10UnormSrgb = 0x00000063,
	ASTC12X12Unorm = 0x00000064,
	ASTC12X12UnormSrgb = 0x00000065,
}

public enum WGPUTextureSampleType
{
	BindingNotUsed = 0x00000000,
	Undefined = 0x00000001,
	Float = 0x00000002,
	UnfilterableFloat = 0x00000003,
	Depth = 0x00000004,
	Sint = 0x00000005,
	Uint = 0x00000006,
}

public enum WGPUTextureViewDimension
{
	Undefined = 0x00000000,
	D1D = 0x00000001,
	D2D = 0x00000002,
	D2DArray = 0x00000003,
	Cube = 0x00000004,
	CubeArray = 0x00000005,
	D3D = 0x00000006,
}

public enum WGPUToneMappingMode
{
	Standard = 0x00000001,
	Extended = 0x00000002,
}

public enum WGPUVertexFormat
{
	Uint8 = 0x00000001,
	Uint8X2 = 0x00000002,
	Uint8X4 = 0x00000003,
	Sint8 = 0x00000004,
	Sint8X2 = 0x00000005,
	Sint8X4 = 0x00000006,
	Unorm8 = 0x00000007,
	Unorm8X2 = 0x00000008,
	Unorm8X4 = 0x00000009,
	Snorm8 = 0x0000000A,
	Snorm8X2 = 0x0000000B,
	Snorm8X4 = 0x0000000C,
	Uint16 = 0x0000000D,
	Uint16X2 = 0x0000000E,
	Uint16X4 = 0x0000000F,
	Sint16 = 0x00000010,
	Sint16X2 = 0x00000011,
	Sint16X4 = 0x00000012,
	Unorm16 = 0x00000013,
	Unorm16X2 = 0x00000014,
	Unorm16X4 = 0x00000015,
	Snorm16 = 0x00000016,
	Snorm16X2 = 0x00000017,
	Snorm16X4 = 0x00000018,
	Float16 = 0x00000019,
	Float16X2 = 0x0000001A,
	Float16X4 = 0x0000001B,
	Float32 = 0x0000001C,
	Float32X2 = 0x0000001D,
	Float32X3 = 0x0000001E,
	Float32X4 = 0x0000001F,
	Uint32 = 0x00000020,
	Uint32X2 = 0x00000021,
	Uint32X3 = 0x00000022,
	Uint32X4 = 0x00000023,
	Sint32 = 0x00000024,
	Sint32X2 = 0x00000025,
	Sint32X3 = 0x00000026,
	Sint32X4 = 0x00000027,
	Unorm1010102 = 0x00000028,
	Unorm8X4BGRA = 0x00000029,
	Snorm1010102 = 0x0000002A,
}

public enum WGPUVertexStepMode
{
	Undefined = 0x00000000,
	Vertex = 0x00000001,
	Instance = 0x00000002,
}

public enum WGPUWaitStatus
{
	Success = 0x00000001,
	TimedOut = 0x00000002,
	Error = 0x00000003,
}

public enum WGPUWGSLLanguageFeatureName
{
	ReadonlyAndReadwriteStorageTextures = 0x00000001,
	Packed4X8IntegerDotProduct = 0x00000002,
	UnrestrictedPointerParameters = 0x00000003,
	PointerCompositeAccess = 0x00000004,
	UniformBufferStandardLayout = 0x00000005,
	SubgroupId = 0x00000006,
	TextureAndSamplerLet = 0x00000007,
	SubgroupUniformity = 0x00000008,
	TextureFormatsTier1 = 0x00000009,
	LinearIndexing = 0x0000000A,
	ImmediateAddressSpace = 0x0000000B,
	BufferView = 0x0000000C,
	SwizzleAssignment = 0x0000000D,
	FragmentDepth = 0x0000000E,
}

[Flags]
public enum WGPUBufferUsage : ulong
{
	None = 0x0000000000000000,
	MapRead = 0x0000000000000001,
	MapWrite = 0x0000000000000002,
	CopySrc = 0x0000000000000004,
	CopyDst = 0x0000000000000008,
	Index = 0x0000000000000010,
	Vertex = 0x0000000000000020,
	Uniform = 0x0000000000000040,
	Storage = 0x0000000000000080,
	Indirect = 0x0000000000000100,
	QueryResolve = 0x0000000000000200,
}

[Flags]
public enum WGPUColorWriteMask : ulong
{
	None = 0x0000000000000000,
	Red = 0x0000000000000001,
	Green = 0x0000000000000002,
	Blue = 0x0000000000000004,
	Alpha = 0x0000000000000008,
	All = 0x000000000000000F,
}

[Flags]
public enum WGPUMapMode : ulong
{
	None = 0x0000000000000000,
	Read = 0x0000000000000001,
	Write = 0x0000000000000002,
}

[Flags]
public enum WGPUShaderStage : ulong
{
	None = 0x0000000000000000,
	Vertex = 0x0000000000000001,
	Fragment = 0x0000000000000002,
	Compute = 0x0000000000000004,
}

[Flags]
public enum WGPUTextureUsage : ulong
{
	None = 0x0000000000000000,
	CopySrc = 0x0000000000000001,
	CopyDst = 0x0000000000000002,
	TextureBinding = 0x0000000000000004,
	StorageBinding = 0x0000000000000008,
	RenderAttachment = 0x0000000000000010,
	TransientAttachment = 0x0000000000000020,
}

