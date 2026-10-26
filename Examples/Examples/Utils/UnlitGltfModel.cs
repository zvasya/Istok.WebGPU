using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;
using AssetManager;
using glTFLoader;
using glTFLoader.Schema;

namespace Examples.Utils;

// Render the first mesh primitive, without applying the glTF scene graph.
// This reader deliberately covers that static, uncompressed triangle-mesh example.
internal sealed record UnlitGltfModel(UnlitGltfModel.Vertex[] Vertices, uint[] Indices,
    Vector4 BaseColor, byte[]? BaseColorImage)
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Vertex
    {
        public Vector3 Position;
        public Vector4 Color;
        public Vector2 UV;
    }

    public static async Task<UnlitGltfModel> LoadAsync(IResourcesProvider resources, string path)
    {
        var model = Interface.DeserializeModel(await resources.LoadTextAsync(path));
        if (model.ExtensionsRequired is { Length: > 0 })
            throw new NotSupportedException("The unlit example requires an uncompressed core glTF 2.0 model.");
        var primitive = model.Meshes?.FirstOrDefault()?.Primitives?.FirstOrDefault()
            ?? throw new InvalidDataException("The glTF model has no mesh primitives.");
        if (primitive.Mode != MeshPrimitive.ModeEnum.TRIANGLES)
            throw new NotSupportedException("The unlit example supports triangle lists only.");

        string directory = path[..(path.LastIndexOf('/') + 1)];
        async Task<byte[]> ReadUri(string uri)
        {
            if (uri.StartsWith("data:", StringComparison.Ordinal))
                return Convert.FromBase64String(uri[(uri.IndexOf(',') + 1)..]);
            return await resources.LoadBytesAsync(directory + Uri.UnescapeDataString(uri));
        }

        // Resolve asynchronously through the same provider used by desktop, mobile and browser examples.
        var buffers = new byte[model.Buffers.Length][];
        for (int i = 0; i < buffers.Length; i++)
        {
            if (model.Buffers[i].Uri == null)
                throw new NotSupportedException("This example expects a .gltf file with external or data-URI buffers.");
            buffers[i] = await ReadUri(model.Buffers[i].Uri);
            if (buffers[i].Length < model.Buffers[i].ByteLength)
                throw new InvalidDataException("The glTF buffer is shorter than its declared byte length.");
        }

        Accessor GetAccessor(int index, Accessor.TypeEnum type)
        {
            var accessor = model.Accessors[index];
            if (accessor.Type != type || accessor.Sparse != null || !accessor.BufferView.HasValue)
                throw new NotSupportedException($"Accessor {index} must be a dense {type} accessor.");
            return accessor;
        }

        // Both the buffer-view and accessor offsets matter, as does the optional interleaved stride.
        ReadOnlySpan<byte> Element(Accessor accessor, int index, int size)
        {
            var view = model.BufferViews[accessor.BufferView!.Value];
            int stride = view.ByteStride ?? size;
            int offset = checked(accessor.ByteOffset + index * stride);
            if (stride < size || offset < 0 || (long)offset + size > view.ByteLength)
                throw new InvalidDataException("Accessor element falls outside its buffer view.");
            return buffers[view.Buffer].AsSpan(checked(view.ByteOffset + offset), size);
        }

        float[] ReadAttribute(int index, Accessor.TypeEnum type, int components)
        {
            var accessor = GetAccessor(index, type);
            int componentSize = accessor.ComponentType switch
            {
                Accessor.ComponentTypeEnum.FLOAT => 4,
                Accessor.ComponentTypeEnum.UNSIGNED_BYTE when accessor.Normalized => 1,
                Accessor.ComponentTypeEnum.UNSIGNED_SHORT when accessor.Normalized => 2,
                _ => throw new NotSupportedException("Expected float or normalized unsigned vertex attributes."),
            };
            var result = new float[checked(accessor.Count * components)];
            for (int i = 0; i < accessor.Count; i++)
            {
                var element = Element(accessor, i, components * componentSize);
                for (int c = 0; c < components; c++)
                {
                    var value = element[(c * componentSize)..];
                    result[i * components + c] = componentSize switch
                    {
                        4 => BinaryPrimitives.ReadSingleLittleEndian(value),
                        2 => BinaryPrimitives.ReadUInt16LittleEndian(value) / 65535.0f,
                        _ => value[0] / 255.0f,
                    };
                }
            }
            return result;
        }

        var positions = ReadAttribute(primitive.Attributes["POSITION"], Accessor.TypeEnum.VEC3, 3);
        var vertices = new Vertex[positions.Length / 3];
        float[]? uv = primitive.Attributes.TryGetValue("TEXCOORD_0", out int uvIndex)
            ? ReadAttribute(uvIndex, Accessor.TypeEnum.VEC2, 2) : null;
        int colorComponents = 4;
        float[]? colors = null;
        if (primitive.Attributes.TryGetValue("COLOR_0", out int colorIndex))
        {
            colorComponents = model.Accessors[colorIndex].Type == Accessor.TypeEnum.VEC3 ? 3 : 4;
            colors = ReadAttribute(colorIndex, colorComponents == 3 ? Accessor.TypeEnum.VEC3 : Accessor.TypeEnum.VEC4, colorComponents);
        }
        if ((uv != null && uv.Length != vertices.Length * 2) ||
            (colors != null && colors.Length != vertices.Length * colorComponents))
            throw new InvalidDataException("Vertex attribute counts do not match POSITION.");
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new Vertex
            {
                Position = new Vector3(positions[3 * i], positions[3 * i + 1], positions[3 * i + 2]),
                Color = colors == null ? Vector4.One : new Vector4(colors[colorComponents * i],
                    colors[colorComponents * i + 1], colors[colorComponents * i + 2], colorComponents == 4 ? colors[colorComponents * i + 3] : 1),
                // ImageSharp and WebGPU both use top-left image coordinates; glTF UVs need no flip.
                UV = uv == null ? Vector2.Zero : new Vector2(uv[2 * i], uv[2 * i + 1]),
            };
        }

        uint[] indices;
        if (primitive.Indices.HasValue)
        {
            var accessor = GetAccessor(primitive.Indices.Value, Accessor.TypeEnum.SCALAR);
            int size = accessor.ComponentType switch
            {
                Accessor.ComponentTypeEnum.UNSIGNED_BYTE => 1,
                Accessor.ComponentTypeEnum.UNSIGNED_SHORT => 2,
                Accessor.ComponentTypeEnum.UNSIGNED_INT => 4,
                _ => throw new NotSupportedException("Unsupported glTF index component type."),
            };
            indices = new uint[accessor.Count];
            for (int i = 0; i < indices.Length; i++)
            {
                var element = Element(accessor, i, size);
                indices[i] = size switch { 1 => element[0], 2 => BinaryPrimitives.ReadUInt16LittleEndian(element), _ => BinaryPrimitives.ReadUInt32LittleEndian(element) };
                if (indices[i] >= vertices.Length)
                    throw new InvalidDataException("Index falls outside the vertex buffer.");
            }
        }
        else
            indices = Enumerable.Range(0, vertices.Length).Select(i => (uint)i).ToArray();
        if (indices.Length == 0 || indices.Length % 3 != 0)
            throw new InvalidDataException("A triangle list must contain complete triangles.");

        var material = primitive.Material.HasValue ? model.Materials[primitive.Material.Value].PbrMetallicRoughness : null;
        Vector4 baseColor = material?.BaseColorFactor is { } factor
            ? new Vector4(factor[0], factor[1], factor[2], factor[3]) : Vector4.One;
        byte[]? image = null;
        if (material?.BaseColorTexture is { } textureInfo)
        {
            if (textureInfo.TexCoord != 0)
                throw new NotSupportedException("The unlit example uses TEXCOORD_0 only.");
            var texture = model.Textures[textureInfo.Index];
            var source = model.Images[texture.Source ?? throw new InvalidDataException("Texture has no image source.")];
            if (source.Uri != null)
                image = await ReadUri(source.Uri);
            else if (source.BufferView is { } viewIndex)
            {
                var view = model.BufferViews[viewIndex];
                image = buffers[view.Buffer].AsSpan(view.ByteOffset, view.ByteLength).ToArray();
            }
            else
                throw new InvalidDataException("Base color image has no data.");
        }
        return new UnlitGltfModel(vertices, indices, baseColor, image);
    }
}
