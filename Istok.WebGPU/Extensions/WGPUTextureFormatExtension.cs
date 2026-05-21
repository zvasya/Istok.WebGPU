namespace Istok.WebGPU;

using System.Diagnostics;

public static class WGPUTextureFormatExtension
{
    
    public record struct WebGPUFormatInfo(uint Size, uint Channels);
    
    extension(WGPUTextureFormat format)
    {
        public bool IsUndef() => format == WGPUTextureFormat.Undefined;
        public bool HasDepth() => format.IsDepthOnly() || format.IsDepthAndStencil();
        public bool HasStencil() => format.IsStencilOnly() || format.IsDepthAndStencil();
        public bool IsColor() => !format.IsUndef() && !format.IsDepthOrStencil();

        public bool IsCompressedEtc2Eac() => format switch
        {
            WGPUTextureFormat.ETC2RGB8Unorm or WGPUTextureFormat.ETC2RGB8UnormSrgb or
                WGPUTextureFormat.ETC2RGB8A1Unorm or WGPUTextureFormat.ETC2RGB8A1UnormSrgb or
                WGPUTextureFormat.ETC2RGBA8Unorm or WGPUTextureFormat.ETC2RGBA8UnormSrgb or
                WGPUTextureFormat.EACR11Unorm or WGPUTextureFormat.EACR11Snorm or
                WGPUTextureFormat.EACRG11Unorm or WGPUTextureFormat.EACRG11Snorm => true,
            _ => false
        };

        public bool IsCompressedAstc() => format switch
        {
            >= WGPUTextureFormat.ASTC4X4Unorm and <= WGPUTextureFormat.ASTC12X12UnormSrgb => true,
            _ => false
        };

        public bool IsCompressedBc() => format switch
        {
            >= WGPUTextureFormat.BC1RGBAUnorm and <= WGPUTextureFormat.BC7RGBAUnormSrgb => true,
            _ => false
        };

        public bool IsCompressed() =>
            format.IsCompressedAstc() || format.IsCompressedBc() || format.IsCompressedEtc2Eac();

        public bool IsPacked() => format switch
        {
            WGPUTextureFormat.RGB10A2Uint or WGPUTextureFormat.RGB10A2Unorm or
                WGPUTextureFormat.RG11B10Ufloat or WGPUTextureFormat.RGB9E5Ufloat => true,
            _ => false
        };

        /// <summary>
        /// Return true if format is 'normal', with one texel per format element
        /// </summary>
        /// <returns></returns>
        public bool ElementIsTexel() => !(format.IsPacked() || format.IsCompressed());

        public bool IsDepthOrStencil() => format.IsDepthAndStencil() || format.IsDepthOnly() || format.IsStencilOnly();

        public bool IsDepthAndStencil() => format switch
        {
            WGPUTextureFormat.Depth24PlusStencil8 or WGPUTextureFormat.Depth32FloatStencil8 => true,
            _ => false
        };

        public bool IsStencilOnly() => format == WGPUTextureFormat.Stencil8;

        public bool IsDepthOnly() => format switch
        {
            WGPUTextureFormat.Depth16Unorm or WGPUTextureFormat.Depth24Plus or WGPUTextureFormat.Depth32Float => true,
            _ => false
        };

        public bool IsUNorm() => format is 
            WGPUTextureFormat.R8Unorm or
            WGPUTextureFormat.R16Unorm or
            WGPUTextureFormat.RG8Unorm or
            WGPUTextureFormat.RG16Unorm or
            WGPUTextureFormat.RGBA8Unorm or
            WGPUTextureFormat.BGRA8Unorm or
            WGPUTextureFormat.RGB10A2Unorm or
            WGPUTextureFormat.RGBA16Unorm or
            WGPUTextureFormat.Depth16Unorm or
            WGPUTextureFormat.BC1RGBAUnorm or
            WGPUTextureFormat.BC2RGBAUnorm or
            WGPUTextureFormat.BC3RGBAUnorm or
            WGPUTextureFormat.BC4RUnorm or
            WGPUTextureFormat.BC5RGUnorm or
            WGPUTextureFormat.BC7RGBAUnorm or
            WGPUTextureFormat.ETC2RGB8Unorm or
            WGPUTextureFormat.ETC2RGB8A1Unorm or
            WGPUTextureFormat.ETC2RGBA8Unorm or
            WGPUTextureFormat.EACR11Unorm or
            WGPUTextureFormat.EACRG11Unorm or
            WGPUTextureFormat.ASTC4X4Unorm or
            WGPUTextureFormat.ASTC5X4Unorm or
            WGPUTextureFormat.ASTC5X5Unorm or
            WGPUTextureFormat.ASTC6X5Unorm or
            WGPUTextureFormat.ASTC6X6Unorm or
            WGPUTextureFormat.ASTC8X5Unorm or
            WGPUTextureFormat.ASTC8X6Unorm or
            WGPUTextureFormat.ASTC8X8Unorm or
            WGPUTextureFormat.ASTC10X5Unorm or
            WGPUTextureFormat.ASTC10X6Unorm or
            WGPUTextureFormat.ASTC10X8Unorm or
            WGPUTextureFormat.ASTC10X10Unorm or
            WGPUTextureFormat.ASTC12X10Unorm or
            WGPUTextureFormat.ASTC12X12Unorm;

