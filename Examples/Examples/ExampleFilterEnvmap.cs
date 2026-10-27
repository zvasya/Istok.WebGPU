using System.Numerics;
using System.Runtime.InteropServices;
using AssetManager;
using Examples.Utils;
using Istok.WebGPU;
using Istok.WebGPU.LowLevel;
using Istok.WebGPU.Requests;

namespace Examples;

// Chapter06/03_FilterEnvmap: offline environment-map filtering, with the same
// importance-sampling equations executed by compute instead of fullscreen draws.
public static class ExampleFilterEnvmap
{
    public sealed record Result(byte[] Prefiltered, byte[] Charlie, byte[] Irradiance);

    [StructLayout(LayoutKind.Sequential)]
    private struct Parameters
    {
        public uint FaceSize;
        public uint SourceWidth;
        public uint SourceHeight;
        public uint SampleCount;
        public float Roughness;
        public uint Distribution;
        public uint OutputOffset;
        public uint Padding;
    }

    public static async Task<Result> GenerateAsync(GPUDevice device, IResourcesProvider resources,
        ReadOnlyMemory<Vector4> panorama, int panoramaWidth, int panoramaHeight,
        uint specularSamples = 1024, uint irradianceSamples = 2048,
        int irradianceSize = 64, Action<string>? progress = null)
    {
        ArgumentOutOfRangeException.ThrowIfZero(specularSamples);
        ArgumentOutOfRangeException.ThrowIfZero(irradianceSamples);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(irradianceSize);
        if (panoramaWidth < 4 || panoramaWidth != panoramaHeight * 2 || panoramaWidth % 4 != 0 ||
            panorama.Length != checked(panoramaWidth * panoramaHeight))
            throw new ArgumentException("Expected a 2:1 equirectangular panorama with width divisible by four.", nameof(panorama));
        if (!device.Features.Contains(WGPUFeatureName.Float32Filterable))
            throw new NotSupportedException("Environment filtering requires the Float32Filterable device feature for RGBA32F cubemap sampling.");
        int faceSize = panoramaWidth / 4;
        if ((uint)faceSize > device.Limits.maxTextureDimension2D)
            throw new ArgumentOutOfRangeException(nameof(panorama), "Cubemap exceeds the device texture limits.");

        string code = await resources.LoadTextAsync("Shaders/example_filter_envmap.wgsl");
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Environment filtering shader source must not be empty.");
        using var shader = device.CreateShaderModule(new GPUShaderModuleDescriptor { Code = code });
        using var pipeline = device.CreateComputePipeline(new GPUComputePipelineDescriptor
        {
            Label = "Filter environment cubemap",
            Layout = GPUPipelineLayout.Auto,
            Compute = new GPUComputeState { Module = shader, EntryPoint = "cs_main" },
        });
        var cubeFaces = UtilsHdrCubemap.ConvertEquirectangularToCubeFaces(panorama.Span, panoramaWidth, panoramaHeight);
        using var environment = UploadEnvironment(device, cubeFaces, faceSize);
        using var environmentView = environment.CreateView(new GPUTextureViewDescriptor
        {
            Dimension = WGPUTextureViewDimension.Cube,
            Usage = WGPUTextureUsage.TextureBinding,
        });
        using var sampler = device.CreateSampler(new GPUSamplerDescriptor
        {
            AddressModeU = WGPUAddressMode.ClampToEdge,
            AddressModeV = WGPUAddressMode.ClampToEdge,
            AddressModeW = WGPUAddressMode.ClampToEdge,
            MinFilter = WGPUFilterMode.Linear,
            MagFilter = WGPUFilterMode.Linear,
            MipmapFilter = WGPUMipmapFilterMode.Linear,
        });

        progress?.Invoke("Prefiltering GGX...");
        byte[] prefiltered = await FilterAsync(device, pipeline, environmentView, sampler, faceSize, faceSize,
            distribution: 1, samples: specularSamples, mipmaps: true);
        progress?.Invoke("Prefiltering Charlie...");
        byte[] charlie = await FilterAsync(device, pipeline, environmentView, sampler, faceSize, faceSize,
            distribution: 2, samples: specularSamples, mipmaps: true);
        progress?.Invoke("Prefiltering Lambertian...");
        byte[] irradiance = await FilterAsync(device, pipeline, environmentView, sampler, faceSize, irradianceSize,
            distribution: 0, samples: irradianceSamples, mipmaps: false);
        return new Result(prefiltered, charlie, irradiance);
    }

