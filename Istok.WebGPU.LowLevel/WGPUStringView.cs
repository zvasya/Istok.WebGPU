using System.Runtime.InteropServices;

namespace Istok.WebGPU.LowLevel;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUStringView
{
	public byte* data;
	public UIntPtr length;
}