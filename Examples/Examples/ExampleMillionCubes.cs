using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using AssetManager;
using Istok.WebGPU;
using Istok.WebGPU.LowLevel;
using Istok.WebGPU.View;
using Silk.NET.Maths;
using static Istok.WebGPU.LowLevel.WebGPUNative;

namespace Examples;

public class ExampleMillionCubes : ExampleBase
{
    public const uint NumCubes = 1024 * 1024;
    private const int TextureSize = 256;
    private const WGPUTextureFormat DepthFormat = WGPUTextureFormat.Depth32Float;

    private GPUShaderModule _shader = null!;
    private GPURenderPipeline _pipeline = null!;
    private GPUBuffer _centersBuffer = null!;
    private GPUBuffer _perFrameBuffer = null!;
    private GPUTexture _xorTexture = null!;
    private GPUTextureView _xorTextureView = null!;
    private GPUSampler _sampler = null!;
    private GPUBindGroup _bindGroup = null!;
    private GPUTexture? _depthTexture;
    private GPUTextureView? _depthTextureView;
    private readonly Stopwatch _timer = new();

    // WGSL uniform structs have 16-byte alignment: 64-byte matrix + 16-byte time/padding.
    [StructLayout(LayoutKind.Sequential)]
    private struct PerFrameData
    {
        public Matrix4x4 ViewProjection;
        public Vector4 Time;
    }

    public ExampleMillionCubes(GPU instance, GPUDevice device, IWebGpuView window,
        GPUSurfaceCapabilities surfaceCapabilities, GPUSurface surface, IResourcesProvider resourcesProvider)
        : base(instance, device, window, surfaceCapabilities, surface, resourcesProvider)
    {
    }

    public override async Task OnLoad()
    {
        string code = await _ResourcesProvider.LoadTextAsync("Shaders/example_million_cubes.wgsl");
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Million cubes shader source must not be empty.");

        _shader = _device.CreateShaderModule(new GPUShaderModuleDescriptor { Code = code });
        unsafe
        {
            var depthState = new WGPUDepthStencilState
            {
                format = DepthFormat,
                depthWriteEnabled = WGPUOptionalBool.True,
                depthCompare = WGPUCompareFunction.Less,
            };
            _pipeline = _device.CreateRenderPipeline(new GPURenderPipelineDescriptor
            {
                Layout = GPUPipelineLayout.Auto,
                Vertex = new GPUVertexState { Module = _shader, EntryPoint = "vs_main" },
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
                Primitive = new GPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList },
                DepthStencil = ref depthState,
            });

            _centersBuffer = _device.CreateBuffer(new GPUBufferDescriptor
            {
                Label = "Million cubes: centers and initial angles",
                Size = (ulong)NumCubes * (ulong)sizeof(Vector4),
                Usage = WGPUBufferUsage.Storage,
                MappedAtCreation = true,
            });
            // Fill the immutable 16 MiB storage buffer directly, without a second CPU array.
            Span<Vector4> centers = _centersBuffer.GetMappedRange<Vector4>(0, (int)NumCubes);
            var random = new Random(0);
            for (int i = 0; i < centers.Length; i++)
            {
                centers[i] = new Vector4(
                    random.NextSingle() * 1000.0f - 500.0f,
                    random.NextSingle() * 1000.0f - 500.0f,
                    random.NextSingle() * 1000.0f - 500.0f,
                    random.NextSingle() * MathF.PI);
            }
            _centersBuffer.Unmap();

            _perFrameBuffer = _device.CreateBuffer(new GPUBufferDescriptor
            {
                Label = "Million cubes: per-frame uniforms",
                Size = (ulong)sizeof(PerFrameData),
                Usage = WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst,
            });

            _xorTexture = _device.CreateTexture(new GPUTextureDescriptor
            {
                Label = "XOR pattern",
                Size = new WGPUExtent3D { width = TextureSize, height = TextureSize, depthOrArrayLayers = 1 },
                Dimension = WGPUTextureDimension.D2D,
                Format = WGPUTextureFormat.RGBA8Unorm,
                Usage = WGPUTextureUsage.TextureBinding | WGPUTextureUsage.CopyDst,
            });
            var pixels = new uint[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            for (int x = 0; x < TextureSize; x++)
            {
                uint value = (uint)(x ^ y);
                pixels[y * TextureSize + x] = 0xff000000u | (value << 16) | (value << 8) | value;
            }
            fixed (uint* pixelsPtr = pixels)
            {
                _device.Queue.WriteTexture(
                    new WGPUTexelCopyTextureInfo { texture = _xorTexture, aspect = WGPUTextureAspect.All },
                    pixelsPtr, (nuint)(pixels.Length * sizeof(uint)),
                    new WGPUTexelCopyBufferLayout { bytesPerRow = TextureSize * sizeof(uint), rowsPerImage = TextureSize },
                    new WGPUExtent3D { width = TextureSize, height = TextureSize, depthOrArrayLayers = 1 });
            }
            _xorTextureView = _xorTexture.CreateView();
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
                    new WGPUBindGroupEntry { binding = 1, buffer = _centersBuffer, size = _centersBuffer.Size },
                    new WGPUBindGroupEntry { binding = 2, textureView = _xorTextureView },
                    new WGPUBindGroupEntry { binding = 3, sampler = _sampler },
                ],
            });
        }
        CreateSwapchain();
        _timer.Restart();
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
            Label = "Million cubes: depth",
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
        var cameraView = Matrix4x4.CreateTranslation(0, 0, -1000.0f + 500.0f * (1.0f - MathF.Cos(time * 0.5f)));
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4, (float)width / height, 0.2f, 1500.0f);
        var perFrame = new PerFrameData
        {
            ViewProjection = cameraView * projection,
            Time = new Vector4(time, 0, 0, 0),
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
        pass.Draw(36, NumCubes, 0, 0);
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
        _centersBuffer?.Dispose();
        _perFrameBuffer?.Dispose();
        _xorTextureView?.Dispose();
        _xorTexture?.Dispose();
        _sampler?.Dispose();
        _depthTextureView?.Dispose();
        _depthTexture?.Dispose();
    }
}