    private static int[] MipSizes(int size)
    {
        int levels = BitOperations.Log2((uint)size) + 1;
        return Enumerable.Range(0, levels).Select(level => Math.Max(1, size >> level)).ToArray();
    }

    private static GPUTexture UploadEnvironment(GPUDevice device, Vector4[] cubeFaces, int size)
    {
        int[] sizes = MipSizes(size);
        var texture = device.CreateTexture(new GPUTextureDescriptor
        {
            Label = "Source HDR cubemap",
            Format = WGPUTextureFormat.RGBA32Float,
            Dimension = WGPUTextureDimension.D2D,
            Size = new WGPUExtent3D { width = (uint)size, height = (uint)size, depthOrArrayLayers = 6 },
            MipLevelCount = (uint)sizes.Length,
            Usage = WGPUTextureUsage.TextureBinding | WGPUTextureUsage.CopyDst,
        });
        try
        {
            // Resize each face separately to avoid bleeding between stacked faces.
            for (int face = 0; face < 6; face++)
            {
                var pixels = cubeFaces.AsSpan(face * size * size, size * size).ToArray();
                for (int mip = 0; mip < sizes.Length; mip++)
                {
                    if (mip > 0)
                        pixels = UtilsHdrCubemap.DownsampleFace(pixels, sizes[mip - 1]);
                    UploadFace(device, texture, pixels, (uint)sizes[mip], (uint)face, (uint)mip);
                }
            }
            return texture;
        }
        catch
        {
            texture.Dispose();
            throw;
        }
    }

    private static unsafe void UploadFace(GPUDevice device, GPUTexture texture, Vector4[] pixels, uint size, uint face, uint mip)
    {
        fixed (Vector4* pointer = pixels)
        {
            device.Queue.WriteTexture(
                new WGPUTexelCopyTextureInfo
                {
                    texture = texture,
                    mipLevel = mip,
                    origin = new WGPUOrigin3D { z = face },
                    aspect = WGPUTextureAspect.All,
                }, pointer, (nuint)(pixels.Length * sizeof(Vector4)),
                new WGPUTexelCopyBufferLayout { bytesPerRow = size * 16, rowsPerImage = size },
                new WGPUExtent3D { width = size, height = size, depthOrArrayLayers = 1 });
        }
    }

    private static async Task<byte[]> FilterAsync(GPUDevice device, GPUComputePipeline pipeline,
        GPUTextureView environment, GPUSampler sampler, int sourceSize, int outputSize,
        uint distribution, uint samples, bool mipmaps)
    {
        int[] sizes = mipmaps ? MipSizes(outputSize) : [outputSize];
        int pixelCount = checked(sizes.Sum(size => checked(size * size * 6)));
        int byteCount = checked(pixelCount * 16);
        var limits = device.Limits;
        if ((ulong)byteCount > limits.maxStorageBufferBindingSize || (ulong)byteCount > limits.maxBufferSize ||
            ((ulong)outputSize + 7) / 8 > limits.maxComputeWorkgroupsPerDimension)
            throw new ArgumentOutOfRangeException(nameof(outputSize), "Filtered cubemap exceeds the device buffer or dispatch limits.");
        using var output = device.CreateBuffer(new GPUBufferDescriptor
        {
            Label = "Filtered cubemap RGBA32F",
            Size = (ulong)byteCount,
            Usage = WGPUBufferUsage.Storage | WGPUBufferUsage.CopySrc,
        });
        using var readback = device.CreateBuffer(new GPUBufferDescriptor
        {
            Label = "Filtered cubemap readback",
            Size = (ulong)byteCount,
            Usage = WGPUBufferUsage.MapRead | WGPUBufferUsage.CopyDst,
        });
        using var layout = pipeline.GetBindGroupLayout(0);
        using var encoder = device.CreateCommandEncoder();
        // Every dispatch needs its own immutable uniform buffer: queue writes to one
        // shared buffer would make all dispatches see the last mip's parameters.
        var uniforms = new List<GPUBuffer>();
        var groups = new List<GPUBindGroup>();
        try
        {
            uint offset = 0;
            for (int mip = 0; mip < sizes.Length; mip++)
            {
                var parameters = new Parameters
                {
                    FaceSize = (uint)sizes[mip],
                    SourceWidth = (uint)sourceSize,
                    SourceHeight = (uint)sourceSize,
                    SampleCount = samples,
                    Roughness = sizes.Length > 1 ? (float)mip / (sizes.Length - 1) : 0,
                    Distribution = distribution,
                    OutputOffset = offset,
                };
                var uniform = CreateUniform(device, parameters);
                uniforms.Add(uniform);
                var group = CreateBindGroup(device, layout, uniform, environment, sampler, output);
                groups.Add(group);
                using var pass = encoder.BeginComputePass();
                pass.SetPipeline(pipeline);
                pass.SetBindGroup(0, group);
                pass.DispatchWorkgroups(((uint)sizes[mip] + 7) / 8, ((uint)sizes[mip] + 7) / 8, 6);
                pass.End();
                offset += (uint)(sizes[mip] * sizes[mip] * 6);
            }
            encoder.CopyBufferToBuffer(output, readback);
            using var commands = encoder.Finish();
            device.Queue.Submit(commands);
            var status = await BufferMap.Request(readback, WGPUMapMode.Read, 0, (nuint)byteCount);
            if (status != WGPUMapAsyncStatus.Success)
                throw new InvalidOperationException($"Filtered cubemap readback failed: {status}.");
            try
            {
                return CreateKtx(readback.GetConstMappedRange(), sizes);
            }
            finally
            {
                readback.Unmap();
            }
        }
        finally
        {
            foreach (var group in groups) group.Dispose();
            foreach (var uniform in uniforms) uniform.Dispose();
        }
    }

