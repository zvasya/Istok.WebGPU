using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using AssetManager;
using Examples.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Istok.WebGPU;
using Istok.WebGPU.LowLevel;
using Istok.WebGPU.View;
using Silk.NET.Maths;
using static Istok.WebGPU.LowLevel.WebGPUNative;

namespace Examples;

// Chapter06/01_Unlit: a rotating textured mesh, without lighting.
public class ExampleUnlit : ExampleBase
{
    private const WGPUTextureFormat DepthFormat = WGPUTextureFormat.Depth32Float;

    private GPUShaderModule _shader = null!;
    private GPURenderPipeline _pipeline = null!;
    private GPUBuffer _vertexBuffer = null!;
    private GPUBuffer _indexBuffer = null!;
    private uint _indexCount;
    private Vector4 _baseColor;
    private GPUBuffer _perFrameBuffer = null!;
    private GPUTexture _baseColorTexture = null!;
    private GPUTextureView _baseColorTextureView = null!;
    private GPUSampler _sampler = null!;
    private GPUBindGroup _bindGroup = null!;
    private GPUTexture? _depthTexture;
    private GPUTextureView? _depthTextureView;
    private readonly Stopwatch _timer = new();

    // WGSL uniform structs have 16-byte alignment: 64-byte matrix + 16-byte base color.
    [StructLayout(LayoutKind.Sequential)]
    private struct PerFrameData
    {
        public Matrix4x4 Mvp;
        public Vector4 BaseColor;
    }

    public ExampleUnlit(GPU instance, GPUDevice device, IWebGpuView window,
        GPUSurfaceCapabilities surfaceCapabilities, GPUSurface surface, IResourcesProvider resourcesProvider)
        : base(instance, device, window, surfaceCapabilities, surface, resourcesProvider)
    {
    }

