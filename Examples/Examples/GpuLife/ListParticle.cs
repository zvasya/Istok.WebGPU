using System.Numerics;
using System.Runtime.InteropServices;

namespace Examples.GpuLife;

[StructLayout(LayoutKind.Explicit, Size = 32)]
public struct ListParticle
{
	[FieldOffset(0)] public float idx;
	[FieldOffset(8)] public Vector2 pos;
	[FieldOffset(16)] public Vector2 vel;
	[FieldOffset(24)] public float colour;
	[FieldOffset(28)] public uint next;
}