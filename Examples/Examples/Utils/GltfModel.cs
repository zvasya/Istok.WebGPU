using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using AssetManager;
using glTFLoader;
using glTFLoader.Schema;

namespace Examples.Utils;

// Static, uncompressed glTF triangle meshes. LoadAsync preserves the first-primitive
// behavior; LoadSceneAsync also resolves scene nodes and their world transforms.
internal sealed record GltfModel(GltfModel.Vertex[] Vertices, uint[] Indices, GltfModel.MaterialData Material)
{
    public Vector4 BaseColor => Material.BaseColorFactor;
    public byte[]? BaseColorImage => Material.BaseColorTexture.Image;

    internal sealed record TextureData(byte[]? Image, int TexCoord, Sampler? Sampler);
    internal sealed record MaterialData(Vector4 BaseColorFactor, Vector4 MetallicRoughnessNormalOcclusion,
        Vector4 EmissiveAlphaCutoff, uint AlphaMode, bool DoubleSided,
        TextureData BaseColorTexture, TextureData MetallicRoughnessTexture, TextureData NormalTexture,
        TextureData OcclusionTexture, TextureData EmissiveTexture,
        Vector4 SpecularGlossinessFactor, bool SpecularGlossiness);

    internal sealed record SceneMesh(GltfModel Model, Matrix4x4 Transform);
    [StructLayout(LayoutKind.Sequential)]
    internal struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector4 Color;
        public Vector2 UV;
        public Vector2 UV1;
    }

    public static async Task<GltfModel> LoadAsync(IResourcesProvider resources, string path)
    {
        var loader = await Loader.CreateAsync(resources, path);
        var primitive = loader.Model.Meshes?.FirstOrDefault()?.Primitives?.FirstOrDefault()
            ?? throw new InvalidDataException("The glTF model has no mesh primitives.");
        return await loader.LoadPrimitiveAsync(primitive);
    }

    public static async Task<SceneMesh[]> LoadSceneAsync(IResourcesProvider resources, string path)
    {
        var loader = await Loader.CreateAsync(resources, path);
        var model = loader.Model;
        if (model.Scenes == null || model.Scenes.Length == 0)
            throw new InvalidDataException("The glTF model has no scene.");
        var result = new List<SceneMesh>();
        var ancestors = new HashSet<int>();
        var meshes = new Dictionary<(int, int), GltfModel>();
        foreach (int node in model.Scenes[model.Scene ?? 0].Nodes ?? [])
            await Visit(node, Matrix4x4.Identity);
        return result.ToArray();

        async Task Visit(int index, Matrix4x4 parent)
        {
            if (!ancestors.Add(index)) throw new InvalidDataException("Cycle in glTF scene nodes.");
            var node = model.Nodes[index];
            Matrix4x4 local;
            // glTF2Loader supplies an identity Matrix even when JSON contains only TRS.
            if (loader.HasNodeMatrix(index) && node.Matrix is { } m)
                local = new Matrix4x4(m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7],
                    m[8], m[9], m[10], m[11], m[12], m[13], m[14], m[15]);
            else
            {
                var t = node.Translation;
                var r = node.Rotation;
                var scale = node.Scale;
                local = Matrix4x4.CreateScale(scale == null ? Vector3.One : new Vector3(scale[0], scale[1], scale[2]))
                    * Matrix4x4.CreateFromQuaternion(r == null ? Quaternion.Identity : new Quaternion(r[0], r[1], r[2], r[3]))
                    * Matrix4x4.CreateTranslation(t == null ? Vector3.Zero : new Vector3(t[0], t[1], t[2]));
            }
            var world = local * parent; // System.Numerics uses row vectors.
            if (node.Mesh is { } mesh)
                for (int i = 0; i < model.Meshes[mesh].Primitives.Length; i++)
                {
                    if (!meshes.TryGetValue((mesh, i), out var loaded))
                        meshes[(mesh, i)] = loaded = await loader.LoadPrimitiveAsync(model.Meshes[mesh].Primitives[i]);
                    result.Add(new SceneMesh(loaded, world));
                }
            foreach (int child in node.Children ?? []) await Visit(child, world);
            ancestors.Remove(index);
        }
    }

    private sealed class Loader(IResourcesProvider resources, string directory, Gltf model, JsonElement json)
    {
        public Gltf Model => model;
        public bool HasNodeMatrix(int index) => json.GetProperty("nodes")[index].TryGetProperty("matrix", out _);
        private readonly Dictionary<string, byte[]> _uris = [];
        private byte[][] buffers = [];

        public static async Task<Loader> CreateAsync(IResourcesProvider resources, string path)
        {
            string text = await resources.LoadTextAsync(path);
            var model = Interface.DeserializeModel(text);
            foreach (string extension in model.ExtensionsRequired ?? [])
                if (extension != "KHR_materials_pbrSpecularGlossiness")
                    throw new NotSupportedException($"Unsupported required glTF extension: {extension}.");
            using var document = JsonDocument.Parse(text);
            var loader = new Loader(resources, path[..(path.LastIndexOf('/') + 1)], model, document.RootElement.Clone());
            loader.buffers = new byte[model.Buffers.Length][];
            for (int i = 0; i < loader.buffers.Length; i++)
            {
                if (model.Buffers[i].Uri == null)
                    throw new NotSupportedException("This example expects .gltf with external or data-URI buffers.");
                loader.buffers[i] = await loader.ReadUri(model.Buffers[i].Uri);
                if (loader.buffers[i].Length < model.Buffers[i].ByteLength)
                    throw new InvalidDataException("The glTF buffer is shorter than its declared byte length.");
            }
            return loader;
        }

        private async Task<byte[]> ReadUri(string uri)
        {
            if (_uris.TryGetValue(uri, out var bytes)) return bytes;
            bytes = uri.StartsWith("data:", StringComparison.Ordinal)
                ? Convert.FromBase64String(uri[(uri.IndexOf(',') + 1)..])
                : await resources.LoadBytesAsync(directory + Uri.UnescapeDataString(uri));
            _uris[uri] = bytes;
            return bytes;
        }

        public async Task<GltfModel> LoadPrimitiveAsync(MeshPrimitive primitive)
        {
            if (primitive.Mode != MeshPrimitive.ModeEnum.TRIANGLES)
                throw new NotSupportedException("This example supports triangle lists only.");

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
            float[]? uv1 = primitive.Attributes.TryGetValue("TEXCOORD_1", out int uv1Index)
                ? ReadAttribute(uv1Index, Accessor.TypeEnum.VEC2, 2) : null;
            float[]? normals = primitive.Attributes.TryGetValue("NORMAL", out int normalIndex)
                ? ReadAttribute(normalIndex, Accessor.TypeEnum.VEC3, 3) : null;
            int colorComponents = 4;
            float[]? colors = null;
            if (primitive.Attributes.TryGetValue("COLOR_0", out int colorIndex))
            {
                colorComponents = model.Accessors[colorIndex].Type == Accessor.TypeEnum.VEC3 ? 3 : 4;
                colors = ReadAttribute(colorIndex, colorComponents == 3 ? Accessor.TypeEnum.VEC3 : Accessor.TypeEnum.VEC4, colorComponents);
            }
            if ((normals != null && normals.Length != vertices.Length * 3) ||
                (uv1 != null && uv1.Length != vertices.Length * 2) || (uv != null && uv.Length != vertices.Length * 2) ||
                (colors != null && colors.Length != vertices.Length * colorComponents))
                throw new InvalidDataException("Vertex attribute counts do not match POSITION.");
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new Vertex
                {
                    Position = new Vector3(positions[3 * i], positions[3 * i + 1], positions[3 * i + 2]),
                    Normal = normals == null ? Vector3.UnitY : new Vector3(normals[3 * i], normals[3 * i + 1], normals[3 * i + 2]),
                    Color = colors == null ? Vector4.One : new Vector4(colors[colorComponents * i],
                        colors[colorComponents * i + 1], colors[colorComponents * i + 2], colorComponents == 4 ? colors[colorComponents * i + 3] : 1),
                    // ImageSharp and WebGPU both use top-left image coordinates; glTF UVs need no flip.
                    UV = uv == null ? Vector2.Zero : new Vector2(uv[2 * i], uv[2 * i + 1]),
                    UV1 = uv1 == null ? Vector2.Zero : new Vector2(uv1[2 * i], uv1[2 * i + 1]),
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

            var material = primitive.Material.HasValue ? model.Materials[primitive.Material.Value] : null;
            var pbr = material?.PbrMetallicRoughness;
            JsonElement sg = default;
            if (primitive.Material is { } materialIndex &&
                json.GetProperty("materials")[materialIndex].TryGetProperty("extensions", out var extensions))
                extensions.TryGetProperty("KHR_materials_pbrSpecularGlossiness", out sg);
            bool specGloss = sg.ValueKind == JsonValueKind.Object;
            float[]? Array(string name) => specGloss && sg.TryGetProperty(name, out var value)
                ? value.EnumerateArray().Select(x => x.GetSingle()).ToArray() : null;
            (int? Index, int UV) Texture(string name)
            {
                if (!specGloss || !sg.TryGetProperty(name, out var value)) return (null, 0);
                return (value.GetProperty("index").GetInt32(), value.TryGetProperty("texCoord", out var uvSet) ? uvSet.GetInt32() : 0);
            }
            var diffuseTexture = Texture("diffuseTexture");
            var specularTexture = Texture("specularGlossinessTexture");
            var specular = Array("specularFactor");
            var factor = specGloss ? Array("diffuseFactor") : pbr?.BaseColorFactor;
            var emissive = material?.EmissiveFactor;
            var data = new MaterialData(
                factor == null ? Vector4.One : new Vector4(factor[0], factor[1], factor[2], factor[3]),
                new Vector4(pbr?.MetallicFactor ?? 1, pbr?.RoughnessFactor ?? 1,
                    material?.NormalTexture?.Scale ?? 1, material?.OcclusionTexture?.Strength ?? 1),
                new Vector4(emissive == null ? Vector3.Zero : new Vector3(emissive[0], emissive[1], emissive[2]), material?.AlphaCutoff ?? 0.5f),
                material?.AlphaMode.Equals(glTFLoader.Schema.Material.AlphaModeEnum.MASK) == true ? 1u :
                    material?.AlphaMode.Equals(glTFLoader.Schema.Material.AlphaModeEnum.BLEND) == true ? 2u : 0u,
                material?.DoubleSided ?? false,
                await LoadTexture(specGloss ? diffuseTexture.Index : pbr?.BaseColorTexture?.Index, specGloss ? diffuseTexture.UV : pbr?.BaseColorTexture?.TexCoord ?? 0),
                await LoadTexture(specGloss ? specularTexture.Index : pbr?.MetallicRoughnessTexture?.Index, specGloss ? specularTexture.UV : pbr?.MetallicRoughnessTexture?.TexCoord ?? 0),
                await LoadTexture(material?.NormalTexture?.Index, material?.NormalTexture?.TexCoord ?? 0),
                await LoadTexture(material?.OcclusionTexture?.Index, material?.OcclusionTexture?.TexCoord ?? 0),
                await LoadTexture(material?.EmissiveTexture?.Index, material?.EmissiveTexture?.TexCoord ?? 0),
                new Vector4(specular == null ? Vector3.One : new Vector3(specular[0], specular[1], specular[2]),
                    specGloss && sg.TryGetProperty("glossinessFactor", out var gloss) ? gloss.GetSingle() : 1), specGloss);
            return new GltfModel(vertices, indices, data);

            async Task<TextureData> LoadTexture(int? index, int texCoord)
            {
                if (texCoord is < 0 or > 1)
                    throw new NotSupportedException("Only TEXCOORD_0 and TEXCOORD_1 are supported.");
                if (index == null)
                    return new TextureData(null, texCoord, null);
                if ((texCoord == 0 && uv == null) || (texCoord == 1 && uv1 == null))
                    throw new InvalidDataException($"Texture references missing TEXCOORD_{texCoord}.");
                var texture = model.Textures[index.Value];
                var source = model.Images[texture.Source ?? throw new InvalidDataException("Texture has no image source.")];
                byte[] image;
                if (source.Uri != null)
                    image = await ReadUri(source.Uri);
                else if (source.BufferView is { } viewIndex)
                {
                    var view = model.BufferViews[viewIndex];
                    image = buffers[view.Buffer].AsSpan(view.ByteOffset, view.ByteLength).ToArray();
                }
                else
                    throw new InvalidDataException("Texture image has no data.");
                return new TextureData(image, texCoord, texture.Sampler.HasValue ? model.Samplers[texture.Sampler.Value] : null);
            }
        }
    }
}
