using System.Buffers.Binary;
using System.Numerics;
using Istok.WebGPU;
using Istok.WebGPU.LowLevel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Examples.Utils;

internal sealed class SampledTexture(GPUTexture texture, GPUTextureView view, GPUSampler sampler) : IDisposable
{
    public GPUTexture Texture { get; } = texture;
    public GPUTextureView View { get; } = view;
    public GPUSampler Sampler { get; } = sampler;

    public static SampledTexture FromImage(GPUDevice device, GltfModel.TextureData source, bool srgb, Rgba32 fallback, MipmapGenerator mipmaps)
    {
        using var image = source.Image == null ? new Image<Rgba32>(1, 1, fallback) : Image.Load<Rgba32>(source.Image);
        uint levels = (uint)BitOperations.Log2((uint)Math.Max(image.Width, image.Height)) + 1;
        var texture = device.CreateTexture(new GPUTextureDescriptor
        {
            Format = srgb ? WGPUTextureFormat.RGBA8UnormSrgb : WGPUTextureFormat.RGBA8Unorm,
            Size = new WGPUExtent3D { width = (uint)image.Width, height = (uint)image.Height, depthOrArrayLayers = 1 },
            MipLevelCount = levels,
            Usage = WGPUTextureUsage.TextureBinding | WGPUTextureUsage.CopyDst | WGPUTextureUsage.RenderAttachment,
        });
        try
        {
            var pixels = new byte[image.Width * image.Height * 4];
            image.CopyPixelDataTo(pixels);
            Upload(device, texture, pixels, (uint)image.Width, (uint)image.Height, 0, 0, 4);
            mipmaps.Generate(texture);
            int minFilter = (int?)(source.Sampler?.MinFilter) ?? 9987;
            int magFilter = (int?)(source.Sampler?.MagFilter) ?? 9729;
            return Finish(device, texture, 1, new GPUSamplerDescriptor
            {
                AddressModeU = AddressMode(source.Sampler == null ? 10497 : (int)source.Sampler.WrapS),
                AddressModeV = AddressMode(source.Sampler == null ? 10497 : (int)source.Sampler.WrapT),
                MagFilter = magFilter == 9728 ? WGPUFilterMode.Nearest : WGPUFilterMode.Linear,
                MinFilter = minFilter is 9728 or 9984 or 9986 ? WGPUFilterMode.Nearest : WGPUFilterMode.Linear,
                MipmapFilter = minFilter is 9986 or 9987 ? WGPUMipmapFilterMode.Linear : WGPUMipmapFilterMode.Nearest,
                LodMaxClamp = minFilter is 9728 or 9729 ? 0 : levels - 1,
            });
        }
        catch { texture.Dispose(); throw; }
    }

