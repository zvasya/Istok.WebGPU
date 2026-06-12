using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Examples.Utils;

public static class UtilsCubemap
{
    /// From Henry J. Warren's "Hacker's Delight"
    private static float RadicalInverseVdC(uint bits)
    {
        bits = (bits << 16) | (bits >> 16);
        bits = ((bits & 0x55555555u) << 1) | ((bits & 0xAAAAAAAAu) >> 1);
        bits = ((bits & 0x33333333u) << 2) | ((bits & 0xCCCCCCCCu) >> 2);
        bits = ((bits & 0x0F0F0F0Fu) << 4) | ((bits & 0xF0F0F0F0u) >> 4);
        bits = ((bits & 0x00FF00FFu) << 8) | ((bits & 0xFF00FF00u) >> 8);
        return bits * 2.3283064365386963e-10f; // / 0x100000000
    }

    /// From http://holger.dammertz.org/stuff/notes_HammersleyOnHemisphere.html
    private static Vector2 Hammersley2d(uint i, uint n)
    {
        return new Vector2((float)i / n, RadicalInverseVdC(i));
    }

    private static Vector3 FaceCoordsToXYZ(int i, int j, int faceID, int faceSize)
    {
        float a = 2.0f * i / faceSize;
        float b = 2.0f * j / faceSize;

        return faceID switch
        {
            0 => new Vector3(-1.0f, a - 1.0f, b - 1.0f),
            1 => new Vector3(a - 1.0f, -1.0f, 1.0f - b),
            2 => new Vector3(1.0f, a - 1.0f, 1.0f - b),
            3 => new Vector3(1.0f - a, 1.0f, 1.0f - b),
            4 => new Vector3(b - 1.0f, a - 1.0f, 1.0f),
            5 => new Vector3(1.0f - b, a - 1.0f, -1.0f),
            _ => new Vector3()
        };
    }

    public static Image<TPixel> ConvertEquirectangularMapToVerticalCross<TPixel>(Image<TPixel> b)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        int faceSize = b.Width / 4;

        int w = faceSize * 3;
        int h = faceSize * 4;

        var result = new Image<TPixel>(w, h);

        Span<Point> faceOffsets =
        [
            new Point(faceSize, faceSize * 3),
            new Point(0, faceSize),
            new Point(faceSize, faceSize),
            new Point(faceSize * 2, faceSize),
            new Point(faceSize, 0),
            new Point(faceSize, faceSize * 2)
        ];

        int clampW = b.Width - 1;
        int clampH = b.Height - 1;

