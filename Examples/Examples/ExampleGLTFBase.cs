using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using AssetManager;
using Examples.Utils;
using SixLabors.ImageSharp.PixelFormats;
using Istok.WebGPU;
using Istok.WebGPU.LowLevel;
using Istok.WebGPU.View;
using Silk.NET.Maths;
using static Istok.WebGPU.LowLevel.WebGPUNative;

namespace Examples;

// Shared static glTF scene renderer: material resources, IBL, frame/depth handling,
// and optional opaque-background capture for transmission and volume.
public abstract class ExampleGLTFBase : ExampleBase
{
    private const WGPUTextureFormat DepthFormat = WGPUTextureFormat.Depth32Float;

    private GPUShaderModule _shader = null!;
    private sealed class MeshDraw
    {
        public GPURenderPipeline Pipeline = null!;
        public GPUBuffer VertexBuffer = null!, IndexBuffer = null!, MaterialBuffer = null!, PerFrameBuffer = null!, UvBuffer = null!;
        public GPUBindGroup BindGroup = null!, MaterialBindGroup = null!, EnvironmentBindGroup = null!;
        public GPUBindGroup? BackgroundBindGroup;
        public bool Transmissive;
        public uint IndexCount;
        public Matrix4x4 Transform;
    }
    private readonly List<MeshDraw> _draws = [];
    private readonly List<SampledTexture> _textures = [];
    private GPUTexture? _depthTexture;
    private GPUTextureView? _depthTextureView;
    private readonly Stopwatch _timer = new();
    private bool _disposed;
    private MipmapGenerator _mipmaps = null!;
    private GPUTexture? _background;
    private GPUTextureView? _backgroundView, _backgroundAttachment;
    private GPUShaderModule _copyShader = null!;
    private GPURenderPipeline _copyPipeline = null!;
    private GPUBindGroup? _copyGroup;
    private GPUSampler _backgroundSampler = null!;

    [StructLayout(LayoutKind.Sequential)]
    private struct PerFrameData
    {
        public Matrix4x4 Model;
        public Matrix4x4 Normal;
        public Matrix4x4 ViewProjection;
        public Vector4 CameraPosition;
    }