    public static SampledTexture FromKtx(GPUDevice device, byte[] data, uint expectedFaces)
    {
        ReadOnlySpan<byte> identifier = [0xab, 0x4b, 0x54, 0x58, 0x20, 0x31, 0x31, 0xbb, 0x0d, 0x0a, 0x1a, 0x0a];
        if (data.Length < 64 || !data.AsSpan(0, 12).SequenceEqual(identifier))
            throw new InvalidDataException("Expected a KTX 1 texture.");
        uint Header(int index) => BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(12 + index * 4));
        uint type = Header(1), typeSize = Header(2), internalFormat = Header(4);
        bool fp32 = type == 0x1406 && typeSize == 4 && internalFormat == 0x8814;
        bool fp16 = type == 0x140b && typeSize == 2 && internalFormat == 0x881a;
        uint width = Header(6), height = Header(7), faces = Header(10), levels = Header(11);
        if (Header(0) != 0x04030201 || Header(3) != 0x1908 || Header(5) != 0x1908 || (!fp32 && !fp16) ||
            width == 0 || height == 0 || Header(8) != 0 || Header(9) != 0 || faces != expectedFaces ||
            (faces != 1 && faces != 6) || (faces == 6 && width != height) ||
            levels == 0 || levels > BitOperations.Log2(Math.Max(width, height)) + 1)
            throw new NotSupportedException("Expected a little-endian RGBA16F/RGBA32F 2D or cube KTX texture.");
        // RGBA16F is filterable in core WebGPU. Preserve full precision when the
        // device enabled Float32Filterable; otherwise convert without clipping HDR.
        bool keep32 = fp32 && device.Features.Contains(WGPUFeatureName.Float32Filterable);
        var texture = device.CreateTexture(new GPUTextureDescriptor
        {
            Format = keep32 ? WGPUTextureFormat.RGBA32Float : WGPUTextureFormat.RGBA16Float,
            Size = new WGPUExtent3D { width = width, height = height, depthOrArrayLayers = faces },
            MipLevelCount = levels,
            Usage = WGPUTextureUsage.TextureBinding | WGPUTextureUsage.CopyDst,
        });
        try
        {
            int offset = checked(64 + (int)Header(12));
            for (uint mip = 0; mip < levels; mip++)
            {
                uint w = Math.Max(1, width >> (int)mip), h = Math.Max(1, height >> (int)mip);
                int imageSize = checked((int)(w * h * 4 * typeSize));
                if (BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset)) != imageSize)
                    throw new InvalidDataException("KTX face size does not match its dimensions.");
                offset += 4;
                for (uint face = 0; face < faces; face++)
                {
                    var pixels = data.AsSpan(offset, imageSize);
                    if (fp32 && !keep32)
                    {
                        var converted = new byte[imageSize / 2];
                        for (int i = 0; i < converted.Length / 2; i++)
                        {
                            float value = BinaryPrimitives.ReadSingleLittleEndian(pixels[(i * 4)..]);
                            Half half = (Half)value;
                            if (!Half.IsFinite(half))
                                throw new InvalidDataException("KTX value exceeds the portable RGBA16F range; enable Float32Filterable for this asset.");
                            BinaryPrimitives.WriteUInt16LittleEndian(converted.AsSpan(i * 2), BitConverter.HalfToUInt16Bits(half));
                        }
                        Upload(device, texture, converted, w, h, face, mip, 8);
                    }
                    else
                        Upload(device, texture, pixels, w, h, face, mip, keep32 ? 16u : 8u);
                    offset = checked(offset + ((imageSize + 3) & ~3));
                }
            }
            if (offset != data.Length)
                throw new InvalidDataException("Unexpected trailing KTX data.");
            return Finish(device, texture, faces, new GPUSamplerDescriptor
            {
                MinFilter = WGPUFilterMode.Linear,
                MagFilter = WGPUFilterMode.Linear,
                MipmapFilter = WGPUMipmapFilterMode.Linear,
                LodMaxClamp = levels - 1,
            });
        }
        catch { texture.Dispose(); throw; }
    }

    private static SampledTexture Finish(GPUDevice device, GPUTexture texture, uint faces, GPUSamplerDescriptor sampler)
    {
        var view = texture.CreateView(new GPUTextureViewDescriptor
        {
            Dimension = faces == 6 ? WGPUTextureViewDimension.Cube : WGPUTextureViewDimension.D2D,
            Usage = WGPUTextureUsage.TextureBinding,
        });
        try { return new SampledTexture(texture, view, device.CreateSampler(sampler)); }
        catch { view.Dispose(); throw; }
    }

    private static WGPUAddressMode AddressMode(int mode) => mode switch
    {
        33071 => WGPUAddressMode.ClampToEdge,
        33648 => WGPUAddressMode.MirrorRepeat,
        _ => WGPUAddressMode.Repeat,
    };

    private static unsafe void Upload(GPUDevice device, GPUTexture texture, ReadOnlySpan<byte> pixels,
        uint width, uint height, uint face, uint mip, uint bytesPerPixel)
    {
        fixed (byte* pointer = pixels)
            device.Queue.WriteTexture(
                new WGPUTexelCopyTextureInfo { texture = texture, mipLevel = mip, origin = new WGPUOrigin3D { z = face }, aspect = WGPUTextureAspect.All },
                pointer, (nuint)pixels.Length,
                new WGPUTexelCopyBufferLayout { bytesPerRow = width * bytesPerPixel, rowsPerImage = height },
                new WGPUExtent3D { width = width, height = height, depthOrArrayLayers = 1 });
    }

    public void Dispose()
    {
        Sampler.Dispose();
        View.Dispose();
        Texture.Dispose();
    }
}