        public bool IsSNorm() => format is
            WGPUTextureFormat.R8Snorm or 
            WGPUTextureFormat.R16Snorm or 
            WGPUTextureFormat.RG8Snorm or 
            WGPUTextureFormat.RG16Snorm or 
            WGPUTextureFormat.RGBA8Snorm or 
            WGPUTextureFormat.RGBA16Snorm or 
            WGPUTextureFormat.BC4RSnorm or 
            WGPUTextureFormat.BC5RGSnorm or 
            WGPUTextureFormat.EACR11Snorm or 
            WGPUTextureFormat.EACRG11Snorm;

        public bool IsUInt() => format is 
            WGPUTextureFormat.R8Uint or
            WGPUTextureFormat.R16Uint or
            WGPUTextureFormat.RG8Uint or
            WGPUTextureFormat.R32Uint or
            WGPUTextureFormat.RG16Uint or
            WGPUTextureFormat.RGBA8Uint or
            WGPUTextureFormat.RGB10A2Uint or
            WGPUTextureFormat.RG32Uint or
            WGPUTextureFormat.RGBA16Uint or
            WGPUTextureFormat.RGBA32Uint;

        public bool IsSInt() => format is
            WGPUTextureFormat.R8Sint or
            WGPUTextureFormat.R16Sint or
            WGPUTextureFormat.RG8Sint or
            WGPUTextureFormat.R32Sint or
            WGPUTextureFormat.RG16Sint or
            WGPUTextureFormat.RGBA8Sint or
            WGPUTextureFormat.RG32Sint or
            WGPUTextureFormat.RGBA16Sint or
            WGPUTextureFormat.RGBA32Sint;

        public bool IsFloat() => format is 
            WGPUTextureFormat.R16Float or
            WGPUTextureFormat.R32Float or
            WGPUTextureFormat.RG16Float or
            WGPUTextureFormat.RG32Float or
            WGPUTextureFormat.RGBA16Float or
            WGPUTextureFormat.RGBA32Float or
            WGPUTextureFormat.Depth32Float or
            WGPUTextureFormat.BC6HRGBFloat;

        public bool IsUfloat() => format is 
            WGPUTextureFormat.RG11B10Ufloat or
            WGPUTextureFormat.RGB9E5Ufloat or
            WGPUTextureFormat.BC6HRGBUfloat;

        public bool IsSRGB() => format is
            WGPUTextureFormat.RGBA8UnormSrgb or
            WGPUTextureFormat.BGRA8UnormSrgb or
            WGPUTextureFormat.BC1RGBAUnormSrgb or
            WGPUTextureFormat.BC2RGBAUnormSrgb or
            WGPUTextureFormat.BC3RGBAUnormSrgb or
            WGPUTextureFormat.BC7RGBAUnormSrgb or
            WGPUTextureFormat.ETC2RGB8UnormSrgb or
            WGPUTextureFormat.ETC2RGB8A1UnormSrgb or
            WGPUTextureFormat.ETC2RGBA8UnormSrgb or
            WGPUTextureFormat.ASTC4X4UnormSrgb or
            WGPUTextureFormat.ASTC5X4UnormSrgb or
            WGPUTextureFormat.ASTC5X5UnormSrgb or
            WGPUTextureFormat.ASTC6X5UnormSrgb or
            WGPUTextureFormat.ASTC6X6UnormSrgb or
            WGPUTextureFormat.ASTC8X5UnormSrgb or
            WGPUTextureFormat.ASTC8X6UnormSrgb or
            WGPUTextureFormat.ASTC8X8UnormSrgb or
            WGPUTextureFormat.ASTC10X5UnormSrgb or
            WGPUTextureFormat.ASTC10X6UnormSrgb or
            WGPUTextureFormat.ASTC10X8UnormSrgb or
            WGPUTextureFormat.ASTC10X10UnormSrgb or
            WGPUTextureFormat.ASTC12X10UnormSrgb or
            WGPUTextureFormat.ASTC12X12UnormSrgb;

        public bool IsInt() => format.IsSInt() || format.IsUInt();
        public bool IsNorm() => format.IsUNorm() || format.IsSNorm();

