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

// Chapter07/03_Transmission: opaque background capture followed by rough transmission.
public class ExampleTransmission : ExampleBase
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

    [StructLayout(LayoutKind.Sequential)]
    private struct MaterialData
    {
        public Vector4 BaseColor;
        public Vector4 MetallicRoughnessNormalOcclusion;
        public Vector4 EmissiveAlphaCutoff;
        public Vector4 Transmission; // factor, IOR, thickness, attenuation distance (0 = infinity)
        public Vector4 AttenuationColor;
        public uint BaseColorUV, MetallicRoughnessUV, NormalUV, OcclusionUV;
        public uint EmissiveUV, AlphaMode, TargetSrgb, Padding;
        public uint TransmissionUV, ThicknessUV, TransmissionPadding0, TransmissionPadding1;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TextureTransform
    {
        public Vector4 U, V;
    }

    public ExampleTransmission(GPU instance, GPUDevice device, IWebGpuView window,
        GPUSurfaceCapabilities surfaceCapabilities, GPUSurface surface, IResourcesProvider resourcesProvider)
        : base(instance, device, window, surfaceCapabilities, surface, resourcesProvider)
    {
    }

    public override async Task OnLoad()
    {
        var scene = await GltfModel.LoadSceneAsync(_ResourcesProvider, "Models/TransmissionRoughnessTest/TransmissionRoughnessTest.gltf");
        string code = await _ResourcesProvider.LoadTextAsync("Shaders/example_transmission.wgsl");
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Transmission shader source must not be empty.");
        _shader = _device.CreateShaderModule(new GPUShaderModuleDescriptor { Code = code });
        _mipmaps = new MipmapGenerator(_device, await _ResourcesProvider.LoadTextAsync("Shaders/generate_mipmaps.wgsl"));
        var environment = new[]
        {
            SampledTexture.FromKtx(_device, await _ResourcesProvider.LoadBytesAsync("Textures/piazza_bologni_1k_prefilter.ktx"), 6),
            SampledTexture.FromKtx(_device, await _ResourcesProvider.LoadBytesAsync("Textures/piazza_bologni_1k_irradiance.ktx"), 6),
            SampledTexture.FromKtx(_device, await _ResourcesProvider.LoadBytesAsync("Textures/brdfLUT.ktx"), 1),
        };
        _textures.AddRange(environment);
        foreach (var mesh in scene) CreateMesh(mesh, _mipmaps, environment);
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
        CreateSwapchain();
        _timer.Restart();
        Console.WriteLine($"Transmission: loaded {scene.Length} scene primitives with screen-space transmission.");
    }

    private unsafe void CreateMesh(GltfModel.SceneMesh mesh, MipmapGenerator mipmaps, SampledTexture[] environment)
    {
        var model = mesh.Model;
        var draw = new MeshDraw { IndexCount = (uint)model.Indices.Length, Transform = mesh.Transform,
            Transmissive = model.Material.Transmission.Factor > 0 };
        _draws.Add(draw);
        var textures = new List<SampledTexture>();
        var material = model.Material;
        GltfModel.TextureData[] sources = [material.BaseColorTexture, material.MetallicRoughnessTexture,
            material.NormalTexture, material.OcclusionTexture, material.EmissiveTexture,
            material.Transmission.Texture, material.Transmission.ThicknessTexture];
        for (int i = 0; i < sources.Length; i++)
        {
            var texture = SampledTexture.FromImage(_device, sources[i], i is 0 or 4,
                i == 2 ? new Rgba32(128, 128, 255, 255) : new Rgba32(255, 255, 255, 255), mipmaps);
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
                    EntryPoint = draw.Transmissive ? "fs_transmission" : "fs_opaque",
                    Targets = [new GPUColorTargetState
                    {
                        Format = draw.Transmissive ? SurfaceFormat() : WGPUTextureFormat.RGBA16Float,
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
                Label = "Transmission: vertices",
                Size = (ulong)model.Vertices.Length * (ulong)sizeof(GltfModel.Vertex),
                Usage = WGPUBufferUsage.Vertex,
                MappedAtCreation = true,
            });
            model.Vertices.AsSpan().CopyTo(draw.VertexBuffer.GetMappedRange<GltfModel.Vertex>(0, model.Vertices.Length));
            draw.VertexBuffer.Unmap();
            draw.IndexBuffer = _device.CreateBuffer(new GPUBufferDescriptor
            {
                Label = "Transmission: indices",
                Size = (ulong)model.Indices.Length * sizeof(uint),
                Usage = WGPUBufferUsage.Index,
                MappedAtCreation = true,
            });
            model.Indices.AsSpan().CopyTo(draw.IndexBuffer.GetMappedRange<uint>(0, model.Indices.Length));
            draw.IndexBuffer.Unmap();
            draw.PerFrameBuffer = _device.CreateBuffer(new GPUBufferDescriptor
            {
                Label = "Transmission: frame",
                Size = (ulong)sizeof(PerFrameData),
                Usage = WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst,
            });
            draw.MaterialBuffer = _device.CreateBuffer(new GPUBufferDescriptor
            {
                Label = "Transmission: material",
                Size = (ulong)sizeof(MaterialData),
                Usage = WGPUBufferUsage.Uniform,
                MappedAtCreation = true,
            });
            var factors = material.MetallicRoughnessNormalOcclusion;
            if (material.NormalTexture.Image == null) factors.Z = 0;
            draw.MaterialBuffer.GetMappedRange<MaterialData>()[0] = new MaterialData
            {
                BaseColor = material.BaseColorFactor,
                MetallicRoughnessNormalOcclusion = factors,
                EmissiveAlphaCutoff = material.EmissiveAlphaCutoff,
                Transmission = new Vector4(material.Transmission.Factor, material.Transmission.Ior, material.Transmission.Thickness,
                    float.IsPositiveInfinity(material.Transmission.AttenuationDistance) ? 0 : material.Transmission.AttenuationDistance),
                AttenuationColor = new Vector4(material.Transmission.AttenuationColor, 0),
                TransmissionUV = (uint)material.Transmission.Texture.TexCoord,
                ThicknessUV = (uint)material.Transmission.ThicknessTexture.TexCoord,
                BaseColorUV = (uint)material.BaseColorTexture.TexCoord,
                MetallicRoughnessUV = (uint)material.MetallicRoughnessTexture.TexCoord,
                NormalUV = (uint)material.NormalTexture.TexCoord,
                OcclusionUV = (uint)material.OcclusionTexture.TexCoord,
                EmissiveUV = (uint)material.EmissiveTexture.TexCoord,
                AlphaMode = material.AlphaMode,
                TargetSrgb = SurfaceFormat().IsSRGB() ? 1u : 0u,
            };
            draw.MaterialBuffer.Unmap();
            draw.UvBuffer = _device.CreateBuffer(new GPUBufferDescriptor
            {
                Label = "Transmission: per-texture UV transforms",
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
            using var frameLayout = draw.Pipeline.GetBindGroupLayout(0);
            draw.BindGroup = _device.CreateBindGroup(new GPUBindGroupDescriptor
            {
                Layout = frameLayout,
                Entries =
                [
                    new WGPUBindGroupEntry { binding = 0, buffer = draw.PerFrameBuffer, size = draw.PerFrameBuffer.Size },
                    new WGPUBindGroupEntry { binding = 1, buffer = draw.MaterialBuffer, size = draw.MaterialBuffer.Size },
                    new WGPUBindGroupEntry { binding = 2, buffer = draw.UvBuffer, size = draw.UvBuffer.Size },
                ],
            });
            Span<WGPUBindGroupEntry> entries = stackalloc WGPUBindGroupEntry[14];
            for (int i = 0; i < 7; i++)
            {
                entries[i] = new WGPUBindGroupEntry { binding = (uint)i, textureView = textures[i].View };
                entries[i + 7] = new WGPUBindGroupEntry { binding = (uint)(i + 7), sampler = textures[i].Sampler };
            }
            using var materialLayout = draw.Pipeline.GetBindGroupLayout(1);
            draw.MaterialBindGroup = _device.CreateBindGroup(new GPUBindGroupDescriptor { Layout = materialLayout, Entries = entries });
            using var environmentLayout = draw.Pipeline.GetBindGroupLayout(2);
            draw.EnvironmentBindGroup = _device.CreateBindGroup(new GPUBindGroupDescriptor
            {
                Layout = environmentLayout,
                Entries =
                [
                    new WGPUBindGroupEntry { binding = 0, textureView = environment[0].View },
                    new WGPUBindGroupEntry { binding = 1, textureView = environment[1].View },
                    new WGPUBindGroupEntry { binding = 2, textureView = environment[2].View },
                    new WGPUBindGroupEntry { binding = 3, sampler = environment[0].Sampler },
                ],
            });
        }
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
            Label = "Transmission: depth",
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

    private unsafe void RenderFrame(GPUTextureView view, GPUTextureView depth, uint width, uint height, float time)
    {
        EnsureBackground(width, height);
        var rotation = Matrix4x4.CreateRotationY(MathF.PI);
        var camera = new Vector3(0, 0, -1.65f);
        var cameraView = Matrix4x4.CreateLookAt(camera, Vector3.Zero, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4, (float)width / height, 0.01f, 100);
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
            Label = "Transmission: linear opaque scene with mip chain",
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
