using Silk.NET.Core;

namespace Istok.WebGPU.LowLevel;

public static unsafe partial class WebGPUNative
{
	public const uint ArrayLayerCountUndefined = uint.MaxValue;
	public const uint CopyStrideUndefined = uint.MaxValue;
	public const float DepthClearValueUndefined = float.NaN;
	public const uint DepthSliceUndefined = uint.MaxValue;
	public const uint LimitU32Undefined = uint.MaxValue;
	public const ulong LimitU64Undefined = ulong.MaxValue;
	public const uint MipLevelCountUndefined = uint.MaxValue;
	public const uint QuerySetIndexUndefined = uint.MaxValue;
	public static readonly UIntPtr Strlen = UIntPtr.MaxValue;
	public static readonly UIntPtr WholeMapSize = UIntPtr.MaxValue;
	public const ulong WholeSize = ulong.MaxValue;
}