        /// <summary>
        /// Returns the dimensions of a block (width, height, depth)
        /// </summary>
        /// <returns></returns>
        public (uint Width, uint Height, uint Depth) TexelBlockExtent()
        {
            if (!format.IsCompressed()) return (1, 1, 1);

            if (format.IsCompressedBc() || format.IsCompressedEtc2Eac()) return (4, 4, 1);

            return format switch
            {
                WGPUTextureFormat.ASTC4X4Unorm or WGPUTextureFormat.ASTC4X4UnormSrgb => (4, 4, 1),
                WGPUTextureFormat.ASTC5X4Unorm or WGPUTextureFormat.ASTC5X4UnormSrgb => (5, 4, 1),
                WGPUTextureFormat.ASTC5X5Unorm or WGPUTextureFormat.ASTC5X5UnormSrgb => (5, 5, 1),
                WGPUTextureFormat.ASTC6X5Unorm or WGPUTextureFormat.ASTC6X5UnormSrgb => (6, 5, 1),
                WGPUTextureFormat.ASTC6X6Unorm or WGPUTextureFormat.ASTC6X6UnormSrgb => (6, 6, 1),
                WGPUTextureFormat.ASTC8X5Unorm or WGPUTextureFormat.ASTC8X5UnormSrgb => (8, 5, 1),
                WGPUTextureFormat.ASTC8X6Unorm or WGPUTextureFormat.ASTC8X6UnormSrgb => (8, 6, 1),
                WGPUTextureFormat.ASTC8X8Unorm or WGPUTextureFormat.ASTC8X8UnormSrgb => (8, 8, 1),
                WGPUTextureFormat.ASTC10X5Unorm or WGPUTextureFormat.ASTC10X5UnormSrgb => (10, 5, 1),
                WGPUTextureFormat.ASTC10X6Unorm or WGPUTextureFormat.ASTC10X6UnormSrgb => (10, 6, 1),
                WGPUTextureFormat.ASTC10X8Unorm or WGPUTextureFormat.ASTC10X8UnormSrgb => (10, 8, 1),
                WGPUTextureFormat.ASTC10X10Unorm or WGPUTextureFormat.ASTC10X10UnormSrgb => (10, 10, 1),
                WGPUTextureFormat.ASTC12X10Unorm or WGPUTextureFormat.ASTC12X10UnormSrgb => (12, 10, 1),
                WGPUTextureFormat.ASTC12X12Unorm or WGPUTextureFormat.ASTC12X12UnormSrgb => (12, 12, 1),
                _ => (1, 1, 1)
            };
        }

        public uint DepthSize() => format switch
        {
            WGPUTextureFormat.Depth16Unorm => 16,
            WGPUTextureFormat.Depth24Plus or WGPUTextureFormat.Depth24PlusStencil8 => 24,
            WGPUTextureFormat.Depth32Float or WGPUTextureFormat.Depth32FloatStencil8 => 32,
            _ => 0
        };

        public uint StencilSize() => format.HasStencil() ? 8u : 0u;

