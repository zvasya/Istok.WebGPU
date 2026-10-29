using Istok.WebGPU;
using Istok.WebGPU.LowLevel;
using static Istok.WebGPU.LowLevel.WebGPUNative;

namespace Examples.Utils;

internal sealed class MipmapGenerator : IDisposable
{
    private readonly GPUDevice _device;
    private readonly GPUShaderModule _shader;
    private readonly GPUSampler _sampler;
    private readonly Dictionary<WGPUTextureFormat, GPURenderPipeline> _pipelines = [];

    public MipmapGenerator(GPUDevice device, string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Mipmap shader source must not be empty.", nameof(code));
        _device = device;
        _shader = device.CreateShaderModule(new GPUShaderModuleDescriptor { Label = "Generate mipmaps", Code = code });
        _sampler = device.CreateSampler(new GPUSamplerDescriptor
        {
            MinFilter = WGPUFilterMode.Linear,
            MagFilter = WGPUFilterMode.Linear,
            MipmapFilter = WGPUMipmapFilterMode.Nearest,
            LodMaxClamp = 0,
        });
    }

    public unsafe void Generate(GPUTexture texture)
    {
        if (texture.MipLevelCount <= 1)
            return;
        if (!_pipelines.TryGetValue(texture.Format, out var pipeline))
        {
            pipeline = _device.CreateRenderPipeline(new GPURenderPipelineDescriptor
            {
                Label = $"Generate {texture.Format} mipmaps",
                Layout = GPUPipelineLayout.Auto,
                Vertex = new GPUVertexState { Module = _shader, EntryPoint = "vs_main" },
                Fragment = new GPUFragmentState
                {
                    Module = _shader,
                    EntryPoint = "fs_main",
                    Targets = [new GPUColorTargetState { Format = texture.Format, WriteMask = WGPUColorWriteMask.All }],
                },
            });
            _pipelines.Add(texture.Format, pipeline);
        }
        using var layout = pipeline.GetBindGroupLayout(0);
        using var encoder = _device.CreateCommandEncoder();
        for (uint mip = 1; mip < texture.MipLevelCount; mip++)
        {
            using var source = texture.CreateView(new GPUTextureViewDescriptor
            {
                Dimension = WGPUTextureViewDimension.D2D,
                BaseMipLevel = mip - 1,
                MipLevelCount = 1,
                ArrayLayerCount = 1,
                Usage = WGPUTextureUsage.TextureBinding,
            });
            using var destination = texture.CreateView(new GPUTextureViewDescriptor
            {
                Dimension = WGPUTextureViewDimension.D2D,
                BaseMipLevel = mip,
                MipLevelCount = 1,
                ArrayLayerCount = 1,
                Usage = WGPUTextureUsage.RenderAttachment,
            });
            using var group = _device.CreateBindGroup(new GPUBindGroupDescriptor
            {
                Layout = layout,
                Entries =
                [
                    new WGPUBindGroupEntry { binding = 0, textureView = source },
                    new WGPUBindGroupEntry { binding = 1, sampler = _sampler },
                ],
            });
            using var pass = encoder.BeginRenderPass(new GPURenderPassDescriptor
            {
                ColorAttachments = [new WGPURenderPassColorAttachment
                {
                    view = destination,
                    loadOp = WGPULoadOp.Clear,
                    storeOp = WGPUStoreOp.Store,
                    depthSlice = DepthSliceUndefined,
                }],
            });
            pass.SetPipeline(pipeline);
            pass.SetBindGroup(0, group);
            pass.Draw(3);
            pass.End();
        }
        using var commands = encoder.Finish();
        _device.Queue.Submit(commands);
    }

    public void Dispose()
    {
        foreach (var pipeline in _pipelines.Values) pipeline.Dispose();
        _pipelines.Clear();
        _sampler.Dispose();
        _shader.Dispose();
    }
}