    // These blocks preserve the existing WGSL layouts: 112 bytes for Clearcoat/Sheen,
    // 128 bytes for Transmission/Volume, 144 bytes for Specular + Sheen.
    [StructLayout(LayoutKind.Sequential)]
    private struct MaterialHeader
    {
        public Vector4 BaseColor, MetallicRoughnessNormalOcclusion, EmissiveAlphaCutoff;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MaterialCoordinates
    {
        public uint BaseColorUV, MetallicRoughnessUV, NormalUV, OcclusionUV;
        public uint EmissiveUV, AlphaMode, TargetSrgb, Padding;
        public uint EffectUV0, EffectUV1, EffectUV2, EffectUV3;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SurfaceMaterialData
    {
        public MaterialHeader Header;
        public Vector4 Effect;
        public MaterialCoordinates Coordinates;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TransmissionMaterialData
    {
        public MaterialHeader Header;
        public Vector4 Effect, AttenuationColor;
        public MaterialCoordinates Coordinates;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SpecularMaterialData
    {
        public MaterialHeader Header;
        public Vector4 Sheen, Specular, Optics; // RGB/roughness, RGB/strength, IOR
        public MaterialCoordinates Coordinates;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TextureTransform
    {
        public Vector4 U, V;
    }

    protected ExampleGLTFBase(GPU instance, GPUDevice device, IWebGpuView window,
        GPUSurfaceCapabilities surfaceCapabilities, GPUSurface surface, IResourcesProvider resourcesProvider)
        : base(instance, device, window, surfaceCapabilities, surface, resourcesProvider)
    {
    }

    protected enum GLTFMaterialType { Clearcoat, Sheen, Transmission, Specular }

    protected abstract string ModelPath { get; }
    protected abstract string ExampleName { get; }
    protected abstract GLTFMaterialType MaterialType { get; }
    protected abstract Vector3 CameraPosition { get; }
    protected virtual Vector3 CameraTarget => Vector3.Zero;
    protected virtual float NearPlane => 0.01f;
    protected virtual float FarPlane => 100;
    protected virtual Matrix4x4 ModelTransform(float time) => Matrix4x4.Identity;

    private bool CaptureOpaqueBackground => MaterialType == GLTFMaterialType.Transmission;
    private bool HasSheen => MaterialType is GLTFMaterialType.Sheen or GLTFMaterialType.Specular;
    private bool HasUVTransforms => MaterialType != GLTFMaterialType.Clearcoat;
    private string ShaderPath => MaterialType switch
    {
        GLTFMaterialType.Clearcoat => "Shaders/example_clearcoat.wgsl",
        GLTFMaterialType.Sheen => "Shaders/example_sheen.wgsl",
        GLTFMaterialType.Transmission => "Shaders/example_transmission.wgsl",
        GLTFMaterialType.Specular => "Shaders/example_specular.wgsl",
        _ => throw new ArgumentOutOfRangeException(),
    };

    public override async Task OnLoad()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        try { await LoadSceneAsync(); }
        catch { Dispose(); throw; }
    }

    private async Task LoadSceneAsync()
    {
        var scene = await GltfModel.LoadSceneAsync(_ResourcesProvider, ModelPath);
        string code = await _ResourcesProvider.LoadTextAsync(ShaderPath);
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException($"{ExampleName} shader source must not be empty.");
        _shader = _device.CreateShaderModule(new GPUShaderModuleDescriptor { Code = code });
        _mipmaps = new MipmapGenerator(_device, await _ResourcesProvider.LoadTextAsync("Shaders/generate_mipmaps.wgsl"));
        var environment = new List<SampledTexture>
        {
            await LoadEnvironmentAsync("Textures/piazza_bologni_1k_prefilter.ktx", 6),
            await LoadEnvironmentAsync("Textures/piazza_bologni_1k_irradiance.ktx", 6),
            await LoadEnvironmentAsync("Textures/brdfLUT.ktx", 1),
        };
        if (HasSheen)
            environment.Add(await LoadEnvironmentAsync("Textures/piazza_bologni_1k_charlie.ktx", 6));
        foreach (var mesh in scene) CreateMesh(mesh, environment);
        if (CaptureOpaqueBackground)
        {
            _backgroundSampler = _device.CreateSampler(new GPUSamplerDescriptor
            {
                MinFilter = WGPUFilterMode.Linear, MagFilter = WGPUFilterMode.Linear,
                MipmapFilter = WGPUMipmapFilterMode.Linear, LodMaxClamp = 32,
            });
            string copyCode = await _ResourcesProvider.LoadTextAsync("Shaders/example_transmission_background.wgsl");
            _copyShader = _device.CreateShaderModule(new GPUShaderModuleDescriptor
            {
                Code = copyCode,
            });
            _copyPipeline = _device.CreateRenderPipeline(new GPURenderPipelineDescriptor
            {
                Layout = GPUPipelineLayout.Auto,
                Vertex = new GPUVertexState { Module = _copyShader, EntryPoint = "vs_main" },
                Fragment = new GPUFragmentState
                {
                    Module = _copyShader, EntryPoint = SurfaceFormat().IsSRGB() ? "fs_linear" : "fs_gamma",
                    Targets = [new GPUColorTargetState { Format = SurfaceFormat(), WriteMask = WGPUColorWriteMask.All }],
                },
            });
        }
        CreateSwapchain();
        _timer.Restart();
        Console.WriteLine($"{ExampleName}: loaded {scene.Length} scene primitives using {MaterialType}.");
    }

    private async Task<SampledTexture> LoadEnvironmentAsync(string path, uint faces)
    {
        byte[] data = await _ResourcesProvider.LoadBytesAsync(path);
        var texture = SampledTexture.FromKtx(_device, data, faces);
        _textures.Add(texture);
        return texture;
    }

    private GltfModel.TextureData[] MaterialTextures(GltfModel.MaterialData material)
    {
        GltfModel.TextureData[] common = [material.BaseColorTexture, material.MetallicRoughnessTexture,
            material.NormalTexture, material.OcclusionTexture, material.EmissiveTexture];
        return MaterialType switch
        {
            GLTFMaterialType.Clearcoat => [..common, material.Clearcoat.Texture, material.Clearcoat.RoughnessTexture, material.Clearcoat.NormalTexture],
            GLTFMaterialType.Sheen => [..common, material.Sheen.ColorTexture, material.Sheen.RoughnessTexture],
            GLTFMaterialType.Specular => [..common, material.Sheen.ColorTexture, material.Sheen.RoughnessTexture,
                material.Specular.Texture, material.Specular.ColorTexture],
            GLTFMaterialType.Transmission => [..common, material.Transmission.Texture, material.Transmission.ThicknessTexture],
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private unsafe void CreateMesh(GltfModel.SceneMesh mesh, List<SampledTexture> environment)
    {
        var model = mesh.Model;
        var draw = new MeshDraw { IndexCount = (uint)model.Indices.Length, Transform = mesh.Transform,
            Transmissive = CaptureOpaqueBackground && model.Material.Transmission.Factor > 0 };
        _draws.Add(draw);
        var textures = new List<SampledTexture>();
        var material = model.Material;
        var sources = MaterialTextures(material);
        for (int i = 0; i < sources.Length; i++)
        {
            var texture = SampledTexture.FromImage(_device, sources[i], i is 0 or 4 || (HasSheen && i == 5) || (MaterialType == GLTFMaterialType.Specular && i == 8),
                i == 2 || (MaterialType == GLTFMaterialType.Clearcoat && i == 7)
                    ? new Rgba32(128, 128, 255, 255) : new Rgba32(255, 255, 255, 255), _mipmaps);
            textures.Add(texture);
            _textures.Add(texture);
        }
        {
            WGPUVertexAttribute* attributes = stackalloc WGPUVertexAttribute[]
            {
                new() { shaderLocation = 0, format = WGPUVertexFormat.Float32X3, offset = 0 },
                new() { shaderLocation = 1, format = WGPUVertexFormat.Float32X3, offset = 12 },
                new() { shaderLocation = 2, format = WGPUVertexFormat.Float32X4, offset = 24 },
                new() { shaderLocation = 3, format = WGPUVertexFormat.Float32X2, offset = 40 },
                new() { shaderLocation = 4, format = WGPUVertexFormat.Float32X2, offset = 48 },
            };
            var depthState = new WGPUDepthStencilState
            {
                format = DepthFormat,
                depthWriteEnabled = material.AlphaMode == 2 ? WGPUOptionalBool.False : WGPUOptionalBool.True,
                depthCompare = WGPUCompareFunction.Less,
            };
            draw.Pipeline = _device.CreateRenderPipeline(new GPURenderPipelineDescriptor
            {
                Layout = GPUPipelineLayout.Auto,
                Vertex = new GPUVertexState
                {
                    Module = _shader,
                    EntryPoint = "vs_main",
                    Buffers = [new WGPUVertexBufferLayout
                    {
                        attributes = attributes,
                        attributeCount = 5,
                        arrayStride = (ulong)sizeof(GltfModel.Vertex),
                        stepMode = WGPUVertexStepMode.Vertex,
                    }],
                },
                Fragment = new GPUFragmentState
                {
                    Module = _shader,
                    EntryPoint = CaptureOpaqueBackground ? (draw.Transmissive ? "fs_transmission" : "fs_opaque") : "fs_main",
                    Targets = [new GPUColorTargetState
                    {
                        Format = CaptureOpaqueBackground && !draw.Transmissive ? WGPUTextureFormat.RGBA16Float : SurfaceFormat(),
                        Blend = material.AlphaMode == 2 ? GPUBlendState.Transparency : GPUBlendState.Opaque,
                        WriteMask = WGPUColorWriteMask.All,
                    }],
                },
                Primitive = new GPUPrimitiveState
                {
                    Topology = WGPUPrimitiveTopology.TriangleList,
                    FrontFace = WGPUFrontFace.CCW,
                    CullMode = material.DoubleSided ? WGPUCullMode.None : WGPUCullMode.Back,
                },
                DepthStencil = ref depthState,
            });
            draw.VertexBuffer = _device.CreateBuffer(new GPUBufferDescriptor
            {
                Label = $"{ExampleName}: vertices",
                Size = (ulong)model.Vertices.Length * (ulong)sizeof(GltfModel.Vertex),
                Usage = WGPUBufferUsage.Vertex,
                MappedAtCreation = true,
            });
            model.Vertices.AsSpan().CopyTo(draw.VertexBuffer.GetMappedRange<GltfModel.Vertex>(0, model.Vertices.Length));
            draw.VertexBuffer.Unmap();
            draw.IndexBuffer = _device.CreateBuffer(new GPUBufferDescriptor
            {
                Label = $"{ExampleName}: indices",
                Size = (ulong)model.Indices.Length * sizeof(uint),
                Usage = WGPUBufferUsage.Index,
                MappedAtCreation = true,
            });
            model.Indices.AsSpan().CopyTo(draw.IndexBuffer.GetMappedRange<uint>(0, model.Indices.Length));
            draw.IndexBuffer.Unmap();
            draw.PerFrameBuffer = _device.CreateBuffer(new GPUBufferDescriptor
            {
                Label = $"{ExampleName}: frame",
                Size = (ulong)sizeof(PerFrameData),
                Usage = WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst,
            });
            draw.MaterialBuffer = CreateMaterialBuffer(material);
            if (HasUVTransforms)
            {
                draw.UvBuffer = _device.CreateBuffer(new GPUBufferDescriptor
                {
                    Label = $"{ExampleName}: per-texture UV transforms",
                    Size = (ulong)(sources.Length * sizeof(TextureTransform)),
                    Usage = WGPUBufferUsage.Uniform,
                    MappedAtCreation = true,
                });
                var transforms = draw.UvBuffer.GetMappedRange<TextureTransform>(0, sources.Length);
                for (int i = 0; i < sources.Length; i++)
                {
                    var t = sources[i].Transform;
                    transforms[i] = new TextureTransform
                    {
                        U = new Vector4(t.M11, t.M21, t.M31, 0),
                        V = new Vector4(t.M12, t.M22, t.M32, 0),
                    };
                }
                draw.UvBuffer.Unmap();
            }
            using var frameLayout = draw.Pipeline.GetBindGroupLayout(0);
            var frameEntries = new List<WGPUBindGroupEntry>
            {
                new() { binding = 0, buffer = draw.PerFrameBuffer, size = draw.PerFrameBuffer.Size },
                new() { binding = 1, buffer = draw.MaterialBuffer, size = draw.MaterialBuffer.Size },
            };
            if (HasUVTransforms)
                frameEntries.Add(new WGPUBindGroupEntry { binding = 2, buffer = draw.UvBuffer, size = draw.UvBuffer.Size });
            draw.BindGroup = _device.CreateBindGroup(new GPUBindGroupDescriptor { Layout = frameLayout, Entries = frameEntries.ToArray() });
            Span<WGPUBindGroupEntry> entries = stackalloc WGPUBindGroupEntry[sources.Length * 2];
            for (int i = 0; i < sources.Length; i++)
            {
                entries[i] = new WGPUBindGroupEntry { binding = (uint)i, textureView = textures[i].View };
                entries[i + sources.Length] = new WGPUBindGroupEntry { binding = (uint)(i + sources.Length), sampler = textures[i].Sampler };
            }
            using var materialLayout = draw.Pipeline.GetBindGroupLayout(1);
            draw.MaterialBindGroup = _device.CreateBindGroup(new GPUBindGroupDescriptor { Layout = materialLayout, Entries = entries });
            using var environmentLayout = draw.Pipeline.GetBindGroupLayout(2);
            var environmentEntries = new List<WGPUBindGroupEntry>
            {
                new() { binding = 0, textureView = environment[0].View },
                new() { binding = 1, textureView = environment[1].View },
                new() { binding = 2, textureView = environment[2].View },
                new() { binding = 3, sampler = environment[0].Sampler },
            };
            if (HasSheen)
                environmentEntries.Add(new WGPUBindGroupEntry { binding = 4, textureView = environment[3].View });
            draw.EnvironmentBindGroup = _device.CreateBindGroup(new GPUBindGroupDescriptor
            {
                Layout = environmentLayout, Entries = environmentEntries.ToArray(),
            });
        }
    }

    private GPUBuffer CreateMaterialBuffer(GltfModel.MaterialData material)
    {
        var factors = material.MetallicRoughnessNormalOcclusion;
        if (material.NormalTexture.Image == null) factors.Z = 0;
        var header = new MaterialHeader
        {
            BaseColor = material.BaseColorFactor, MetallicRoughnessNormalOcclusion = factors,
            EmissiveAlphaCutoff = material.EmissiveAlphaCutoff,
        };
        var coordinates = new MaterialCoordinates
        {
            BaseColorUV = (uint)material.BaseColorTexture.TexCoord,
            MetallicRoughnessUV = (uint)material.MetallicRoughnessTexture.TexCoord,
            NormalUV = (uint)material.NormalTexture.TexCoord,
            OcclusionUV = (uint)material.OcclusionTexture.TexCoord,
            EmissiveUV = (uint)material.EmissiveTexture.TexCoord,
            AlphaMode = material.AlphaMode, TargetSrgb = SurfaceFormat().IsSRGB() ? 1u : 0u,
        };
        Vector4 effect;
        switch (MaterialType)
        {
            case GLTFMaterialType.Clearcoat:
                effect = new Vector4(material.Clearcoat.Factor, material.Clearcoat.Roughness,
                    material.Clearcoat.NormalScale, material.Clearcoat.NormalTexture.Image != null ? 1 : 0);
                coordinates.EffectUV0 = (uint)material.Clearcoat.Texture.TexCoord;
                coordinates.EffectUV1 = (uint)material.Clearcoat.RoughnessTexture.TexCoord;
                coordinates.EffectUV2 = (uint)material.Clearcoat.NormalTexture.TexCoord;
                break;
            case GLTFMaterialType.Sheen:
                effect = new Vector4(material.Sheen.Color, material.Sheen.Roughness);
                coordinates.EffectUV0 = (uint)material.Sheen.ColorTexture.TexCoord;
                coordinates.EffectUV1 = (uint)material.Sheen.RoughnessTexture.TexCoord;
                break;
            case GLTFMaterialType.Specular:
                coordinates.EffectUV0 = (uint)material.Sheen.ColorTexture.TexCoord;
                coordinates.EffectUV1 = (uint)material.Sheen.RoughnessTexture.TexCoord;
                coordinates.EffectUV2 = (uint)material.Specular.Texture.TexCoord;
                coordinates.EffectUV3 = (uint)material.Specular.ColorTexture.TexCoord;
                return CreateUniformBuffer(new SpecularMaterialData
                {
                    Header = header, Coordinates = coordinates,
                    Sheen = new Vector4(material.Sheen.Color, material.Sheen.Roughness),
                    Specular = new Vector4(material.Specular.Color, material.Specular.Factor),
                    Optics = new Vector4(material.Transmission.Ior, 0, 0, 0),
                });
            case GLTFMaterialType.Transmission:
                effect = new Vector4(material.Transmission.Factor, material.Transmission.Ior, material.Transmission.Thickness,
                    float.IsPositiveInfinity(material.Transmission.AttenuationDistance) ? 0 : material.Transmission.AttenuationDistance);
                coordinates.EffectUV0 = (uint)material.Transmission.Texture.TexCoord;
                coordinates.EffectUV1 = (uint)material.Transmission.ThicknessTexture.TexCoord;
                return CreateUniformBuffer(new TransmissionMaterialData
                {
                    Header = header, Effect = effect, Coordinates = coordinates,
                    AttenuationColor = new Vector4(material.Transmission.AttenuationColor, 0),
                });
            default:
                throw new ArgumentOutOfRangeException();
        }
        return CreateUniformBuffer(new SurfaceMaterialData { Header = header, Effect = effect, Coordinates = coordinates });
    }

    private unsafe GPUBuffer CreateUniformBuffer<T>(T data) where T : unmanaged
    {
        var buffer = _device.CreateBuffer(new GPUBufferDescriptor
        {
            Label = $"{ExampleName}: material", Size = (ulong)sizeof(T), Usage = WGPUBufferUsage.Uniform, MappedAtCreation = true,
        });
        buffer.GetMappedRange<T>()[0] = data;
        buffer.Unmap();
        return buffer;
    }

    private WGPUTextureFormat SurfaceFormat() =>
        _SurfaceCapabilities.Formats.FirstOrDefault(format => !format.IsSRGB(), _SurfaceCapabilities.Formats[0]);

    private void CreateSwapchain()
    {
        var (width, height) = GetFramebufferSizeInPixel();
        if (width <= 0 || height <= 0)
            return;

        _Surface.Configure(new GPUSurfaceConfiguration
        {
            Device = _device,
            Usage = WGPUTextureUsage.RenderAttachment,
            Format = SurfaceFormat(),
            PresentMode = WGPUPresentMode.Fifo,
            AlphaMode = _SurfaceCapabilities.AlphaModes[0],
            Width = (uint)width,
            Height = (uint)height,
        });

        _depthTextureView?.Dispose();
        _depthTexture?.Dispose();
        _depthTexture = _device.CreateTexture(new GPUTextureDescriptor
        {
            Label = $"{ExampleName}: depth",
            Size = new WGPUExtent3D { width = (uint)width, height = (uint)height, depthOrArrayLayers = 1 },
            Dimension = WGPUTextureDimension.D2D,
            Format = DepthFormat,
            Usage = WGPUTextureUsage.RenderAttachment,
        });
        _depthTextureView = _depthTexture.CreateView();
    }

    public override unsafe void WindowOnRender(double delta)
    {
        var (width, height) = GetFramebufferSizeInPixel();
        if (width <= 0 || height <= 0 || _depthTexture == null)
            return;

        var surfaceTexture = _Surface.GetCurrentTexture();
        switch (surfaceTexture.status)
        {
            case WGPUSurfaceGetCurrentTextureStatus.SuccessOptimal:
            case WGPUSurfaceGetCurrentTextureStatus.SuccessSuboptimal:
                break;
            case WGPUSurfaceGetCurrentTextureStatus.Outdated:
            case WGPUSurfaceGetCurrentTextureStatus.Lost:
                if (surfaceTexture.texture != WGPUTexture.Null)
                    wgpuTextureRelease(surfaceTexture.texture);
                CreateSwapchain();
                return;
            default:
                if (surfaceTexture.texture != WGPUTexture.Null)
                    wgpuTextureRelease(surfaceTexture.texture);
                return;
        }

        using var texture = new GPUTexture(surfaceTexture.texture, null);
        using var view = texture.CreateView();
        RenderFrame(view, _depthTextureView!, (uint)width, (uint)height, (float)_timer.Elapsed.TotalSeconds);
        _Surface.Present();
        _Window.SwapBuffers();
    }

    protected unsafe void RenderFrame(GPUTextureView view, GPUTextureView depth, uint width, uint height, float time)
    {
        var rotation = ModelTransform(time);
        var camera = CameraPosition;
        var cameraView = Matrix4x4.CreateLookAt(camera, CameraTarget, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4, (float)width / height, NearPlane, FarPlane);
        foreach (var draw in _draws)
        {
            var model = draw.Transform * rotation;
            Matrix4x4.Invert(model, out var inverseModel);
            var perFrame = new PerFrameData
            {
                Model = model, Normal = Matrix4x4.Transpose(inverseModel),
                ViewProjection = cameraView * projection, CameraPosition = new Vector4(camera, 1),
            };
            _device.Queue.WriteBuffer(draw.PerFrameBuffer, 0, &perFrame, (nuint)sizeof(PerFrameData));
        }
        if (!CaptureOpaqueBackground)
        {
            RenderScene(view, depth);
            return;
        }
        EnsureBackground(width, height);
        // Capture only opaque objects. The sampled texture is never a render attachment in the same pass.
        using (var encoder = _device.CreateCommandEncoder())
        {
            var depthAttachment = new WGPURenderPassDepthStencilAttachment
            {
                view = depth, depthLoadOp = WGPULoadOp.Clear, depthStoreOp = WGPUStoreOp.Store, depthClearValue = 1,
            };
            using var pass = encoder.BeginRenderPass(new GPURenderPassDescriptor
            {
                ColorAttachments = [new WGPURenderPassColorAttachment
                {
                    view = _backgroundAttachment!, loadOp = WGPULoadOp.Clear, storeOp = WGPUStoreOp.Store,
                    clearValue = new WGPUColor { r = 1, g = 1, b = 1, a = 1 }, depthSlice = DepthSliceUndefined,
                }],
                DepthStencilAttachment = ref depthAttachment,
            });
            foreach (var draw in _draws.Where(d => !d.Transmissive)) DrawMesh(pass, draw);
            pass.End();
            using var commands = encoder.Finish();
            _device.Queue.Submit(commands);
        }
        _mipmaps.Generate(_background!);
        using (var encoder = _device.CreateCommandEncoder())
        {
            // Convert the linear HDR opaque capture to the output surface exactly once.
            using (var pass = encoder.BeginRenderPass(new GPURenderPassDescriptor
            {
                ColorAttachments = [new WGPURenderPassColorAttachment
                {
                    view = view, loadOp = WGPULoadOp.Clear, storeOp = WGPUStoreOp.Store, depthSlice = DepthSliceUndefined,
                }],
            }))
            {
                pass.SetPipeline(_copyPipeline); pass.SetBindGroup(0, _copyGroup!); pass.Draw(3); pass.End();
            }
            var depthAttachment = new WGPURenderPassDepthStencilAttachment
            {
                // Emscripten serializes this field even for Load; keep it finite for WebIDL.
                view = depth, depthLoadOp = WGPULoadOp.Load, depthStoreOp = WGPUStoreOp.Store, depthClearValue = 1,
            };
            using (var pass = encoder.BeginRenderPass(new GPURenderPassDescriptor
            {
                ColorAttachments = [new WGPURenderPassColorAttachment
                {
                    view = view, loadOp = WGPULoadOp.Load, storeOp = WGPUStoreOp.Store, depthSlice = DepthSliceUndefined,
                }],
                DepthStencilAttachment = ref depthAttachment,
            }))
            {
                foreach (var draw in _draws.Where(d => d.Transmissive)) DrawMesh(pass, draw);
                pass.End();
            }
            using var commands = encoder.Finish();
            _device.Queue.Submit(commands);
        }
    }

    private unsafe void RenderScene(GPUTextureView view, GPUTextureView depth)
    {
        using var encoder = _device.CreateCommandEncoder();
        var depthAttachment = new WGPURenderPassDepthStencilAttachment
        {
            view = depth, depthLoadOp = WGPULoadOp.Clear, depthStoreOp = WGPUStoreOp.Store, depthClearValue = 1,
        };
        using var pass = encoder.BeginRenderPass(new GPURenderPassDescriptor
        {
            ColorAttachments = [new WGPURenderPassColorAttachment
            {
                view = view, loadOp = WGPULoadOp.Clear, storeOp = WGPUStoreOp.Store,
                clearValue = new WGPUColor { r = 1, g = 1, b = 1, a = 1 }, depthSlice = DepthSliceUndefined,
            }],
            DepthStencilAttachment = ref depthAttachment,
        });
        foreach (var draw in _draws) DrawMesh(pass, draw);
        pass.End();
        using var commands = encoder.Finish();
        _device.Queue.Submit(commands);
    }

    private static void DrawMesh(GPURenderPassEncoder pass, MeshDraw draw)
    {
        pass.SetPipeline(draw.Pipeline);
        pass.SetBindGroup(0, draw.BindGroup); pass.SetBindGroup(1, draw.MaterialBindGroup); pass.SetBindGroup(2, draw.EnvironmentBindGroup);
        if (draw.Transmissive) pass.SetBindGroup(3, draw.BackgroundBindGroup!);
        pass.SetVertexBuffer(0, draw.VertexBuffer); pass.SetIndexBuffer(draw.IndexBuffer, WGPUIndexFormat.Uint32);
        pass.DrawIndexed(draw.IndexCount);
    }

    private void EnsureBackground(uint width, uint height)
    {
        if (_background != null && _background.Width == width && _background.Height == height) return;
        _copyGroup?.Dispose();
        foreach (var draw in _draws) { draw.BackgroundBindGroup?.Dispose(); draw.BackgroundBindGroup = null; }
        _backgroundView?.Dispose(); _backgroundAttachment?.Dispose(); _background?.Dispose();
        _background = _device.CreateTexture(new GPUTextureDescriptor
        {
            Label = $"{ExampleName}: linear opaque scene with mip chain",
            Format = WGPUTextureFormat.RGBA16Float,
            Size = new WGPUExtent3D { width = width, height = height, depthOrArrayLayers = 1 },
            MipLevelCount = (uint)BitOperations.Log2(Math.Max(width, height)) + 1,
            Usage = WGPUTextureUsage.TextureBinding | WGPUTextureUsage.RenderAttachment,
        });
        _backgroundView = _background.CreateView();
        _backgroundAttachment = _background.CreateView(new GPUTextureViewDescriptor
        {
            Dimension = WGPUTextureViewDimension.D2D, MipLevelCount = 1, ArrayLayerCount = 1,
            Usage = WGPUTextureUsage.RenderAttachment,
        });
        using (var layout = _copyPipeline.GetBindGroupLayout(0))
            _copyGroup = BackgroundGroup(layout);
        foreach (var draw in _draws.Where(d => d.Transmissive))
        {
            using var layout = draw.Pipeline.GetBindGroupLayout(3);
            draw.BackgroundBindGroup = BackgroundGroup(layout);
        }
    }

    private GPUBindGroup BackgroundGroup(GPUBindGroupLayout layout) => _device.CreateBindGroup(new GPUBindGroupDescriptor
    {
        Layout = layout,
        Entries = [new WGPUBindGroupEntry { binding = 0, textureView = _backgroundView! },
            new WGPUBindGroupEntry { binding = 1, sampler = _backgroundSampler }],
    });

    public override void FramebufferResize(Vector2D<int> size) => CreateSwapchain();

    public override void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        foreach (var draw in _draws)
        {
            draw.BindGroup?.Dispose();
            draw.MaterialBindGroup?.Dispose();
            draw.EnvironmentBindGroup?.Dispose();
            draw.Pipeline?.Dispose();
            draw.VertexBuffer?.Dispose();
            draw.IndexBuffer?.Dispose();
            draw.PerFrameBuffer?.Dispose();
            draw.MaterialBuffer?.Dispose();
            draw.UvBuffer?.Dispose();
            draw.BackgroundBindGroup?.Dispose();
        }
        _draws.Clear();
        _shader?.Dispose();
        _copyGroup?.Dispose(); _copyPipeline?.Dispose(); _copyShader?.Dispose(); _backgroundSampler?.Dispose();
        _backgroundView?.Dispose(); _backgroundAttachment?.Dispose(); _background?.Dispose(); _mipmaps?.Dispose();
        foreach (var texture in _textures) texture.Dispose();
        _textures.Clear();
        _depthTextureView?.Dispose();
        _depthTexture?.Dispose();
    }
}
