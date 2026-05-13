using System.Numerics;
using Istok.WebGPU;
using Istok.WebGPU.LowLevel;

namespace Examples.GpuLife;

public static class Utils
{
	public static Vector3 HslToRgb(float h, float s, float l)
	{
		h = ((h % 360f) + 360f) % 360f;
		s = Math.Clamp(s, 0f, 1f);
		l = Math.Clamp(l, 0f, 1f);

		float c = (1f - MathF.Abs(2f * l - 1f)) * s;
		float x = c * (1f - MathF.Abs((h / 60f) % 2f - 1f));
		float m = l - c / 2f;

		Vector3 color = h switch
		{
			< 60 => new Vector3(c, x, 0f),
			< 120 => new Vector3(x, c, 0f),
			< 180 => new Vector3(0f, c, x),
			< 240 => new Vector3(0f, x, c),
			< 300 => new Vector3(x, 0f, c),
			_ => new Vector3(c, 0f, x)
		};

		return color + new Vector3(m);
	}

	public static async Task LogBufferF32(GPUDevice device, GPUBuffer buffer, ulong size)
	{
		GPUBuffer readbackBuffer = device.CreateBuffer(new GPUBufferDescriptor
		{
			Size = size,
			Usage = WGPUBufferUsage.CopyDst | WGPUBufferUsage.MapRead,
		});

		GPUCommandEncoder commandEncoder = device.CreateCommandEncoder();
		commandEncoder.CopyBufferToBuffer(buffer, 0, readbackBuffer, 0, size);

		device.Queue.Submit(commandEncoder.Finish());

		await readbackBuffer.MapAsync(WGPUMapMode.Read);

		Span<float> outputData = readbackBuffer.GetMappedRange<float>(0, (int)(size / sizeof(float)));

		Console.WriteLine($"[{string.Join(", ", outputData.ToArray())}]");

		readbackBuffer.Unmap();
		readbackBuffer.Destroy();
	}

	public static async Task LogBufferU32(GPUDevice device, GPUBuffer buffer, ulong size)
	{
		GPUBuffer readbackBuffer = device.CreateBuffer(new GPUBufferDescriptor
		{
			Size = size,
			Usage = WGPUBufferUsage.CopyDst | WGPUBufferUsage.MapRead,
		});

		GPUCommandEncoder commandEncoder = device.CreateCommandEncoder();
		commandEncoder.CopyBufferToBuffer(buffer, 0, readbackBuffer, 0, size);

		device.Queue.Submit(commandEncoder.Finish());

		await readbackBuffer.MapAsync(WGPUMapMode.Read);

		Span<uint> outputData = readbackBuffer.GetMappedRange<uint>(0, (int)(size / sizeof(uint)));

		Console.WriteLine($"[{string.Join(", ", outputData.ToArray())}]");

		readbackBuffer.Unmap();
		readbackBuffer.Destroy();
	}

	private sealed class TimestampEntry
	{
		public GPUQuerySet QuerySet = null!;
		public GPUBuffer ResolveBuffer = null!;
		public GPUBuffer ResultBuffer = null!;
		public WGPUPassTimestampWrites Writes;
		public double V;
	}

	private static bool _canTimestamp;
	private static readonly Dictionary<string, TimestampEntry> _timestamps = new();

	public static bool NoTimestamp { get; set; } = false;

	public static Task<GPUDevice> RequestTimestamps(GPUAdapter adapter)
	{
		_canTimestamp = Array.IndexOf(adapter.Features, WGPUFeatureName.TimestampQuery) >= 0;

		if (NoTimestamp)
		{
			_canTimestamp = false;
		}

		if (_canTimestamp)
		{
			return RequestDeviceWithTimestamp(adapter);
		}

		return adapter.RequestDevice();
	}

