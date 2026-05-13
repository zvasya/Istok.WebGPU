using System.Runtime.InteropServices;

namespace Examples.GpuLife;

[StructLayout(LayoutKind.Explicit, Size = 16*4)]
public struct Uniforms
{
	[FieldOffset(0)] public float aspect;
	[FieldOffset(4*4)] public Input.Mouse mouse;
	[FieldOffset(8*4)] public float size;
}