        public WebGPUFormatInfo GetFormatInfo()
        {
            switch (format)
            {
                // 8-bit formats
                case WGPUTextureFormat.R8Unorm:
                case WGPUTextureFormat.R8Snorm:
                case WGPUTextureFormat.R8Uint:
                case WGPUTextureFormat.R8Sint:
                case WGPUTextureFormat.Stencil8:
                    return new(1, 1);
                // 16-bit formats
                case WGPUTextureFormat.R16Unorm:
                case WGPUTextureFormat.R16Snorm:
                case WGPUTextureFormat.R16Uint:
                case WGPUTextureFormat.R16Sint:
                case WGPUTextureFormat.R16Float:
                    return new(2, 1);
                case WGPUTextureFormat.RG8Unorm:
                case WGPUTextureFormat.RG8Snorm:
                case WGPUTextureFormat.RG8Uint:
                case WGPUTextureFormat.RG8Sint:
                    return new(2, 2);
                case WGPUTextureFormat.Depth16Unorm:
                    return new(2, 1);
                // 32-bit formats
                case WGPUTextureFormat.R32Float:
                case WGPUTextureFormat.R32Uint:
                case WGPUTextureFormat.R32Sint:
                    return new(4, 1);
                case WGPUTextureFormat.RG16Unorm:
                case WGPUTextureFormat.RG16Snorm:
                case WGPUTextureFormat.RG16Uint:
                case WGPUTextureFormat.RG16Sint:
                case WGPUTextureFormat.RG16Float:
                    return new(4, 2);
                case WGPUTextureFormat.RGBA8Unorm:
                case WGPUTextureFormat.RGBA8UnormSrgb:
                case WGPUTextureFormat.RGBA8Snorm:
                case WGPUTextureFormat.RGBA8Uint:
                case WGPUTextureFormat.RGBA8Sint:
                case WGPUTextureFormat.BGRA8Unorm:
                case WGPUTextureFormat.BGRA8UnormSrgb:
                case WGPUTextureFormat.RGB10A2Uint:
                case WGPUTextureFormat.RGB10A2Unorm:
                case WGPUTextureFormat.RG11B10Ufloat:
                case WGPUTextureFormat.RGB9E5Ufloat: // 4 channels conceptually
                    return new(4, 4);
                case WGPUTextureFormat.Depth32Float:
                case WGPUTextureFormat.Depth24Plus: // Logical size
                    return new(4, 1);
                // 64-bit formats
                case WGPUTextureFormat.RG32Float:
                case WGPUTextureFormat.RG32Uint:
                case WGPUTextureFormat.RG32Sint:
                    return new(8, 2);
                case WGPUTextureFormat.RGBA16Unorm:
                case WGPUTextureFormat.RGBA16Snorm:
                case WGPUTextureFormat.RGBA16Uint:
                case WGPUTextureFormat.RGBA16Sint:
                case WGPUTextureFormat.RGBA16Float:
                    return new(8, 4);
                case WGPUTextureFormat.Depth24PlusStencil8:
                case WGPUTextureFormat.Depth32FloatStencil8:
                    return new(8, 2);
                // 128-bit formats
                case WGPUTextureFormat.RGBA32Float:
                case WGPUTextureFormat.RGBA32Uint:
                case WGPUTextureFormat.RGBA32Sint:
                    return new(16, 4);
                // BC Compressed Formats (Bytes per 4x4 Block)
                case WGPUTextureFormat.BC1RGBAUnorm:
                case WGPUTextureFormat.BC1RGBAUnormSrgb:
                    return new(8, 4);
                case WGPUTextureFormat.BC4RUnorm:
                case WGPUTextureFormat.BC4RSnorm:
                    return new(8, 1);
                case WGPUTextureFormat.BC2RGBAUnorm:
                case WGPUTextureFormat.BC2RGBAUnormSrgb:
                case WGPUTextureFormat.BC3RGBAUnorm:
                case WGPUTextureFormat.BC3RGBAUnormSrgb:
                case WGPUTextureFormat.BC7RGBAUnorm:
                case WGPUTextureFormat.BC7RGBAUnormSrgb:
                    return new(16, 4);
                case WGPUTextureFormat.BC5RGUnorm:
                case WGPUTextureFormat.BC5RGSnorm:
                    return new(16, 2);
                case WGPUTextureFormat.BC6HRGBUfloat:
                case WGPUTextureFormat.BC6HRGBFloat:
                    return new(16, 3);
                // ETC2 / EAC Compressed Formats
                case WGPUTextureFormat.ETC2RGB8Unorm:
                case WGPUTextureFormat.ETC2RGB8UnormSrgb:
                    return new(8, 3);
                case WGPUTextureFormat.ETC2RGB8A1Unorm:
                case WGPUTextureFormat.ETC2RGB8A1UnormSrgb:
                    return new(8, 4);
                case WGPUTextureFormat.ETC2RGBA8Unorm:
                case WGPUTextureFormat.ETC2RGBA8UnormSrgb:
                    return new(16, 4);
                case WGPUTextureFormat.EACR11Unorm:
                case WGPUTextureFormat.EACR11Snorm:
                    return new(8, 1);
                case WGPUTextureFormat.EACRG11Unorm:
                case WGPUTextureFormat.EACRG11Snorm:
                    return new(16, 2);
                // ASTC Compressed Formats (All blocks in WebGPU take 16 bytes)
                case >= WGPUTextureFormat.ASTC4X4Unorm and <= WGPUTextureFormat.ASTC12X12UnormSrgb:
                    return new(16, 4);
                default:
                    return new(0, 0);
            }
        }

        public uint ElementSize() => format.GetFormatInfo().Size;
        public uint ChannelCount() => format.GetFormatInfo().Channels;

        public double TexelSize()
        {
            double texelSize = format.ElementSize();
            var blockExtent = format.TexelBlockExtent();
            var texelsPerBlock = blockExtent.Width * blockExtent.Height * blockExtent.Depth;
        
            if (1.0 < texelsPerBlock)
            {
                texelSize /= texelsPerBlock;
            }

            return texelSize;
        }

        public uint GetRegionSize(uint width, uint height, uint depth)
        {
            uint blockSizeInBytes;
            if (format.IsCompressed())
            {
                var blockSize = format.TexelBlockExtent();
                Debug.Assert((width % blockSize.Width == 0 || width < blockSize.Width) && 
                             (height % blockSize.Height == 0 || height < blockSize.Height));
            
                blockSizeInBytes = format.ElementSize();
                width = (width + blockSize.Width - 1) / blockSize.Width;
                height = (height + blockSize.Height - 1) / blockSize.Height;
            }
            else
            {
                blockSizeInBytes = format.ElementSize();
            }

            return width * height * depth * blockSizeInBytes;
        }
    }
}