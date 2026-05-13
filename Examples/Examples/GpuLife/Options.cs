using Istok.WebGPU;

namespace Examples.GpuLife;

public static class Options
{
	public const int ParticleAmt = 50000;
	public const int ColourAmt = 200;
	const int CellAmt = 2000;

	public static class Params
	{
		public static double Fps = 0;
		public static string Engine = "linkedList";
		public static int Particles = ParticleAmt;
		public static int Colours = ColourAmt;
		public static int Cells = CellAmt;
	}

	public static class OptionParams
	{
		public static int Colours = ColourAmt;
		public static float R = 15f;
		public static float Force = 1f;
		public static float Beta = 0.3f;
		public static float Delta = 0.02f;
		public static float Friction = 0.04f;
		public static int Cells = CellAmt;
		public static float Avoidance = 4f;
		public static float WorldSize = 6f;
		public static bool Border = true;
		public static bool Vortex = false;
	}
	
	public static unsafe void SetSim(GPUDevice device, GPUBuffer simBuffer)
	{
		Sim sim = new Sim
		{
			Colours = ColourAmt,
			Beta = OptionParams.Beta,
			RMax = 1f / OptionParams.R,
			Force = OptionParams.Force / (1f / OptionParams.R),
			Friction = MathF.Pow(0.5f, OptionParams.Delta / OptionParams.Friction),
			Dt = OptionParams.Delta,
			CellSize = (1f / OptionParams.R) * 2f,
			CellAmt = Params.Cells,
			Avoidance = OptionParams.Avoidance,
			WorldSize = OptionParams.WorldSize,
			Border = OptionParams.Border ? 1 : 0,
			Vortex = OptionParams.Vortex ? 1 : 0,
		};

		device.Queue.WriteBuffer(simBuffer, 0, &sim, (UIntPtr)sizeof(Sim));
	}
}