    public override async Task OnLoad()
    {
        var model = await GltfModel.LoadAsync(_ResourcesProvider, "Models/DamagedHelmet/DamagedHelmet.gltf");
        _indexCount = (uint)model.Indices.Length;
        _baseColor = model.BaseColor;
        string code = await _ResourcesProvider.LoadTextAsync("Shaders/example_unlit.wgsl");
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Unlit shader source must not be empty.");
        _shader = _device.CreateShaderModule(new GPUShaderModuleDescriptor { Code = code });
        using Image<Rgba32> image = model.BaseColorImage == null
            ? new Image<Rgba32>(1, 1, new Rgba32(255, 255, 255, 255))
            : Image.Load<Rgba32>(model.BaseColorImage);
        unsafe
        {
            WGPUVertexAttribute* attributes = stackalloc WGPUVertexAttribute[]
            {
                new() { shaderLocation = 0, format = WGPUVertexFormat.Float32X3, offset = 0 },
                new() { shaderLocation = 1, format = WGPUVertexFormat.Float32X4, offset = (ulong)(sizeof(Vector3) * 2) },
                new() { shaderLocation = 2, format = WGPUVertexFormat.Float32X2, offset = (ulong)(sizeof(Vector3) * 2 + sizeof(Vector4)) },
            };
            var depthState = new WGPUDepthStencilState
            {
                format = DepthFormat,
                depthWriteEnabled = WGPUOptionalBool.True,
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
                        attributeCount = 3,
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
                        Blend = GPUBlendState.Opaque,
                        WriteMask = WGPUColorWriteMask.All,
                    }],
                },
                Primitive = new GPUPrimitiveState
                {
                    Topology = WGPUPrimitiveTopology.TriangleList,
                    FrontFace = WGPUFrontFace.CCW,
                    CullMode = WGPUCullMode.Back,
                },
                DepthStencil = ref depthState,
            });

            _vertexBuffer = _device.CreateBuffer(new GPUBufferDescriptor
            {
                Label = "Unlit: vertices",
                Size = (ulong)model.Vertices.Length * (ulong)sizeof(GltfModel.Vertex),
                Usage = WGPUBufferUsage.Vertex,
                MappedAtCreation = true,
            });
            model.Vertices.AsSpan().CopyTo(_vertexBuffer.GetMappedRange<GltfModel.Vertex>(0, model.Vertices.Length));
            _vertexBuffer.Unmap();
            _indexBuffer = _device.CreateBuffer(new GPUBufferDescriptor
            {
                Label = "Unlit: indices",
                Size = (ulong)model.Indices.Length * sizeof(uint),
                Usage = WGPUBufferUsage.Index,
                MappedAtCreation = true,
            });
            model.Indices.AsSpan().CopyTo(_indexBuffer.GetMappedRange<uint>(0, model.Indices.Length));
            _indexBuffer.Unmap();
            _perFrameBuffer = _device.CreateBuffer(new GPUBufferDescriptor
            {
                Label = "Unlit: per-frame uniforms",
                Size = (ulong)sizeof(PerFrameData),
                Usage = WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst,
            });
            _baseColorTexture = _device.CreateTexture(new GPUTextureDescriptor
            {
                Label = "Unlit: base color",
                Size = new WGPUExtent3D { width = (uint)image.Width, height = (uint)image.Height, depthOrArrayLayers = 1 },
                Dimension = WGPUTextureDimension.D2D,
                Format = WGPUTextureFormat.RGBA8UnormSrgb,
                Usage = WGPUTextureUsage.TextureBinding | WGPUTextureUsage.CopyDst,
            });
            var pixels = new Rgba32[image.Width * image.Height];
            image.CopyPixelDataTo(pixels);
            fixed (Rgba32* pixelsPtr = pixels)
            {
                _device.Queue.WriteTexture(
                    new WGPUTexelCopyTextureInfo { texture = _baseColorTexture, aspect = WGPUTextureAspect.All },
                    pixelsPtr, (nuint)(pixels.Length * sizeof(Rgba32)),
                    new WGPUTexelCopyBufferLayout { bytesPerRow = (uint)(image.Width * sizeof(Rgba32)), rowsPerImage = (uint)image.Height },
                    new WGPUExtent3D { width = (uint)image.Width, height = (uint)image.Height, depthOrArrayLayers = 1 });
            }
            _baseColorTextureView = _baseColorTexture.CreateView();
            // DamagedHelmet uses the default glTF sampler: repeat with linear filtering.
            _sampler = _device.CreateSampler(new GPUSamplerDescriptor
            {
                AddressModeU = WGPUAddressMode.Repeat,
                AddressModeV = WGPUAddressMode.Repeat,
                MagFilter = WGPUFilterMode.Linear,
                MinFilter = WGPUFilterMode.Linear,
                MipmapFilter = WGPUMipmapFilterMode.Linear,
            });
            using var layout = _pipeline.GetBindGroupLayout(0);
            _bindGroup = _device.CreateBindGroup(new GPUBindGroupDescriptor
            {
                Layout = layout,
                Entries =
                [
                    new WGPUBindGroupEntry { binding = 0, buffer = _perFrameBuffer, size = _perFrameBuffer.Size },
                    new WGPUBindGroupEntry { binding = 1, textureView = _baseColorTextureView },
                    new WGPUBindGroupEntry { binding = 2, sampler = _sampler },
                ],
            });
        }
        CreateSwapchain();
        _timer.Restart();
        Console.WriteLine($"Unlit: loaded {model.Vertices.Length} vertices, {_indexCount} indices using glTF2Loader.");
    }

    private WGPUTextureFormat SurfaceFormat() =>
        _SurfaceCapabilities.Formats.FirstOrDefault(format => format.IsSRGB(), _SurfaceCapabilities.Formats[0]);

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
            Label = "Unlit: depth",
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
        float time = (float)_timer.Elapsed.TotalSeconds;
        var modelMatrix = Matrix4x4.CreateRotationX(MathF.PI / 2) * Matrix4x4.CreateRotationY(time);
        var cameraView = Matrix4x4.CreateLookAt(new Vector3(0, 0, 3.5f), Vector3.Zero, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4, (float)width / height, 0.1f, 1000.0f);
        var perFrame = new PerFrameData
        {
            // System.Numerics row-vector order; WGSL interprets the memory as column-major.
            Mvp = modelMatrix * cameraView * projection,
            BaseColor = _baseColor,
        };
        _device.Queue.WriteBuffer(_perFrameBuffer, 0, &perFrame, (nuint)sizeof(PerFrameData));

        using var encoder = _device.CreateCommandEncoder();
        var depthAttachment = new WGPURenderPassDepthStencilAttachment
        {
            view = _depthTextureView!,
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
        pass.SetVertexBuffer(0, _vertexBuffer);
        pass.SetIndexBuffer(_indexBuffer, WGPUIndexFormat.Uint32);
        pass.DrawIndexed(_indexCount);
        pass.End();

        using var commands = encoder.Finish();
        _device.Queue.Submit(commands);
        _Surface.Present();
        _Window.SwapBuffers();
    }

    public override void FramebufferResize(Vector2D<int> size) => CreateSwapchain();

    public override void Dispose()
    {
        _timer.Stop();
        _bindGroup?.Dispose();
        _pipeline?.Dispose();
        _shader?.Dispose();
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _perFrameBuffer?.Dispose();
        _baseColorTextureView?.Dispose();
        _baseColorTexture?.Dispose();
        _sampler?.Dispose();
        _depthTextureView?.Dispose();
        _depthTexture?.Dispose();
    }
}