        for (int face = 0; face != 6; face++)
        {
            for (int i = 0; i != faceSize; i++)
            {
                for (int j = 0; j != faceSize; j++)
                {
                    Vector3 p = FaceCoordsToXYZ(i, j, face, faceSize);
                    float r = MathF.Sqrt(p.X * p.X + p.Y * p.Y);
                    float theta = MathF.Atan2(p.Y, p.X);
                    float phi = MathF.Atan2(p.Z, r);
                    // float point source coordinates
                    float uf = 2.0f * faceSize * (theta + MathF.PI) / MathF.PI;
                    float vf = 2.0f * faceSize * (MathF.PI / 2.0f - phi) / MathF.PI;
                    // 4-samples for bilinear interpolation
                    int u1 = Math.Clamp((int)MathF.Floor(uf), 0, clampW);
                    int v1 = Math.Clamp((int)MathF.Floor(vf), 0, clampH);
                    int u2 = Math.Clamp(u1 + 1, 0, clampW);
                    int v2 = Math.Clamp(v1 + 1, 0, clampH);
                    // fractional part
                    float s = uf - u1;
                    float t = vf - v1;
                    // fetch 4-samples
                    Vector4 a = b[u1, v1].ToVector4();
                    Vector4 bb = b[u2, v1].ToVector4();
                    Vector4 c = b[u1, v2].ToVector4();
                    Vector4 d = b[u2, v2].ToVector4();
                    // bilinear interpolation
                    Vector4 color = a * (1 - s) * (1 - t) + bb * s * (1 - t) + c * (1 - s) * t + d * s * t;
                    TPixel pixel = default;
                    pixel.FromVector4(color);
                    result[i + faceOffsets[face].X, j + faceOffsets[face].Y] = pixel;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Converts a vertical-cross layout image into the 6 cube map faces, stacked
    /// vertically into a single image of size (faceWidth, faceHeight * 6).
    /// </summary>
    public static Image<TPixel> ConvertVerticalCrossToCubeMapFaces<TPixel>(Image<TPixel> b)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        int faceWidth = b.Width / 3;
        int faceHeight = b.Height / 4;

        var cubemap = new Image<TPixel>(faceWidth, faceHeight * 6);

        /*
            ------
            | +Y |
         ----------------
         | -X | -Z | +X |
         ----------------
            | -Y |
            ------
            | +Z |
            ------
        */

        for (int face = 0; face != 6; ++face)
        {
            for (int j = 0; j != faceHeight; ++j)
            {
                for (int i = 0; i != faceWidth; ++i)
                {
                    int x = 0;
                    int y = 0;

                    switch (face)
                    {
                        // CUBE_MAP_POSITIVE_X
                        case 0:
                            x = 2 * faceWidth + i;
                            y = 1 * faceHeight + j;
                            break;

                        // CUBE_MAP_NEGATIVE_X
                        case 1:
                            x = i;
                            y = faceHeight + j;
                            break;

                        // CUBE_MAP_POSITIVE_Y
                        case 2:
                            x = 1 * faceWidth + i;
                            y = j;
                            break;

                        // CUBE_MAP_NEGATIVE_Y
                        case 3:
                            x = 1 * faceWidth + i;
                            y = 2 * faceHeight + j;
                            break;

                        // CUBE_MAP_POSITIVE_Z
                        case 4:
                            x = faceWidth + i;
                            y = faceHeight + j;
                            break;

                        // CUBE_MAP_NEGATIVE_Z
                        case 5:
                            x = 2 * faceWidth - (i + 1);
                            y = b.Height - (j + 1);
                            break;
                    }

                    cubemap[i, face * faceHeight + j] = b[x, y];
                }
            }
        }

        return cubemap;
    }

    public static Image<TPixel> ConvertEquirectangularMapToCubeMapFaces<TPixel>(Image<TPixel> b)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        using var cross = ConvertEquirectangularMapToVerticalCross(b);
        return ConvertVerticalCrossToCubeMapFaces(cross);
    }

    public static void ConvolveLambertian(ReadOnlySpan<Vector3> data, int srcW, int srcH, int dstW, int dstH, Span<Vector3> output, int numMonteCarloSamples)
    {
        // only equirectangular maps are supported
        if (srcW != 2 * srcH)
            return;

        Vector3[] scratch = ResizeVec3(data, srcW, srcH, dstW, dstH);
        srcW = dstW;
        srcH = dstH;

        for (int y = 0; y != dstH; y++)
        {
            Console.WriteLine($"Line {y}...");
            float theta1 = (float)y / dstH * MathF.PI;
            for (int x = 0; x != dstW; x++)
            {
                float phi1 = (float)x / dstW * MathF.Tau;
                var v1 = new Vector3(MathF.Sin(theta1) * MathF.Cos(phi1), MathF.Sin(theta1) * MathF.Sin(phi1), MathF.Cos(theta1));
                var color = Vector3.Zero;
                float weight = 0.0f;
                for (int i = 0; i != numMonteCarloSamples; i++)
                {
                    Vector2 hh = Hammersley2d((uint)i, (uint)numMonteCarloSamples);
                    int x1 = (int)MathF.Floor(hh.X * srcW);
                    int y1 = (int)MathF.Floor(hh.Y * srcH);
                    float theta2 = (float)y1 / srcH * MathF.PI;
                    float phi2 = (float)x1 / srcW * MathF.Tau;
                    var v2 = new Vector3(MathF.Sin(theta2) * MathF.Cos(phi2), MathF.Sin(theta2) * MathF.Sin(phi2), MathF.Cos(theta2));
                    float dd = MathF.Max(0.0f, Vector3.Dot(v1, v2));
                    if (dd > 0.01f)
                    {
                        color += scratch[y1 * srcW + x1] * dd;
                        weight += dd;
                    }
                }
                output[y * dstW + x] = color / weight;
            }
        }
    }

    public static void ConvolveGGX(ReadOnlySpan<Vector3> data, int srcW, int srcH, int dstW, int dstH, Span<Vector3> output, int numMonteCarloSamples)
    {
        // only equirectangular maps are supported
        if (srcW != 2 * srcH)
            return;

        Vector3[] scratch = ResizeVec3(data, srcW, srcH, dstW, dstH);
        srcW = dstW;
        srcH = dstH;

        for (int y = 0; y != dstH; y++)
        {
            Console.WriteLine($"Line {y}...");
            float theta1 = (float)y / dstH * MathF.PI;
            for (int x = 0; x != dstW; x++)
            {
                float phi1 = (float)x / dstW * MathF.Tau;
                var v1 = new Vector3(MathF.Sin(theta1) * MathF.Cos(phi1), MathF.Sin(theta1) * MathF.Sin(phi1), MathF.Cos(theta1));
                var color = Vector3.Zero;
                float weight = 0.0f;
                for (int i = 0; i != numMonteCarloSamples; i++)
                {
                    Vector2 hh = Hammersley2d((uint)i, (uint)numMonteCarloSamples);
                    int x1 = (int)MathF.Floor(hh.X * srcW);
                    int y1 = (int)MathF.Floor(hh.Y * srcH);
                    float theta2 = (float)y1 / srcH * MathF.PI;
                    float phi2 = (float)x1 / srcW * MathF.Tau;
                    var v2 = new Vector3(MathF.Sin(theta2) * MathF.Cos(phi2), MathF.Sin(theta2) * MathF.Sin(phi2), MathF.Cos(theta2));
                    float dd = MathF.Max(0.0f, Vector3.Dot(v1, v2));
                    if (dd > 0.01f)
                    {
                        color += scratch[y1 * srcW + x1] * dd;
                        weight += dd;
                    }
                }
                output[y * dstW + x] = color / weight;
            }
        }
    }

    // Replaces the stb_image_resize CUBICBSPLINE resize used in the original C++.
    // Note: ImageSharp's resampler does not expose stb's EDGE_WRAP mode, so the
    // horizontal seam is handled with the default edge behaviour instead of wrapping.
    private static Vector3[] ResizeVec3(ReadOnlySpan<Vector3> data, int srcW, int srcH, int dstW, int dstH)
    {
        using var image = new Image<RgbaVector>(srcW, srcH);
        for (int y = 0; y < srcH; y++)
        {
            for (int x = 0; x < srcW; x++)
            {
                Vector3 v = data[y * srcW + x];
                image[x, y] = new RgbaVector(v.X, v.Y, v.Z, 1.0f);
            }
        }

        image.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(dstW, dstH),
            Sampler = KnownResamplers.Bicubic
        }));

        var result = new Vector3[dstW * dstH];
        for (int y = 0; y < dstH; y++)
        {
            for (int x = 0; x < dstW; x++)
            {
                RgbaVector px = image[x, y];
                result[y * dstW + x] = new Vector3(px.R, px.G, px.B);
            }
        }

        return result;
    }
}
