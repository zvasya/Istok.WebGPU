namespace Istok.WebGPU.LowLevel;

public static unsafe partial class WebGPUNative
{
	const string WebGPULib = 
#if __IOS__
		"__Internal";
#else
		"wgpu_native";
#endif
}