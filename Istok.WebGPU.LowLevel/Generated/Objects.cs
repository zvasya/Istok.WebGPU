using Silk.NET.Core;

namespace Istok.WebGPU.LowLevel;

public record struct WGPUAdapter(IntPtr Handle)
{
	public readonly IntPtr Handle = Handle;
	public static WGPUAdapter Null => new WGPUAdapter(IntPtr.Zero);
	public static implicit operator WGPUAdapter(IntPtr handle) => new WGPUAdapter(handle);
}

public record struct WGPUBindGroup(IntPtr Handle)
{
	public readonly IntPtr Handle = Handle;
	public static WGPUBindGroup Null => new WGPUBindGroup(IntPtr.Zero);
	public static implicit operator WGPUBindGroup(IntPtr handle) => new WGPUBindGroup(handle);
}

public record struct WGPUBindGroupLayout(IntPtr Handle)
{
	public readonly IntPtr Handle = Handle;
	public static WGPUBindGroupLayout Null => new WGPUBindGroupLayout(IntPtr.Zero);
	public static implicit operator WGPUBindGroupLayout(IntPtr handle) => new WGPUBindGroupLayout(handle);
}

public record struct WGPUBuffer(IntPtr Handle)
{
	public readonly IntPtr Handle = Handle;
	public static WGPUBuffer Null => new WGPUBuffer(IntPtr.Zero);
	public static implicit operator WGPUBuffer(IntPtr handle) => new WGPUBuffer(handle);
}

public record struct WGPUCommandBuffer(IntPtr Handle)
{
	public readonly IntPtr Handle = Handle;
	public static WGPUCommandBuffer Null => new WGPUCommandBuffer(IntPtr.Zero);
	public static implicit operator WGPUCommandBuffer(IntPtr handle) => new WGPUCommandBuffer(handle);
}

public record struct WGPUCommandEncoder(IntPtr Handle)
{
	public readonly IntPtr Handle = Handle;
	public static WGPUCommandEncoder Null => new WGPUCommandEncoder(IntPtr.Zero);
	public static implicit operator WGPUCommandEncoder(IntPtr handle) => new WGPUCommandEncoder(handle);
}

public record struct WGPUComputePassEncoder(IntPtr Handle)
{
	public readonly IntPtr Handle = Handle;
	public static WGPUComputePassEncoder Null => new WGPUComputePassEncoder(IntPtr.Zero);
	public static implicit operator WGPUComputePassEncoder(IntPtr handle) => new WGPUComputePassEncoder(handle);
}

public record struct WGPUComputePipeline(IntPtr Handle)
{
	public readonly IntPtr Handle = Handle;
	public static WGPUComputePipeline Null => new WGPUComputePipeline(IntPtr.Zero);
	public static implicit operator WGPUComputePipeline(IntPtr handle) => new WGPUComputePipeline(handle);
}

public record struct WGPUDevice(IntPtr Handle)
{
	public readonly IntPtr Handle = Handle;
	public static WGPUDevice Null => new WGPUDevice(IntPtr.Zero);
	public static implicit operator WGPUDevice(IntPtr handle) => new WGPUDevice(handle);
}

public record struct WGPUExternalTexture(IntPtr Handle)
{
	public readonly IntPtr Handle = Handle;
	public static WGPUExternalTexture Null => new WGPUExternalTexture(IntPtr.Zero);
	public static implicit operator WGPUExternalTexture(IntPtr handle) => new WGPUExternalTexture(handle);
}

public record struct WGPUInstance(IntPtr Handle)
{
	public readonly IntPtr Handle = Handle;
	public static WGPUInstance Null => new WGPUInstance(IntPtr.Zero);
	public static implicit operator WGPUInstance(IntPtr handle) => new WGPUInstance(handle);
}

public record struct WGPUPipelineLayout(IntPtr Handle)
{
	public readonly IntPtr Handle = Handle;
	public static WGPUPipelineLayout Null => new WGPUPipelineLayout(IntPtr.Zero);
	public static implicit operator WGPUPipelineLayout(IntPtr handle) => new WGPUPipelineLayout(handle);
}

public record struct WGPUQuerySet(IntPtr Handle)
{
	public readonly IntPtr Handle = Handle;
	public static WGPUQuerySet Null => new WGPUQuerySet(IntPtr.Zero);
	public static implicit operator WGPUQuerySet(IntPtr handle) => new WGPUQuerySet(handle);
}

public record struct WGPUQueue(IntPtr Handle)
{
	public readonly IntPtr Handle = Handle;
	public static WGPUQueue Null => new WGPUQueue(IntPtr.Zero);
	public static implicit operator WGPUQueue(IntPtr handle) => new WGPUQueue(handle);
}

public record struct WGPURenderBundle(IntPtr Handle)
{
	public readonly IntPtr Handle = Handle;
	public static WGPURenderBundle Null => new WGPURenderBundle(IntPtr.Zero);
	public static implicit operator WGPURenderBundle(IntPtr handle) => new WGPURenderBundle(handle);
}

public record struct WGPURenderBundleEncoder(IntPtr Handle)
{
	public readonly IntPtr Handle = Handle;
	public static WGPURenderBundleEncoder Null => new WGPURenderBundleEncoder(IntPtr.Zero);
	public static implicit operator WGPURenderBundleEncoder(IntPtr handle) => new WGPURenderBundleEncoder(handle);
}

public record struct WGPURenderPassEncoder(IntPtr Handle)
{
	public readonly IntPtr Handle = Handle;
	public static WGPURenderPassEncoder Null => new WGPURenderPassEncoder(IntPtr.Zero);
	public static implicit operator WGPURenderPassEncoder(IntPtr handle) => new WGPURenderPassEncoder(handle);
}

public record struct WGPURenderPipeline(IntPtr Handle)
{
	public readonly IntPtr Handle = Handle;
	public static WGPURenderPipeline Null => new WGPURenderPipeline(IntPtr.Zero);
	public static implicit operator WGPURenderPipeline(IntPtr handle) => new WGPURenderPipeline(handle);
}

public record struct WGPUSampler(IntPtr Handle)
{
	public readonly IntPtr Handle = Handle;
	public static WGPUSampler Null => new WGPUSampler(IntPtr.Zero);
	public static implicit operator WGPUSampler(IntPtr handle) => new WGPUSampler(handle);
}

public record struct WGPUShaderModule(IntPtr Handle)
{
	public readonly IntPtr Handle = Handle;
	public static WGPUShaderModule Null => new WGPUShaderModule(IntPtr.Zero);
	public static implicit operator WGPUShaderModule(IntPtr handle) => new WGPUShaderModule(handle);
}

public record struct WGPUSurface(IntPtr Handle)
{
	public readonly IntPtr Handle = Handle;
	public static WGPUSurface Null => new WGPUSurface(IntPtr.Zero);
	public static implicit operator WGPUSurface(IntPtr handle) => new WGPUSurface(handle);
}

public record struct WGPUTexture(IntPtr Handle)
{
	public readonly IntPtr Handle = Handle;
	public static WGPUTexture Null => new WGPUTexture(IntPtr.Zero);
	public static implicit operator WGPUTexture(IntPtr handle) => new WGPUTexture(handle);
}

public record struct WGPUTextureView(IntPtr Handle)
{
	public readonly IntPtr Handle = Handle;
	public static WGPUTextureView Null => new WGPUTextureView(IntPtr.Zero);
	public static implicit operator WGPUTextureView(IntPtr handle) => new WGPUTextureView(handle);
}

