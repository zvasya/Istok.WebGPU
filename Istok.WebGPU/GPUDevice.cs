using System.Diagnostics.CodeAnalysis;

namespace Istok.WebGPU;

public unsafe class GPUDevice(WGPUDevice device, string? label, GPUAdapterInfo adapterInfo, Task<(WGPUDeviceLostReason, string?)> lostDevicePromise)
	: GPUObjectWithName<WGPUDevice>(device, label)
{
	public GPUAdapterInfo AdapterInfo { get; } = adapterInfo;

	[field: AllowNull, MaybeNull]
	public WGPUFeatureName[] Features => field ??= EnumerateFeatures();
	WGPUFeatureName[] EnumerateFeatures()
	{
		WGPUSupportedFeatures supportedFeatures =  new WGPUSupportedFeatures();
		wgpuDeviceGetFeatures(_handle, &supportedFeatures);

		Span<WGPUFeatureName> span = new Span<WGPUFeatureName>(supportedFeatures.features, (int) supportedFeatures.featureCount);
		WGPUFeatureName[] features = span.ToArray();
		
		wgpuSupportedFeaturesFreeMembers(supportedFeatures);
		return features;
	}

	protected override void SetLabel(WGPUStringView label)
	{
		wgpuDeviceSetLabel(_handle, label);
	}

	public WGPULimits Limits => GetLimits();

	public Task<(WGPUDeviceLostReason, string?)> Lost { get; } = lostDevicePromise;

	[field: AllowNull, MaybeNull]
	public GPUQueue Queue => field ??= GetQueue();

	public GPUQueue GetQueue()
	{
		var queue = wgpuDeviceGetQueue(_handle);
		return new GPUQueue(queue, null);
	}

	public WGPULimits GetLimits()
	{
		var limits = new WGPULimits();
		wgpuDeviceGetLimits(_handle, &limits);
		return limits;
	}


	public override void Dispose()
	{
		wgpuDeviceRelease(_handle);
	}

	// [Throws]
	public GPUBuffer CreateBuffer(GPUBufferDescriptor descriptor)
	{
		using (descriptor.Label.ToWGPUStringView(out var labelPtr))
		{

			WGPUBufferDescriptor bufferDescriptor = new WGPUBufferDescriptor() with
			{
				label = labelPtr,
				usage = descriptor.Usage,
				size = descriptor.Size,
				mappedAtCreation = descriptor.MappedAtCreation,
			};
			WGPUBuffer buffer = wgpuDeviceCreateBuffer(_handle, &bufferDescriptor);
			return new GPUBuffer(buffer, descriptor.Label);
		}
	}

	public GPUTexture CreateTexture(GPUTextureDescriptor descriptor)
	{
		WGPUTexture texture;
		using(descriptor.Label.ToWGPUStringView(out WGPUStringView descriptorLabelPtr))
			fixed (WGPUTextureFormat* viewFormatsPtr = descriptor.ViewFormats)
			{
				var textureDescriptor = new WGPUTextureDescriptor() with
				{
					label = descriptorLabelPtr,
					usage = descriptor.Usage,
					dimension = descriptor.Dimension,
					size = descriptor.Size,
					format = descriptor.Format,
					mipLevelCount = descriptor.MipLevelCount,
					sampleCount = descriptor.SampleCount,
					viewFormatCount = (UIntPtr)descriptor.ViewFormats.Length,
					viewFormats = viewFormatsPtr,
				};
				texture = wgpuDeviceCreateTexture(_handle, &textureDescriptor);
			}
	
		return new GPUTexture(texture, descriptor.Label);
	}

	public GPUSampler CreateSampler()
	{
		var sampler = wgpuDeviceCreateSampler(_handle, null);
		return new GPUSampler(sampler, null);
	}

	public GPUSampler CreateSampler(GPUSamplerDescriptor descriptor)
	{
		using (descriptor.Label.ToWGPUStringView(out WGPUStringView descriptorLabelPtr))
		{
			var samplerDescriptor = new WGPUSamplerDescriptor() with
			{
				label = descriptorLabelPtr,
				addressModeU = descriptor.AddressModeU,
				addressModeV = descriptor.AddressModeV,
				addressModeW = descriptor.AddressModeW,
				magFilter = descriptor.MagFilter,
				minFilter = descriptor.MinFilter,
				mipmapFilter = descriptor.MipmapFilter,
				lodMinClamp = descriptor.LodMinClamp,
				lodMaxClamp = descriptor.LodMaxClamp,
				compare = descriptor.Compare,
				maxAnisotropy = descriptor.MaxAnisotropy,
			};
			var sampler = wgpuDeviceCreateSampler(_handle, &samplerDescriptor);
			return new GPUSampler(sampler, descriptor.Label);
		}
	}

	public GPUBindGroupLayout CreateBindGroupLayout(GPUBindGroupLayoutDescriptor descriptor)
	{
		using (descriptor.Label.ToWGPUStringView(out WGPUStringView descriptorLabelPtr))
		{
			WGPUBindGroupLayout bindGroupLayout;
			
			fixed (WGPUBindGroupLayoutEntry* entriesPtr = descriptor.Entries)
			{
				var bindGroupLayoutDescriptor = new WGPUBindGroupLayoutDescriptor() with
				{
					label = descriptorLabelPtr,
					entryCount = (UIntPtr)descriptor.Entries.Length,
					entries = entriesPtr
				};
				bindGroupLayout = wgpuDeviceCreateBindGroupLayout(_handle, &bindGroupLayoutDescriptor);
			}

			return new GPUBindGroupLayout(bindGroupLayout, descriptor.Label);
		}
	}

	public GPUPipelineLayout CreatePipelineLayout(GPUPipelineLayoutDescriptor descriptor)
	{
		using (descriptor.Label.ToWGPUStringView(out WGPUStringView descriptorLabelPtr))
		{
			var count = descriptor.BindGroupLayouts.Length;
			WGPUBindGroupLayout* bindGroupLayouts = stackalloc WGPUBindGroupLayout[count];
			for (var i = 0; i < count; i++) 
				bindGroupLayouts[i] = descriptor.BindGroupLayouts[i]._handle;

			var pipelineLayoutDescriptor = new WGPUPipelineLayoutDescriptor() with
			{
				label = descriptorLabelPtr,
				bindGroupLayoutCount = (UIntPtr)count,
				bindGroupLayouts = bindGroupLayouts,
				immediateSize = descriptor.ImmediateSize
			};
			var pipelineLayout = wgpuDeviceCreatePipelineLayout(_handle, &pipelineLayoutDescriptor);

			return new GPUPipelineLayout(pipelineLayout, descriptor.Label);
		}
	}

	public GPUBindGroup CreateBindGroup(GPUBindGroupDescriptor descriptor)
	{
		using (descriptor.Label.ToWGPUStringView(out WGPUStringView descriptorLabelPtr))
		{
			WGPUBindGroup bindGroup;

			fixed (WGPUBindGroupEntry* entriesPtr = descriptor.Entries)
			{
				var bindGroupLayoutDescriptor = new WGPUBindGroupDescriptor() with
				{
					label = descriptorLabelPtr,
					layout = descriptor.Layout._handle,
					entryCount = (UIntPtr)descriptor.Entries.Length,
					entries = entriesPtr
				};
				bindGroup = wgpuDeviceCreateBindGroup(_handle, &bindGroupLayoutDescriptor);
			}


			return new GPUBindGroup(bindGroup, descriptor.Label);
		}
	}

// 	[Throws]
	public GPUShaderModule CreateShaderModule(GPUShaderModuleDescriptor descriptor)
	{

		using (descriptor.Label.ToWGPUStringView(out var labelPtr))
		using (descriptor.Code.ToWGPUStringView(out var codePtr))
		{

			WGPUShaderModule shaderModule;
			
			var wgslDescriptor = new WGPUShaderSourceWGSL
			{
				code = codePtr,
				chain = new ChainedStruct
				{
					sType = WGPUSType.ShaderSourceWGSL
				}
			};
			
			// fixed (ShaderModuleCompilationHint* hintsPtr = descriptor.CompilationHints)
			{
				var shaderModuleDescriptor = new WGPUShaderModuleDescriptor() with
				{
					nextInChain = (ChainedStruct*)(&wgslDescriptor),
					label = labelPtr,
					// hintCount = (UIntPtr)descriptor.CompilationHints.Length,
					// Hints = hintsPtr
				};
				shaderModule = wgpuDeviceCreateShaderModule(_handle, &shaderModuleDescriptor);
			}

			return new GPUShaderModule(shaderModule, descriptor.Label);
		}
	}

	public GPUComputePipeline CreateComputePipeline(GPUComputePipelineDescriptor descriptor)
	{
		using (descriptor.Label.ToWGPUStringView(out WGPUStringView labelPtr))
		using (descriptor.Compute.EntryPoint.ToWGPUStringView(out WGPUStringView entryPointPtr))
		{
			WGPUComputePipeline computePipeline;
			fixed (WGPUConstantEntry* constants = descriptor.Compute.Constants)
			{
				var computePipelineDescriptor = new WGPUComputePipelineDescriptor() with
				{
					label = labelPtr,
					layout = descriptor.Layout._handle,
					compute = new WGPUComputeState
					{
						module = descriptor.Compute.Module._handle,
						entryPoint = entryPointPtr,
						constantCount = (UIntPtr)descriptor.Compute.Constants.Length,
						constants = constants
					},
				};

				computePipeline = wgpuDeviceCreateComputePipeline(_handle, &computePipelineDescriptor);
			}

			return new GPUComputePipeline(computePipeline, descriptor.Label);
		}
	}

	public GPURenderPipeline CreateRenderPipeline(GPURenderPipelineDescriptor descriptor)
	{
		using (descriptor.Label.ToWGPUStringView(out WGPUStringView descriptorLabelPtr))
		using (descriptor.Vertex.EntryPoint.ToWGPUStringView(out WGPUStringView vertexEntryPointPtr))
		using (descriptor.Fragment.GetValueOrDefault().EntryPoint.ToWGPUStringView(out WGPUStringView entryPointPtr))
		{
			WGPURenderPipeline renderPipeline;
			fixed (WGPUConstantEntry* vertexConstants = descriptor.Vertex.Constants)
			fixed (WGPUVertexBufferLayout* vertexBufferLayout = descriptor.Vertex.Buffers)
			fixed (WGPUConstantEntry* constants = descriptor.Fragment.GetValueOrDefault().Constants)
			{
				WGPUVertexState vertexState = new WGPUVertexState
				{
					module = descriptor.Vertex.Module._handle,
					entryPoint = vertexEntryPointPtr,
					constantCount = (UIntPtr)descriptor.Vertex.Constants.Length,
					constants = vertexConstants,
					bufferCount = (UIntPtr)descriptor.Vertex.Buffers.Length,
					buffers = vertexBufferLayout
				};
				WGPUColorTargetState* targets = stackalloc WGPUColorTargetState[descriptor.Fragment.GetValueOrDefault().Targets.Length];
				WGPUBlendState* targetsBlendState = stackalloc WGPUBlendState[descriptor.Fragment.GetValueOrDefault().Targets.Length];
				WGPUFragmentState fragment;
				if (descriptor.Fragment.HasValue)
				{
					GPUFragmentState gpuFragmentState = descriptor.Fragment.Value;
					for (int i = 0; i < gpuFragmentState.Targets.Length; i++)
					{
						GPUColorTargetState gpuColorTargetState = gpuFragmentState.Targets[i];
						targetsBlendState[i] = gpuColorTargetState.Blend.GetValueOrDefault();
						targets[i] = new WGPUColorTargetState
						{
							format = gpuColorTargetState.Format,
							blend = gpuColorTargetState.Blend.HasValue ? &targetsBlendState[i] : null,
							writeMask = gpuColorTargetState.WriteMask
						};
					}
					fragment = new WGPUFragmentState
					{
						nextInChain = null,
						module = gpuFragmentState.Module._handle,
						entryPoint = entryPointPtr,
						constantCount = (UIntPtr)gpuFragmentState.Constants.Length,
						constants = constants,
						targetCount = (UIntPtr)gpuFragmentState.Targets.Length,
						targets = targets
					};
				}
				
				WGPURenderPipelineDescriptor renderPipelineDescriptor = new WGPURenderPipelineDescriptor() with
				{
					label = descriptorLabelPtr,
					layout = descriptor.Layout._handle,
					vertex = vertexState,
					primitive = descriptor.Primitive,
					depthStencil = &descriptor.DepthStencil,
					multisample = descriptor.Multisample,
					fragment = descriptor.Fragment.HasValue ? &fragment : null,
				};
				renderPipeline = wgpuDeviceCreateRenderPipeline(_handle, &renderPipelineDescriptor);
			}

			

			return new GPURenderPipeline(renderPipeline, descriptor.Label);
		}
	}
	
// 	[Throws]
// 	Promise<GPUComputePipeline> createComputePipelineAsync(GPUComputePipelineDescriptor descriptor);
// 	[Throws]
// 	Promise<GPURenderPipeline> createRenderPipelineAsync(GPURenderPipelineDescriptor descriptor);

	public GPUCommandEncoder CreateCommandEncoder()
	{
		var commandEncoder = wgpuDeviceCreateCommandEncoder(_handle, null);
		return new GPUCommandEncoder(commandEncoder, null);
	}

	public GPUCommandEncoder CreateCommandEncoder(GPUCommandEncoderDescriptor descriptor)
	{
		using (descriptor.Label.ToWGPUStringView(out WGPUStringView descriptorLabelPtr))
		{
			var commandEncoderDescriptor = new WGPUCommandEncoderDescriptor() with
			{
				label = descriptorLabelPtr,
			};
			var commandEncoder = wgpuDeviceCreateCommandEncoder(_handle, &commandEncoderDescriptor);

			return new GPUCommandEncoder(commandEncoder, descriptor.Label);
		}
	}

	public GPURenderBundleEncoder CreateRenderBundleEncoder(GPURenderBundleEncoderDescriptor descriptor)
	{
		using (descriptor.Label.ToWGPUStringView(out WGPUStringView descriptorLabelPtr))
		{
			WGPURenderBundleEncoder renderBundleEncoder;
			fixed (WGPUTextureFormat* colorFormatsPtr = descriptor.ColorFormats)
			{
				var renderBundleEncoderDescriptor = new WGPURenderBundleEncoderDescriptor() with
				{
					label = descriptorLabelPtr,
					colorFormatCount = (nuint)descriptor.ColorFormats.Length,
					colorFormats = colorFormatsPtr,
					depthStencilFormat = descriptor.DepthStencilFormat,
					sampleCount = descriptor.SampleCount,
					depthReadOnly = descriptor.DepthReadOnly,
					stencilReadOnly = descriptor.StencilReadOnly
				};
				renderBundleEncoder = wgpuDeviceCreateRenderBundleEncoder(_handle, &renderBundleEncoderDescriptor);
			}

			return new GPURenderBundleEncoder(renderBundleEncoder, descriptor.Label);
		}
	}

// 	[Throws]
	public GPUQuerySet CreateQuerySet(GPUQuerySetDescriptor descriptor)
	{
		using (descriptor.Label.ToWGPUStringView(out WGPUStringView descriptorLabelPtr))
		{
			var querySetDescriptor = new WGPUQuerySetDescriptor() with
			{
				label = descriptorLabelPtr,
				type = descriptor.Type,
				count = descriptor.Count,
			};
			var querySet = wgpuDeviceCreateQuerySet(_handle, &querySetDescriptor);

			return new GPUQuerySet(querySet, descriptor.Label);
		}
	}

	// private PfnErrorCallback _callback;
	// private static event Action<ErrorType, string>? onError;
	// public event Action<ErrorType, string>? OnError
	// {
	// 	add
	// 	{
	// 		if (_callback.Handle == null)
	// 		{
	// 			_callback = new PfnErrorCallback(&ErrorCallback);
	// 			wgpuun
	// 			wgpuDeviceSetUncapturedErrorCallback(_handle, _callback, null);
	// 		}
	// 		onError += value;
	// 	}
	// 	remove => onError -= value;
	// }
	//
	// [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	// public static void ErrorCallback(ErrorType errorType, byte* message, void* userData)
	// {
	// 	onError?.Invoke(errorType, SilkMarshal.PtrToString((IntPtr)message));
	// }
}