namespace Istok.WebGPU.LowLevel;

public static unsafe partial class WebGPUNative
{
	const string WebGPULib = 
#if __IOS__
		"@rpath/webgpu_dawn.framework/webgpu_dawn";
#else
		"webgpu_dawn";
#endif
}
