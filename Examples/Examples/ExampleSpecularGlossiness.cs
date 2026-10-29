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

// Chapter06/05_SpecularGlossiness: compare both PBR workflows in the same glTF scene.
public class ExampleSpecularGlossiness : ExampleBase
{
    private const WGPUTextureFormat DepthFormat = WGPUTextureFormat.Depth32Float;

    private GPUShaderModule _shader = null!;
    private sealed class MeshDraw
    {
        public GPURenderPipeline Pipeline = null!;
        public GPUBuffer VertexBuffer = null!, IndexBuffer = null!, MaterialBuffer = null!, PerFrameBuffer = null!;
        public GPUBindGroup BindGroup = null!, MaterialBindGroup = null!, EnvironmentBindGroup = null!;
        public uint IndexCount;
        public Matrix4x4 Transform;
    }
    private readonly List<MeshDraw> _draws = [];
    private readonly List<SampledTexture> _textures = [];
    private GPUTexture? _depthTexture;
    private GPUTextureView? _depthTextureView;
    private readonly Stopwatch _timer = new();

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
        public Vector4 SpecularGlossiness;
        public uint BaseColorUV, MetallicRoughnessUV, NormalUV, OcclusionUV;
        public uint EmissiveUV, AlphaMode, TargetSrgb, Workflow;
    }

    public ExampleSpecularGlossiness(GPU instance, GPUDevice device, IWebGpuView window,
        GPUSurfaceCapabilities surfaceCapabilities, GPUSurface surface, IResourcesProvider resourcesProvider)
        : base(instance, device, window, surfaceCapabilities, surface, resourcesProvider)
    {
    }

    public override async Task OnLoad()
    {
        var scene = await GltfModel.LoadSceneAsync(_ResourcesProvider, "Models/SpecGlossVsMetalRough/SpecGlossVsMetalRough.gltf");
        string code = await _ResourcesProvider.LoadTextAsync("Shaders/example_specular_glossiness.wgsl");
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Specular/glossiness shader source must not be empty.");
        _shader = _device.CreateShaderModule(new GPUShaderModuleDescriptor { Code = code });
        using var mipmaps = new MipmapGenerator(_device, await _ResourcesProvider.LoadTextAsync("Shaders/generate_mipmaps.wgsl"));
        var environment = new[]
        {
            SampledTexture.FromKtx(_device, await _ResourcesProvider.LoadBytesAsync("Textures/piazza_bologni_1k_prefilter.ktx"), 6),
            SampledTexture.FromKtx(_device, await _ResourcesProvider.LoadBytesAsync("Textures/piazza_bologni_1k_irradiance.ktx"), 6),
            SampledTexture.FromKtx(_device, await _ResourcesProvider.LoadBytesAsync("Textures/brdfLUT.ktx"), 1),
        };
        _textures.AddRange(environment);
        foreach (var mesh in scene) CreateMesh(mesh, mipmaps, environment);
        CreateSwapchain();
        _timer.Restart();
        Console.WriteLine($"Specular/glossiness: loaded {scene.Length} scene primitives with both material workflows.");
    }

    private unsafe void CreateMesh(GltfModel.SceneMesh mesh, MipmapGenerator mipmaps, SampledTexture[] environment)
    {
        var model = mesh.Model;
        var draw = new MeshDraw { IndexCount = (uint)model.Indices.Length, Transform = mesh.Transform };
        _draws.Add(draw);
        var textures = new List<SampledTexture>();
        var material = model.Material;
        textures.Add(SampledTexture.FromImage(_device, material.BaseColorTexture, true, new Rgba32(255, 255, 255, 255), mipmaps));
        textures.Add(SampledTexture.FromImage(_device, material.MetallicRoughnessTexture, material.SpecularGlossiness, new Rgba32(255, 255, 255, 255), mipmaps));
        textures.Add(SampledTexture.FromImage(_device, material.NormalTexture, false, new Rgba32(128, 128, 255, 255), mipmaps));
        textures.Add(SampledTexture.FromImage(_device, material.OcclusionTexture, false, new Rgba32(255, 255, 255, 255), mipmaps));
        textures.Add(SampledTexture.FromImage(_device, material.EmissiveTexture, true, new Rgba32(255, 255, 255, 255), mipmaps));
        _textures.AddRange(textures);
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
                    EntryPoint = "fs_main",
                    Targets = [new GPUColorTargetState
                    {
                        Format = SurfaceFormat(),
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
                Label = "Specular/glossiness: vertices",
                Size = (ulong)model.Vertices.Length * (ulong)sizeof(GltfModel.Vertex),
                Usage = WGPUBufferUsage.Vertex,
                MappedAtCreation = true,
            });
            model.Vertices.AsSpan().CopyTo(draw.VertexBuffer.GetMappedRange<GltfModel.Vertex>(0, model.Vertices.Length));
            draw.VertexBuffer.Unmap();
            draw.IndexBuffer = _device.CreateBuffer(new GPUBufferDescriptor
            {
                Label = "Specular/glossiness: indices",
                Size = (ulong)model.Indices.Length * sizeof(uint),
                Usage = WGPUBufferUsage.Index,
                MappedAtCreation = true,
            });
            model.Indices.AsSpan().CopyTo(draw.IndexBuffer.GetMappedRange<uint>(0, model.Indices.Length));
            draw.IndexBuffer.Unmap();
            draw.PerFrameBuffer = _device.CreateBuffer(new GPUBufferDescriptor
            {
                Label = "Specular/glossiness: frame",
                Size = (ulong)sizeof(PerFrameData),
                Usage = WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst,
            });
            draw.MaterialBuffer = _device.CreateBuffer(new GPUBufferDescriptor
            {
                Label = "Specular/glossiness: material",
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
                SpecularGlossiness = material.SpecularGlossinessFactor,
                Workflow = material.SpecularGlossiness ? 1u : 0u,
                BaseColorUV = (uint)material.BaseColorTexture.TexCoord,
                MetallicRoughnessUV = (uint)material.MetallicRoughnessTexture.TexCoord,
                NormalUV = (uint)material.NormalTexture.TexCoord,
                OcclusionUV = (uint)material.OcclusionTexture.TexCoord,
                EmissiveUV = (uint)material.EmissiveTexture.TexCoord,
                AlphaMode = material.AlphaMode,
                TargetSrgb = SurfaceFormat().IsSRGB() ? 1u : 0u,
            };
            draw.MaterialBuffer.Unmap();
            using var frameLayout = draw.Pipeline.GetBindGroupLayout(0);
            draw.BindGroup = _device.CreateBindGroup(new GPUBindGroupDescriptor
            {
                Layout = frameLayout,
                Entries =
                [
                    new WGPUBindGroupEntry { binding = 0, buffer = draw.PerFrameBuffer, size = draw.PerFrameBuffer.Size },
                    new WGPUBindGroupEntry { binding = 1, buffer = draw.MaterialBuffer, size = draw.MaterialBuffer.Size },
                ],
            });
            Span<WGPUBindGroupEntry> entries = stackalloc WGPUBindGroupEntry[10];
            for (int i = 0; i < 5; i++)
            {
                entries[i] = new WGPUBindGroupEntry { binding = (uint)i, textureView = textures[i].View };
                entries[i + 5] = new WGPUBindGroupEntry { binding = (uint)(i + 5), sampler = textures[i].Sampler };
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
            Label = "Specular/glossiness: depth",
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
        var rotation = Matrix4x4.CreateRotationY(time * 0.3f);
        var camera = new Vector3(0, 0.08f, 0.6f);
        var cameraView = Matrix4x4.CreateLookAt(camera, new Vector3(0, 0.08f, 0), Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4, (float)width / height, 0.01f, 100);
        using var encoder = _device.CreateCommandEncoder();
        var depthAttachment = new WGPURenderPassDepthStencilAttachment
        {
            view = depth,
            depthLoadOp = WGPULoadOp.Clear,
            depthStoreOp = WGPUStoreOp.Store,
            depthClearValue = 1,
        };
        using var pass = encoder.BeginRenderPass(new GPURenderPassDescriptor
        {
            ColorAttachments = [new WGPURenderPassColorAttachment
            {
                view = view,
                loadOp = WGPULoadOp.Clear,
                storeOp = WGPUStoreOp.Store,
                clearValue = new WGPUColor { r = 1, g = 1, b = 1, a = 1 },
                depthSlice = DepthSliceUndefined,
            }],
            DepthStencilAttachment = ref depthAttachment,
        });
        foreach (var draw in _draws)
        {
            var model = draw.Transform * rotation;
            Matrix4x4.Invert(model, out var inverseModel);
            var perFrame = new PerFrameData
            {
                Model = model,
                Normal = Matrix4x4.Transpose(inverseModel),
                ViewProjection = cameraView * projection,
                CameraPosition = new Vector4(camera, 1),
            };
            _device.Queue.WriteBuffer(draw.PerFrameBuffer, 0, &perFrame, (nuint)sizeof(PerFrameData));
            pass.SetPipeline(draw.Pipeline);
            pass.SetBindGroup(0, draw.BindGroup);
            pass.SetBindGroup(1, draw.MaterialBindGroup);
            pass.SetBindGroup(2, draw.EnvironmentBindGroup);
            pass.SetVertexBuffer(0, draw.VertexBuffer);
            pass.SetIndexBuffer(draw.IndexBuffer, WGPUIndexFormat.Uint32);
            pass.DrawIndexed(draw.IndexCount);
        }
        pass.End();
        using var commands = encoder.Finish();
        _device.Queue.Submit(commands);
    }

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
        }
        _draws.Clear();
        _shader?.Dispose();
        foreach (var texture in _textures) texture.Dispose();
        _textures.Clear();
        _depthTextureView?.Dispose();
        _depthTexture?.Dispose();
    }
}
