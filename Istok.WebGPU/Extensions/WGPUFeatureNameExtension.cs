namespace Istok.WebGPU;

public static class WGPUFeatureNameExtension
{
	extension(WGPUFeatureName)
	{
		public static WGPUFeatureName Immediates => (WGPUFeatureName)0x00030001;
		public static WGPUFeatureName TextureAdapterSpecificFormatFeatures => (WGPUFeatureName)0x00030002;
		public static WGPUFeatureName PipelineStatisticsQuery => (WGPUFeatureName)0x00030005;
		public static WGPUFeatureName TextureFormat16BitNorm => (WGPUFeatureName)0x00030006;
		public static WGPUFeatureName TextureCompressionAstcHdr => (WGPUFeatureName)0x00030007;
		public static WGPUFeatureName MappablePrimaryBuffers => (WGPUFeatureName)0x00030009;
		public static WGPUFeatureName BufferBindingArray => (WGPUFeatureName)0x0003000A;
		public static WGPUFeatureName UniformBufferAndStorageTextureArrayIndexing => (WGPUFeatureName)0x0003000B;
		public static WGPUFeatureName VertexStorage => (WGPUFeatureName)0x0003000C;
		public static WGPUFeatureName TextureBindingArray => (WGPUFeatureName)0x0003000E;
		public static WGPUFeatureName StorageResourceBindingArray => (WGPUFeatureName)0x00030013;
		public static WGPUFeatureName Subgroup => (WGPUFeatureName)0x0003001C;
		public static WGPUFeatureName RayTracingAccelerationStructure => (WGPUFeatureName)0x00030021;
		public static WGPUFeatureName RayQuery => (WGPUFeatureName)0x00030023;
		public static WGPUFeatureName ShaderUnusedVertexOutput => (WGPUFeatureName)0x00030024;
		public static WGPUFeatureName VertexAttribute64Bit => (WGPUFeatureName)0x00030026;
	}
}