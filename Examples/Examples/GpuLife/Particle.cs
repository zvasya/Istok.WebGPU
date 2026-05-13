using System.Numerics;
using System.Runtime.InteropServices;

namespace Examples.GpuLife;

[StructLayout(LayoutKind.Sequential)]
public struct Particle
{
	public Vector2 pos;
	public Vector2 vel;
	public float colour;
	private float notUsed; //We need this field for proper alignment in the Storage buffer. Because the structure's size in the buffer must be a multiple of its maximum alignment (vec2f for pos and vel).
}