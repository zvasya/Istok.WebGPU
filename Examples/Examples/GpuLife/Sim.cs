using System.Runtime.InteropServices;

namespace Examples.GpuLife;

[StructLayout(LayoutKind.Sequential)]
public struct Sim {
	public float Colours;
	public float Beta;
	public float RMax;
	public float Force;
	public float Friction;
	public float Dt;
	public float CellSize;
	public float CellAmt;
	public float Avoidance;
	public float WorldSize;
	public float Border;
	public float Vortex;
}