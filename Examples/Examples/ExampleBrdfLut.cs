using AssetManager;
using Istok.WebGPU;
using Istok.WebGPU.LowLevel;
using Istok.WebGPU.Requests;

namespace Examples;

// Offline compute example, so it needs no window or surface.
public static class ExampleBrdfLut
{
    public const uint Width = 256;
    public const uint Height = 256;
    public const uint NumSamples = 1024;

    /// <summary>
    /// Generates a KTX 1 texture in RGBA16F: GGX scale/bias in RG, Charlie sheen in B,
    /// and an opaque alpha. X is NdotV; roughness decreases with increasing row index.
    /// </summary>
    public static async Task<byte[]> GenerateAsync(GPUDevice device, IResourcesProvider resources,
        uint width = Width, uint height = Height, uint numSamples = NumSamples)
    {
        ArgumentOutOfRangeException.ThrowIfZero(width);
        ArgumentOutOfRangeException.ThrowIfZero(height);
        ArgumentOutOfRangeException.ThrowIfZero(numSamples);
        int byteCount = checked((int)((ulong)width * height * 8));
        var limits = device.Limits;
        if ((ulong)byteCount > limits.maxStorageBufferBindingSize || (ulong)byteCount > limits.maxBufferSize ||
            (width + 15UL) / 16 > limits.maxComputeWorkgroupsPerDimension ||
            (height + 15UL) / 16 > limits.maxComputeWorkgroupsPerDimension)
            throw new ArgumentOutOfRangeException(nameof(width), "LUT dimensions exceed the device limits.");

        string code = await resources.LoadTextAsync("Shaders/example_brdf_lut.wgsl");
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("BRDF LUT shader source must not be empty.");
        using var shader = device.CreateShaderModule(new GPUShaderModuleDescriptor { Code = code });
        using var pipeline = CreatePipeline(device, shader, numSamples);
        using var parameters = device.CreateBuffer(new GPUBufferDescriptor
        {
            Label = "BRDF LUT: dimensions",
            Size = 16,
            Usage = WGPUBufferUsage.Uniform,
            MappedAtCreation = true,
        });
        parameters.GetMappedRange<uint>(0, 4)[0] = width;
        parameters.GetMappedRange<uint>(0, 4)[1] = height;
        parameters.Unmap();

        using var output = device.CreateBuffer(new GPUBufferDescriptor
        {
            Label = "BRDF LUT: packed RGBA16F",
            Size = (ulong)byteCount,
            Usage = WGPUBufferUsage.Storage | WGPUBufferUsage.CopySrc,
        });
        using var readback = device.CreateBuffer(new GPUBufferDescriptor
        {
            Label = "BRDF LUT: readback",
            Size = (ulong)byteCount,
            Usage = WGPUBufferUsage.MapRead | WGPUBufferUsage.CopyDst,
        });
        using var bindGroup = CreateBindGroup(device, pipeline, parameters, output);
        using var encoder = device.CreateCommandEncoder();
        using (var pass = encoder.BeginComputePass(new GPUComputePassDescriptor()))
        {
            pass.SetPipeline(pipeline);
            pass.SetBindGroup(0, bindGroup);
            pass.DispatchWorkgroups((width + 15) / 16, (height + 15) / 16, 1);
            pass.End();
        }
        encoder.CopyBufferToBuffer(output, readback);
        using var commands = encoder.Finish();
        device.Queue.Submit(commands);

        // Mapping waits for the preceding GPU copy; check its status before accessing memory.
        var status = await BufferMap.Request(readback, WGPUMapMode.Read, 0, (nuint)byteCount);
        if (status != WGPUMapAsyncStatus.Success)
            throw new InvalidOperationException($"BRDF LUT readback failed: {status}.");
        try
        {
            return CreateKtx(readback.GetConstMappedRange(), width, height);
        }
        finally
        {
            readback.Unmap();
        }
    }

    private static unsafe GPUComputePipeline CreatePipeline(GPUDevice device, GPUShaderModule shader, uint numSamples)
    {
        // Numeric WGSL override ID 0 corresponds to the original Vulkan specialization constant.
        using ("0".ToWGPUStringView(out var key))
        {
            return device.CreateComputePipeline(new GPUComputePipelineDescriptor
            {
                Label = "Compute BRDF LUT",
                Layout = GPUPipelineLayout.Auto,
                Compute = new GPUComputeState
                {
                    Module = shader,
                    EntryPoint = "cs_main",
                    Constants = [new WGPUConstantEntry { key = key, value = numSamples }],
                },
            });
        }
    }

    private static unsafe GPUBindGroup CreateBindGroup(GPUDevice device, GPUComputePipeline pipeline,
        GPUBuffer parameters, GPUBuffer output)
    {
        using var layout = pipeline.GetBindGroupLayout(0);
        return device.CreateBindGroup(new GPUBindGroupDescriptor
        {
            Layout = layout,
            Entries =
            [
                new WGPUBindGroupEntry { binding = 0, buffer = parameters, size = parameters.Size },
                new WGPUBindGroupEntry { binding = 1, buffer = output, size = output.Size },
            ],
        });
    }

    private static byte[] CreateKtx(ReadOnlySpan<byte> pixels, uint width, uint height)
    {
        // KTX 1.0, little-endian, one 2D image and one mip level. Rows are multiples of 8 bytes.
        // https://registry.khronos.org/KTX/specs/1.0/ktxspec.v1.html
        using var stream = new MemoryStream(checked(68 + pixels.Length));
        using var writer = new BinaryWriter(stream);
        writer.Write(new byte[] { 0xab, 0x4b, 0x54, 0x58, 0x20, 0x31, 0x31, 0xbb, 0x0d, 0x0a, 0x1a, 0x0a });
        writer.Write(0x04030201u); // endianness
        writer.Write(0x140bu);     // GL_HALF_FLOAT
        writer.Write(2u);          // glTypeSize
        writer.Write(0x1908u);     // GL_RGBA
        writer.Write(0x881au);     // GL_RGBA16F
        writer.Write(0x1908u);     // glBaseInternalFormat: GL_RGBA
        writer.Write(width);
        writer.Write(height);
        writer.Write(0u);          // pixelDepth: 2D
        writer.Write(0u);          // numberOfArrayElements: not an array
        writer.Write(1u);          // numberOfFaces
        writer.Write(1u);          // numberOfMipmapLevels
        writer.Write(0u);          // bytesOfKeyValueData
        writer.Write((uint)pixels.Length);
        writer.Write(pixels);
        return stream.ToArray();
    }
}