    private static unsafe GPUBuffer CreateUniform(GPUDevice device, Parameters parameters)
    {
        var buffer = device.CreateBuffer(new GPUBufferDescriptor
        {
            Label = "Environment filter parameters",
            Size = (ulong)sizeof(Parameters),
            Usage = WGPUBufferUsage.Uniform,
            MappedAtCreation = true,
        });
        buffer.GetMappedRange<Parameters>()[0] = parameters;
        buffer.Unmap();
        return buffer;
    }

    private static unsafe GPUBindGroup CreateBindGroup(GPUDevice device, GPUBindGroupLayout layout,
        GPUBuffer parameters, GPUTextureView environment, GPUSampler sampler, GPUBuffer output) =>
        device.CreateBindGroup(new GPUBindGroupDescriptor
        {
            Layout = layout,
            Entries =
            [
                new WGPUBindGroupEntry { binding = 0, buffer = parameters, size = parameters.Size },
                new WGPUBindGroupEntry { binding = 1, textureView = environment },
                new WGPUBindGroupEntry { binding = 2, sampler = sampler },
                new WGPUBindGroupEntry { binding = 3, buffer = output, size = output.Size },
            ],
        });

    private static byte[] CreateKtx(ReadOnlySpan<byte> pixels, int[] sizes)
    {
        using var stream = new MemoryStream(checked(64 + sizes.Length * 4 + pixels.Length));
        using var writer = new BinaryWriter(stream);
        writer.Write(new byte[] { 0xab, 0x4b, 0x54, 0x58, 0x20, 0x31, 0x31, 0xbb, 0x0d, 0x0a, 0x1a, 0x0a });
        writer.Write(0x04030201u);
        writer.Write(0x1406u); // GL_FLOAT
        writer.Write(4u);
        writer.Write(0x1908u); // GL_RGBA
        writer.Write(0x8814u); // GL_RGBA32F
        writer.Write(0x1908u);
        writer.Write((uint)sizes[0]);
        writer.Write((uint)sizes[0]);
        writer.Write(0u); // pixelDepth
        writer.Write(0u); // numberOfArrayElements
        writer.Write(6u); // numberOfFaces
        writer.Write((uint)sizes.Length);
        writer.Write(0u); // bytesOfKeyValueData
        int offset = 0;
        foreach (int size in sizes)
        {
            int faceBytes = checked(size * size * 16);
            writer.Write((uint)faceBytes); // KTX 1 imageSize is the size of ONE cubemap face.
            writer.Write(pixels.Slice(offset, faceBytes * 6));
            offset += faceBytes * 6;
        }
        return stream.ToArray();
    }
}
