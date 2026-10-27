using System.Reflection;
using System.Numerics;
using Examples;
using Istok.WebGPU;
using Istok.WebGPU.LowLevel;
using StbImageSharp;

namespace FilterEnvmapStandalone;

internal static class Program
{
    private static async Task Main()
    {
        string assetsDirectory = Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "ExamplesAssetsDirectory").Value!;
        var resources = new ResourcesProvider(assetsDirectory);
        string texturesDirectory = Path.Combine(assetsDirectory, "Textures");

        using var instance = GPU.Create();
        var requestedAdapter = await instance.RequestAdapter(new WGPURequestAdapterOptions());
        if (requestedAdapter.Handle.Handle == IntPtr.Zero)
            throw new InvalidOperationException("No WebGPU adapter is available for environment filtering.");
        using var adapter = requestedAdapter;
        if (!adapter.Features.Contains(WGPUFeatureName.Float32Filterable))
            throw new NotSupportedException("Environment filtering requires Float32Filterable support for the source RGBA32F cubemap.");
        var requestedDevice = await adapter.RequestDevice(new GPUDeviceDescriptor
        {
            RequiredFeatures = [WGPUFeatureName.Float32Filterable],
        });
        if (requestedDevice.Handle.Handle == IntPtr.Zero)
            throw new InvalidOperationException("Unable to create a WebGPU device for environment filtering.");
        using var device = requestedDevice;

        foreach (string name in new[] { "piazza_bologni_1k", "immenstadter_horn_2k" })
        {
            Console.WriteLine($"Loading {name}.hdr on {adapter.Info.Device}...");
            var hdr = ImageResultFloat.FromMemory(await resources.LoadBytesAsync($"Textures/{name}.hdr"), ColorComponents.RedGreenBlueAlpha);
            var panorama = new Vector4[hdr.Width * hdr.Height];
            for (int i = 0; i < panorama.Length; i++)
                panorama[i] = new Vector4(hdr.Data[4 * i], hdr.Data[4 * i + 1], hdr.Data[4 * i + 2], 1);
            Console.WriteLine($"Panorama: {hdr.Width}x{hdr.Height}; cubemap faces: {hdr.Width / 4}x{hdr.Width / 4}.");
            var result = await ExampleFilterEnvmap.GenerateAsync(device, resources, panorama, hdr.Width, hdr.Height, progress: Console.WriteLine);
            Directory.CreateDirectory(texturesDirectory);
            await SaveAsync($"{name}_prefilter.ktx", result.Prefiltered);
            await SaveAsync($"{name}_charlie.ktx", result.Charlie);
            await SaveAsync($"{name}_irradiance.ktx", result.Irradiance);
        }

        async Task SaveAsync(string fileName, byte[] data)
        {
            string path = Path.Combine(texturesDirectory, fileName);
            await File.WriteAllBytesAsync(path, data);
            Console.WriteLine($"Saved {path} ({data.Length} bytes).");
        }
    }
}
