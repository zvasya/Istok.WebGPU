using System.Numerics;

namespace Examples.Utils;

// Floating-point counterpart of the vertical-cross conversion.
// Vector4 arithmetic preserves radiance above one throughout resampling.
public static class UtilsHdrCubemap
{
    public static Vector4[] ConvertEquirectangularToCubeFaces(ReadOnlySpan<Vector4> panorama, int width, int height)
    {
        if (width < 4 || width != height * 2 || width % 4 != 0 || panorama.Length != checked(width * height))
            throw new ArgumentException("Expected a complete 2:1 equirectangular panorama with width divisible by four.");
        int size = width / 4;
        var result = new Vector4[checked(size * size * 6)];
        for (int face = 0; face < 6; face++)
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float a = 2.0f * x / size;
            float b = 2.0f * y / size;
            // Compose faceCoordsToXYZ with the book's vertical-cross face extraction.
            var direction = face switch
            {
                0 => new Vector3(1 - a, 1, 1 - b),
                1 => new Vector3(a - 1, -1, 1 - b),
                2 => new Vector3(b - 1, a - 1, 1),
                3 => new Vector3(1 - b, a - 1, -1),
                // Match UtilsCubemap's +Z orientation, including its corrected vertical sign.
                4 => new Vector3(1, a - 1, 1 - b),
                _ => new Vector3(-1, 1 - 2.0f * (x + 1) / size, 1 - 2.0f * (y + 1) / size),
            };
            float theta = MathF.Atan2(direction.Y, direction.X);
            float phi = MathF.Atan2(direction.Z, MathF.Sqrt(direction.X * direction.X + direction.Y * direction.Y));
            float u = width * (theta + MathF.PI) / MathF.Tau;
            float v = height * (MathF.PI / 2 - phi) / MathF.PI;
            int x0 = Math.Clamp((int)MathF.Floor(u), 0, width - 1);
            int y0 = Math.Clamp((int)MathF.Floor(v), 0, height - 1);
            int x1 = Math.Min(x0 + 1, width - 1);
            int y1 = Math.Min(y0 + 1, height - 1);
            float s = u - x0;
            float t = v - y0;
            var color = Vector4.Lerp(Vector4.Lerp(panorama[y0 * width + x0], panorama[y0 * width + x1], s),
                Vector4.Lerp(panorama[y1 * width + x0], panorama[y1 * width + x1], s), t);
            color.W = 1;
            result[(face * size + y) * size + x] = color;
        }
        return result;
    }

    public static Vector4[] DownsampleFace(ReadOnlySpan<Vector4> source, int size)
    {
        if (size <= 1 || source.Length != checked(size * size))
            throw new ArgumentException("Expected a square face larger than one texel.");
        int targetSize = Math.Max(1, size / 2);
        var result = new Vector4[targetSize * targetSize];
        float scale = (float)size / targetSize;
        // Triangle reconstruction with a downsampling-scaled footprint and clamped edges.
        // Each face is filtered separately, just like the original stb resize calls.
        for (int y = 0; y < targetSize; y++)
        for (int x = 0; x < targetSize; x++)
        {
            float cx = (x + 0.5f) * scale - 0.5f;
            float cy = (y + 0.5f) * scale - 0.5f;
            var color = Vector4.Zero;
            float weight = 0;
            for (int sy = (int)MathF.Ceiling(cy - scale); sy <= (int)MathF.Floor(cy + scale); sy++)
            for (int sx = (int)MathF.Ceiling(cx - scale); sx <= (int)MathF.Floor(cx + scale); sx++)
            {
                float w = MathF.Max(1 - MathF.Abs(sx - cx) / scale, 0) * MathF.Max(1 - MathF.Abs(sy - cy) / scale, 0);
                color += source[Math.Clamp(sy, 0, size - 1) * size + Math.Clamp(sx, 0, size - 1)] * w;
                weight += w;
            }
            result[y * targetSize + x] = color / weight;
        }
        return result;
    }
}
