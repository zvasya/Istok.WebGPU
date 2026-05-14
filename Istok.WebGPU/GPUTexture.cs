namespace Istok.WebGPU;

public unsafe class GPUTexture(WGPUTexture texture, string? label) : GPUObjectWithName<WGPUTexture>(texture, label)
{
	public GPUTextureView CreateView()
	{
		var textureView = wgpuTextureCreateView(_handle, null);
		return new GPUTextureView(textureView, null);
	}

	public GPUTextureView CreateView(GPUTextureViewDescriptor descriptor)
	{
		using (descriptor.Label.ToWGPUStringView(out WGPUStringView labelPtr))
		{
			WGPUTextureView textureView;
			{
				var textureViewDescriptor = new WGPUTextureViewDescriptor() with
				{
					label = labelPtr,
					format = descriptor.Format,
					dimension = descriptor.Dimension,
					baseMipLevel = descriptor.BaseMipLevel,
					mipLevelCount = descriptor.MipLevelCount,
					baseArrayLayer = descriptor.BaseArrayLayer,
					arrayLayerCount = descriptor.ArrayLayerCount,
					aspect = descriptor.Aspect,
					usage = descriptor.Usage,
				};
				textureView = wgpuTextureCreateView(_handle, &textureViewDescriptor);
			}
			return new GPUTextureView(textureView, descriptor.Label);
		}
	}
	
	public void Destroy()
	{
		wgpuTextureDestroy(_handle);
	}

	public uint Width => wgpuTextureGetWidth(_handle);
	public uint Height => wgpuTextureGetHeight(_handle);
	public uint DepthOrArrayLayers  => wgpuTextureGetDepthOrArrayLayers(_handle);
	public uint MipLevelCount => wgpuTextureGetMipLevelCount(_handle);

	public uint SampleCount => wgpuTextureGetSampleCount(_handle);
	public WGPUTextureDimension Dimension => wgpuTextureGetDimension(_handle);
	
	public WGPUTextureFormat Format => wgpuTextureGetFormat(_handle);
	public WGPUTextureUsage Usage => wgpuTextureGetUsage(_handle);

	public override void Dispose()
	{
		wgpuTextureRelease(_handle);
	}
	
	protected override void SetLabel(WGPUStringView label)
	{
		wgpuTextureSetLabel(_handle,label);
	}
}