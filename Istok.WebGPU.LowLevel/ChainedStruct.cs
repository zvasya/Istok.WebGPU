using System.Runtime.InteropServices;

namespace Istok.WebGPU.LowLevel;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ChainedStruct
{
	public ChainedStruct* next;
	public WGPUSType sType;
}