	private static async Task<GPUDevice> RequestDeviceWithTimestamp(GPUAdapter adapter)
	{
		Task<GPUDevice> deviceTask;
		{
			ReadOnlySpan<WGPUFeatureName> requiredFeatures = [WGPUFeatureName.TimestampQuery];
			deviceTask = adapter.RequestDevice(new GPUDeviceDescriptor
			{
				RequiredFeatures = requiredFeatures,
			});
		}

		GPUDevice device = await deviceTask;

		if (Array.IndexOf(device.Features, WGPUFeatureName.TimestampQuery) < 0)
		{
			_canTimestamp = false;
		}

		return device;
	}

	public static void SetupTimestamp(GPUDevice device, string name)
	{
		if (!_canTimestamp) return;

		GPUQuerySet querySet = device.CreateQuerySet(new GPUQuerySetDescriptor
		{
			Type = WGPUQueryType.Timestamp,
			Count = 2,
		});

		_timestamps[name] = new TimestampEntry
		{
			QuerySet = querySet,
			ResolveBuffer = device.CreateBuffer(new GPUBufferDescriptor
			{
				Size = 2 * 8,
				Usage = WGPUBufferUsage.QueryResolve | WGPUBufferUsage.CopySrc,
			}),
			ResultBuffer = device.CreateBuffer(new GPUBufferDescriptor
			{
				Size = 2 * 8,
				Usage = WGPUBufferUsage.CopyDst | WGPUBufferUsage.MapRead,
			}),
			V = 0,
		};
	}

	public static void LinkComputeTimestamp(string name, ref GPUComputePassDescriptor descriptor)
	{
		if (!_canTimestamp) return;

		TimestampEntry entry = _timestamps[name];
		entry.Writes = new WGPUPassTimestampWrites
		{
			querySet = entry.QuerySet.Handle,
			beginningOfPassWriteIndex = 0,
			endOfPassWriteIndex = 1,
		};
		descriptor.TimestampWrites = ref entry.Writes;
	}

	public static void LinkRenderTimestamp(string name, ref GPURenderPassDescriptor descriptor)
	{
		if (!_canTimestamp) return;

		TimestampEntry entry = _timestamps[name];
		entry.Writes = new WGPUPassTimestampWrites
		{
			querySet = entry.QuerySet.Handle,
			beginningOfPassWriteIndex = 0,
			endOfPassWriteIndex = 1,
		};
		descriptor.TimestampWrites = ref entry.Writes;
	}

	public static void ResolveTimestamp(GPUCommandEncoder commandEncoder, string name)
	{
		if (!_canTimestamp) return;

		TimestampEntry entry = _timestamps[name];
		commandEncoder.ResolveQuerySet(
			entry.QuerySet,
			0,
			entry.QuerySet.Count,
			entry.ResolveBuffer,
			0
		);

		if (entry.ResultBuffer.MapState == WGPUBufferMapState.Unmapped)
		{
			commandEncoder.CopyBufferToBuffer(
				entry.ResolveBuffer,
				0,
				entry.ResultBuffer,
				0,
				entry.ResultBuffer.Size
			);
		}
	}

	public static async Task<double> ReadTimestamp(string name)
	{
		if (!_canTimestamp) return 0;

		TimestampEntry entry = _timestamps[name];
		if (entry.ResultBuffer.MapState != WGPUBufferMapState.Unmapped)
		{
			return entry.V;
		}

		await entry.ResultBuffer.MapAsync(WGPUMapMode.Read);

		Span<ulong> times = entry.ResultBuffer.GetConstMappedRange<ulong>(0, 2);

		entry.V = (times[1] - times[0]) / 1_000_000.0;
		entry.ResultBuffer.Unmap();

		return entry.V;
	}

	public static float Lerpn(float start, float end, float multiply, float step)
	{
		multiply = 1f - MathF.Pow(1f - multiply, step);
		if (multiply > 1f) multiply = 1f;
		if (multiply < 0f) multiply = 0f;
		return start + (end - start) * multiply;
	}

	public static float Lerp5(float start, float end, float step)
	{
		return Lerpn(start, end, 0.5f, step);
	}
}
