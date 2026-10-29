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

// Chapter06/04_MetallicRoughness: textured glTF PBR with precomputed environment lighting.
public class ExampleMetallicRoughness : ExampleBase
{
    private const WGPUTextureFormat DepthFormat = WGPUTextureFormat.Depth32Float;

    private GPUShaderModule _shader = null!;
    private GPURenderPipeline _pipeline = null!;
    private GPUBuffer _vertexBuffer = null!;
    private GPUBuffer _indexBuffer = null!;
    private uint _indexCount;
    private GPUBuffer _materialBuffer = null!;
    private GPUBindGroup _materialBindGroup = null!;
    private GPUBindGroup _environmentBindGroup = null!;
    private readonly List<SampledTexture> _textures = [];
    private GPUBuffer _perFrameBuffer = null!;
    private GPUBindGroup _bindGroup = null!;
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
        public uint BaseColorUV, MetallicRoughnessUV, NormalUV, OcclusionUV;
        public uint EmissiveUV, AlphaMode, TargetSrgb, Padding;
    }

    public ExampleMetallicRoughness(GPU instance, GPUDevice device, IWebGpuView window,
        GPUSurfaceCapabilities surfaceCapabilities, GPUSurface surface, IResourcesProvider resourcesProvider)
        : base(instance, device, window, surfaceCapabilities, surface, resourcesProvider)
    {
    }

    public override async Task OnLoad()
    {
        var model = await GltfModel.LoadAsync(_ResourcesProvider, "Models/DamagedHelmet/DamagedHelmet.gltf");
        _indexCount = (uint)model.Indices.Length;
        string code = await _ResourcesProvider.LoadTextAsync("Shaders/example_metallic_roughness.wgsl");
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Metallic/roughness shader source must not be empty.");
        _shader = _device.CreateShaderModule(new GPUShaderModuleDescriptor { Code = code });
        using var mipmaps = new MipmapGenerator(_device, await _ResourcesProvider.LoadTextAsync("Shaders/generate_mipmaps.wgsl"));
        var material = model.Material;
        _textures.Add(SampledTexture.FromImage(_device, material.BaseColorTexture, true, new Rgba32(255, 255, 255, 255), mipmaps));
        _textures.Add(SampledTexture.FromImage(_device, material.MetallicRoughnessTexture, false, new Rgba32(255, 255, 255, 255), mipmaps));
        _textures.Add(SampledTexture.FromImage(_device, material.NormalTexture, false, new Rgba32(128, 128, 255, 255), mipmaps));
        _textures.Add(SampledTexture.FromImage(_device, material.OcclusionTexture, false, new Rgba32(255, 255, 255, 255), mipmaps));
        _textures.Add(SampledTexture.FromImage(_device, material.EmissiveTexture, true, new Rgba32(255, 255, 255, 255), mipmaps));
        _textures.Add(SampledTexture.FromKtx(_device, await _ResourcesProvider.LoadBytesAsync("Textures/piazza_bologni_1k_prefilter.ktx"), 6));
        _textures.Add(SampledTexture.FromKtx(_device, await _ResourcesProvider.LoadBytesAsync("Textures/piazza_bologni_1k_irradiance.ktx"), 6));
        _textures.Add(SampledTexture.FromKtx(_device, await _ResourcesProvider.LoadBytesAsync("Textures/brdfLUT.ktx"), 1));
        unsafe
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
            _pipeline = _device.CreateRenderPipeline(new GPURenderPipelineDescriptor
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
            _vertexBuffer = _device.CreateBuffer(new GPUBufferDescriptor
            {
                Label = "Metallic/roughness: vertices",
                Size = (ulong)model.Vertices.Length * (ulong)sizeof(GltfModel.Vertex),
                Usage = WGPUBufferUsage.Vertex,
                MappedAtCreation = true,
            });
            model.Vertices.AsSpan().CopyTo(_vertexBuffer.GetMappedRange<GltfModel.Vertex>(0, model.Vertices.Length));
            _vertexBuffer.Unmap();
            _indexBuffer = _device.CreateBuffer(new GPUBufferDescriptor
            {
                Label = "Metallic/roughness: indices",
                Size = (ulong)model.Indices.Length * sizeof(uint),
                Usage = WGPUBufferUsage.Index,
                MappedAtCreation = true,
            });
            model.Indices.AsSpan().CopyTo(_indexBuffer.GetMappedRange<uint>(0, model.Indices.Length));
            _indexBuffer.Unmap();
            _perFrameBuffer = _device.CreateBuffer(new GPUBufferDescriptor
            {
                Label = "Metallic/roughness: frame",
                Size = (ulong)sizeof(PerFrameData),
                Usage = WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst,
            });
            _materialBuffer = _device.CreateBuffer(new GPUBufferDescriptor
            {
                Label = "Metallic/roughness: material",
                Size = (ulong)sizeof(MaterialData),
                Usage = WGPUBufferUsage.Uniform,
                MappedAtCreation = true,
            });
            var factors = material.MetallicRoughnessNormalOcclusion;
            if (material.NormalTexture.Image == null) factors.Z = 0;
            _materialBuffer.GetMappedRange<MaterialData>()[0] = new MaterialData
            {
                BaseColor = material.BaseColorFactor,
                MetallicRoughnessNormalOcclusion = factors,
                EmissiveAlphaCutoff = material.EmissiveAlphaCutoff,
                BaseColorUV = (uint)material.BaseColorTexture.TexCoord,
                MetallicRoughnessUV = (uint)material.MetallicRoughnessTexture.TexCoord,
                NormalUV = (uint)material.NormalTexture.TexCoord,
                OcclusionUV = (uint)material.OcclusionTexture.TexCoord,
                EmissiveUV = (uint)material.EmissiveTexture.TexCoord,
                AlphaMode = material.AlphaMode,
                TargetSrgb = SurfaceFormat().IsSRGB() ? 1u : 0u,
            };
            _materialBuffer.Unmap();
            using var frameLayout = _pipeline.GetBindGroupLayout(0);
            _bindGroup = _device.CreateBindGroup(new GPUBindGroupDescriptor
            {
                Layout = frameLayout,
                Entries =
                [
                    new WGPUBindGroupEntry { binding = 0, buffer = _perFrameBuffer, size = _perFrameBuffer.Size },
                    new WGPUBindGroupEntry { binding = 1, buffer = _materialBuffer, size = _materialBuffer.Size },
                ],
            });
            Span<WGPUBindGroupEntry> entries = stackalloc WGPUBindGroupEntry[10];
            for (int i = 0; i < 5; i++)
            {
                entries[i] = new WGPUBindGroupEntry { binding = (uint)i, textureView = _textures[i].View };
                entries[i + 5] = new WGPUBindGroupEntry { binding = (uint)(i + 5), sampler = _textures[i].Sampler };
            }
            using var materialLayout = _pipeline.GetBindGroupLayout(1);
            _materialBindGroup = _device.CreateBindGroup(new GPUBindGroupDescriptor { Layout = materialLayout, Entries = entries });
            using var environmentLayout = _pipeline.GetBindGroupLayout(2);
            _environmentBindGroup = _device.CreateBindGroup(new GPUBindGroupDescriptor
            {
                Layout = environmentLayout,
                Entries =
                [
                    new WGPUBindGroupEntry { binding = 0, textureView = _textures[5].View },
                    new WGPUBindGroupEntry { binding = 1, textureView = _textures[6].View },
                    new WGPUBindGroupEntry { binding = 2, textureView = _textures[7].View },
                    new WGPUBindGroupEntry { binding = 3, sampler = _textures[5].Sampler },
                ],
            });
        }
        CreateSwapchain();
        _timer.Restart();
        Console.WriteLine($"Metallic/roughness: loaded {model.Vertices.Length} vertices, {_indexCount} indices, 5 material maps and 3 IBL textures.");
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
            Label = "Metallic/roughness: depth",
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
        var model = Matrix4x4.CreateRotationX(MathF.PI / 2) * Matrix4x4.CreateRotationY(time);
        var camera = new Vector3(0, 0, 3.5f);
        var cameraView = Matrix4x4.CreateLookAt(camera, Vector3.Zero, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4, (float)width / height, 0.1f, 1000);
        Matrix4x4.Invert(model, out var inverseModel);
        var perFrame = new PerFrameData
        {
            Model = model,
            Normal = Matrix4x4.Transpose(inverseModel),
            ViewProjection = cameraView * projection,
            CameraPosition = new Vector4(camera, 1),
        };
        _device.Queue.WriteBuffer(_perFrameBuffer, 0, &perFrame, (nuint)sizeof(PerFrameData));
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
        pass.SetPipeline(_pipeline);
        pass.SetBindGroup(0, _bindGroup);
        pass.SetBindGroup(1, _materialBindGroup);
        pass.SetBindGroup(2, _environmentBindGroup);
        pass.SetVertexBuffer(0, _vertexBuffer);
        pass.SetIndexBuffer(_indexBuffer, WGPUIndexFormat.Uint32);
        pass.DrawIndexed(_indexCount);
        pass.End();
        using var commands = encoder.Finish();
        _device.Queue.Submit(commands);
    }

    public override void FramebufferResize(Vector2D<int> size) => CreateSwapchain();

    public override void Dispose()
    {
        _timer.Stop();
        _bindGroup?.Dispose();
        _materialBindGroup?.Dispose();
        _environmentBindGroup?.Dispose();
        _pipeline?.Dispose();
        _shader?.Dispose();
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _perFrameBuffer?.Dispose();
        _materialBuffer?.Dispose();
        foreach (var texture in _textures) texture.Dispose();
        _textures.Clear();
        _depthTextureView?.Dispose();
        _depthTexture?.Dispose();
    }
}
