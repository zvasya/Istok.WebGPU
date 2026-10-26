using System.Reflection;
using Examples;
using Istok.WebGPU;
using Istok.WebGPU.LowLevel;

namespace GenerateBrdfLutStandalone;

internal static class Program
{
    private static async Task Main()
    {
        // MSBuild embeds the source Assets directory, so Rider, dotnet run and direct
        // execution all read the same shader and write the same source texture asset.
        string assetsDirectory = Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "ExamplesAssetsDirectory").Value!;
        string outputPath = Path.Combine(assetsDirectory, "Textures", "brdfLUT.ktx");

        using var instance = GPU.Create();
        var requestedAdapter = await instance.RequestAdapter(new WGPURequestAdapterOptions());
        if (requestedAdapter.Handle.Handle == IntPtr.Zero)
            throw new InvalidOperationException("No WebGPU adapter is available for BRDF LUT generation.");
        using var adapter = requestedAdapter;
        var requestedDevice = await adapter.RequestDevice(new GPUDeviceDescriptor());
        if (requestedDevice.Handle.Handle == IntPtr.Zero)
            throw new InvalidOperationException("Unable to create a WebGPU device for BRDF LUT generation.");
        using var device = requestedDevice;

        Console.WriteLine($"Calculating {ExampleBrdfLut.Width}x{ExampleBrdfLut.Height} BRDF LUT ({ExampleBrdfLut.NumSamples} samples) on {adapter.Info.Device}...");
        byte[] ktx = await ExampleBrdfLut.GenerateAsync(device, new ResourcesProvider(assetsDirectory));
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllBytesAsync(outputPath, ktx);
        Console.WriteLine($"Saved BRDF LUT: {outputPath} ({ktx.Length} bytes).");
    }
}
