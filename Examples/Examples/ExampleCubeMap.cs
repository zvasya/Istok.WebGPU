using System.Numerics;
using System.Runtime.InteropServices;
using AssetManager;
using Examples.Utils;
using Istok.Mathematics;
using Istok.WebGPU;
using Istok.WebGPU.LowLevel;
using Silk.NET.Maths;
using Istok.WebGPU.View;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;
using static Istok.WebGPU.LowLevel.WebGPUNative;

namespace Examples;

public class ExampleCubeMap : ExampleBase
{
	private WGPUTextureFormat depthFormat;
	private GPUTexture? depthTexture;
	private GPUTextureView _depthTextureView;

	GPUTexture cubemapTex;
	GPUTextureView cubemapTexView;
	GPUSampler cubemapTexSampler;

	GPUTexture mainTex;
	GPUTextureView mainTexView;

	private GPUShaderModule _ShaderMain;
	private GPUShaderModule _ShaderSkybox;
	private GPURenderPipeline _PipelineMain;
	private GPURenderPipeline _PipelineSkybox;
	private GPUBuffer _VertexBuffer;
	private ulong _VertexBufferSize;

	private GPUBuffer _PerFrameBuffer;
	GPUBindGroup _SkyboxBindGroup;
	GPUBindGroup _MainBindGroup;

	private DateTime _StartTime;

	public struct Vertex
	{
		public Vertex(Vector3 position, Vector3 normal, Vector2 texCoord)
		{
			Position = position;
			Normal = normal;
			TexCoord = texCoord;
		}

		public Vector3 Position;
		public Vector3 Normal;
		public Vector2 TexCoord;
	}

	static unsafe WGPUVertexAttribute[] vertexAttributes =
	[
		new WGPUVertexAttribute
		{
			format = WGPUVertexFormat.Float32X3,
			offset = 0,
			shaderLocation = 0
		},
		new WGPUVertexAttribute
		{
			format = WGPUVertexFormat.Float32X3,
			offset = (ulong)sizeof(Vector3),
			shaderLocation = 1
		},
		new WGPUVertexAttribute
		{
			format = WGPUVertexFormat.Float32X2,
			offset = (ulong)(sizeof(Vector3) + sizeof(Vector3)),
			shaderLocation = 2
		}
	];

	struct PerFrameData
	{
		public Matrix4x4 model;
		public Matrix4x4 normal;
		public Matrix4x4 view;
		public Matrix4x4 proj;
		public Vector4 cameraPos;
	};

	private static Matrix4x4 CreateNormalMatrix(Matrix4x4 model)
	{
		Matrix4x4.Invert(model, out Matrix4x4 inverseModel);
		return Matrix4x4.Transpose(inverseModel);
	}

	public ExampleCubeMap(GPUDevice device, IWebGpuView window, GPUSurfaceCapabilities surfaceCapabilities, GPUSurface surface, IResourcesProvider resourcesProvider)
		: base(device, window, surfaceCapabilities, surface, resourcesProvider)
	{
		_StartTime = DateTime.UtcNow;
	}

	async Task<GPUShaderModule> LoadShader(string path)
	{
		string shaderCode = await _ResourcesProvider.LoadTextAsync(path);

		if (shaderCode.Length == 0)
			throw new ArgumentException($"Shader source at {path} must not be empty.", nameof(shaderCode));

		GPUShaderModuleDescriptor shaderModuleDescriptor = new GPUShaderModuleDescriptor
		{
			Code = shaderCode
		};

		return _device.CreateShaderModule(shaderModuleDescriptor);
	}

	public override async Task OnLoad()
	{
		_ShaderMain = await LoadShader("Shaders/CubeMapExample/example_cubemap.wgsl");
		_ShaderSkybox = await LoadShader("Shaders/CubeMapExample/skybox.wgsl");

		depthFormat = WGPUTextureFormat.Depth32Float;

		Image<RgbaVector> skybox;
		byte[] skyboxBytes = await _ResourcesProvider.LoadBytesAsync("Textures/piazza_bologni_1k.png");
		skybox = Image.Load<RgbaVector>(skyboxBytes);

		Image<Rgba32> image;
		byte[] logoBytes = await _ResourcesProvider.LoadBytesAsync("Textures/WebGPU_logo.png");
		image = Image.Load<Rgba32>(logoBytes);

		unsafe
		{
			fixed (WGPUVertexAttribute* vertexAttributesPtr = &vertexAttributes[0])
			{
				//Create vertex buffer layout
				Span<WGPUVertexBufferLayout> vertexBufferLayout =
				[
					new WGPUVertexBufferLayout
					{
						attributes = vertexAttributesPtr,
						attributeCount = (UIntPtr)vertexAttributes.Length,
						stepMode = WGPUVertexStepMode.Vertex,
						arrayStride = (ulong)sizeof(Vertex)
					},
				];

				//Create DepthStencilState

				WGPUDepthStencilState depthStencilState = new WGPUDepthStencilState
				{
					format = depthFormat,
					depthWriteEnabled = WGPUOptionalBool.True,
					depthCompare = WGPUCompareFunction.LessEqual,
				};

				{
					//Create pipeline
					GPURenderPipelineDescriptor renderPipelineDescriptor = new GPURenderPipelineDescriptor
					{
						Vertex = new GPUVertexState()
						{
							Module = _ShaderMain,
							EntryPoint = "vs_main",
							Buffers = vertexBufferLayout,
						},
						Fragment = new GPUFragmentState
						{
							Module = _ShaderMain,
							Targets =
							[
								new GPUColorTargetState
								{
									Format = SurfaceCapabilitiesFormat(),
									Blend = GPUBlendState.Opaque,
									WriteMask = WGPUColorWriteMask.All
								}
							],
							EntryPoint = "fs_main"
						},
						DepthStencil = ref depthStencilState,
						Layout = GPUPipelineLayout.Auto
					};

					_PipelineMain = _device.CreateRenderPipeline(renderPipelineDescriptor);
				}
				Console.WriteLine($"Created pipeline {(nuint)_PipelineMain.Handle.Handle:X}");

				{
					//Create SkyBox pipeline
					GPURenderPipelineDescriptor renderPipelineDescriptor = new GPURenderPipelineDescriptor
					{
						Vertex = new GPUVertexState()
						{
							Module = _ShaderSkybox,
							EntryPoint = "vs_main",
						},
						Fragment = new GPUFragmentState
						{
							Module = _ShaderSkybox,
							Targets =
							[
								new GPUColorTargetState
								{
									Format = SurfaceCapabilitiesFormat(),
									Blend = GPUBlendState.Opaque,
									WriteMask = WGPUColorWriteMask.All
								}
							],
							EntryPoint = "fs_main"
						},
						DepthStencil = ref depthStencilState,
						Layout = GPUPipelineLayout.Auto
					};

					_PipelineSkybox = _device.CreateRenderPipeline(renderPipelineDescriptor);
				}
				Console.WriteLine($"Created skybox pipeline {(nuint)_PipelineSkybox.Handle.Handle:X}");
			} //Create pipeline

			{
				//Create per frame buffer
				var descriptor = new GPUBufferDescriptor
				{
					Label = "Buffer: per-frame",
					Size = (ulong)sizeof(PerFrameData),
					Usage = WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst,
					MappedAtCreation = false
				};

				_PerFrameBuffer = _device.CreateBuffer(descriptor);
			} //Create per frame buffer

			//Prepare and upload Skybox cubemap
			{
				var outSkybox = UtilsCubemap.ConvertEquirectangularMapToVerticalCross(skybox);
				var cubemapSkybox = UtilsCubemap.ConvertVerticalCrossToCubeMapFaces(outSkybox);

				var cubemapSkyboxWidth = cubemapSkybox.Width;
				var cubemapSkyboxHeight = cubemapSkybox.Height / 6;
				cubemapTex = _device.CreateTexture(new GPUTextureDescriptor
				{
					Label = "piazza_bologni_1k",
					Usage = WGPUTextureUsage.TextureBinding | WGPUTextureUsage.CopyDst,
					Dimension = WGPUTextureDimension.D2D,
					Size = new WGPUExtent3D() { width = (uint)cubemapSkyboxWidth, height = (uint)(cubemapSkyboxHeight), depthOrArrayLayers = 6 },
					Format = WGPUTextureFormat.RGBA16Float,
				});
				cubemapTexView = cubemapTex.CreateView(new GPUTextureViewDescriptor
				{
					Label = "piazza_bologni_1k_view",
					Format = cubemapTex.Format,
					Dimension = WGPUTextureViewDimension.Cube,
					Usage = WGPUTextureUsage.TextureBinding
				});
				cubemapTexSampler = _device.CreateSampler(new GPUSamplerDescriptor
				{
					Compare = WGPUCompareFunction.Undefined,
					MipmapFilter = WGPUMipmapFilterMode.Linear,
					MagFilter = WGPUFilterMode.Linear,
					MinFilter = WGPUFilterMode.Linear,
					MaxAnisotropy = 1
				});

				uint bytesPerPixel = cubemapTex.Format.ElementSize();
				uint bytesPerRow = (uint)(cubemapSkyboxWidth * bytesPerPixel);

				IMemoryGroup<RgbaVector> pixelMemoryGroup = cubemapSkybox.GetPixelMemoryGroup();
				RgbaVector[] destination1 = new RgbaVector[pixelMemoryGroup.TotalLength];
				cubemapSkybox.CopyPixelDataTo(destination1);
				Silk.NET.Maths.Vector4D<Half>[] destination = new Silk.NET.Maths.Vector4D<Half>[pixelMemoryGroup.TotalLength];

				for (int i = 0; i < destination1.Length; i++)
				{
					RgbaVector rgbaVector = destination1[i];
					destination[i] = new Vector4D<Half>((Half)rgbaVector.R, (Half)rgbaVector.G, (Half)rgbaVector.B, (Half)rgbaVector.A);
				}

				ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(destination);
				fixed (byte* bytesPtr = bytes)
				{
					_device.Queue.WriteTexture(
						new WGPUTexelCopyTextureInfo()
						{
							texture = cubemapTex,
							mipLevel = 0,
							origin = new WGPUOrigin3D(),
							aspect = WGPUTextureAspect.All
						},
						bytesPtr,
						(UIntPtr)bytes.Length,
						new WGPUTexelCopyBufferLayout()
						{
							offset = 0,
							bytesPerRow = bytesPerRow,
							rowsPerImage = (uint)cubemapSkyboxHeight,
						},
						new WGPUExtent3D() { width = (uint)cubemapSkyboxWidth, height = (uint)cubemapSkyboxHeight, depthOrArrayLayers = 6 }
					);
				}
			} //Prepare and upload Skybox cubemap

			//Prepare and upload main texture
			{
				mainTex = _device.CreateTexture(new GPUTextureDescriptor
				{
					Label = "main_tex",
					Usage = WGPUTextureUsage.TextureBinding | WGPUTextureUsage.CopyDst,
					Dimension = WGPUTextureDimension.D2D,
					Size = new WGPUExtent3D() { width = (uint)image.Width, height = (uint)(image.Height), depthOrArrayLayers = 1 },
					Format = WGPUTextureFormat.RGBA8Unorm,
				});
				mainTexView = mainTex.CreateView();

				Rgba32[] imageData = new Rgba32[image.Width * image.Height];
				image.CopyPixelDataTo(imageData);

				uint bytesPerPixel = mainTex.Format.ElementSize();
				uint bytesPerRow = (uint)(image.Width * bytesPerPixel);

				ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(imageData);
				fixed (byte* bytesPtr = bytes)
				{
					_device.Queue.WriteTexture(
						new WGPUTexelCopyTextureInfo()
						{
							texture = mainTex,
							mipLevel = 0,
							origin = new WGPUOrigin3D(),
							aspect = WGPUTextureAspect.All
						},
						bytesPtr,
						(UIntPtr)bytes.Length,
						new WGPUTexelCopyBufferLayout()
						{
							offset = 0,
							bytesPerRow = bytesPerRow,
							rowsPerImage = (uint)image.Height,
						},
						new WGPUExtent3D() { width = (uint)image.Width, height = (uint)image.Height, depthOrArrayLayers = 1 }
					);
				}
			} //Prepare and upload main texture

			{
				//Create bind group for skybox
				Span<WGPUBindGroupEntry> bindGroupEntries = stackalloc WGPUBindGroupEntry[3]
				{
					new WGPUBindGroupEntry
					{
						binding = 0,
						buffer = _PerFrameBuffer,
						size = _PerFrameBuffer.Size,
					},
					new WGPUBindGroupEntry
					{
						binding = 1,
						textureView = cubemapTexView
					},
					new WGPUBindGroupEntry
					{
						binding = 2,
						sampler = cubemapTexSampler
					}
				};

				GPUBindGroupDescriptor descriptor = new GPUBindGroupDescriptor
				{
					Entries = bindGroupEntries,
					Layout = _PipelineSkybox.GetBindGroupLayout(0)
				};

				_SkyboxBindGroup = _device.CreateBindGroup(descriptor);
			} //Create bind group for skybox

			{
				//Create bind group for main 
				Span<WGPUBindGroupEntry> bindGroupEntries = stackalloc WGPUBindGroupEntry[4]
				{
					new WGPUBindGroupEntry
					{
						binding = 0,
						buffer = _PerFrameBuffer,
						size = _PerFrameBuffer.Size,
					},
					new WGPUBindGroupEntry
					{
						binding = 1,
						textureView = mainTexView
					},
					new WGPUBindGroupEntry
					{
						binding = 2,
						textureView = cubemapTexView
					},
					new WGPUBindGroupEntry
					{
						binding = 3,
						sampler = cubemapTexSampler
					}
				};

				GPUBindGroupDescriptor descriptor = new GPUBindGroupDescriptor
				{
					Entries = bindGroupEntries,
					Layout = _PipelineMain.GetBindGroupLayout(0)
				};

				_MainBindGroup = _device.CreateBindGroup(descriptor);
			} //Create bind group for main 

			{
				//Create vertex buffer
				Span<Vertex> data = stackalloc Vertex[36];
				var descriptor = new GPUBufferDescriptor
				{
					Size = _VertexBufferSize = (ulong)(sizeof(Vertex) * data.Length),
					Usage = WGPUBufferUsage.Vertex | WGPUBufferUsage.CopyDst
				};

				_VertexBuffer = _device.CreateBuffer(descriptor);
				Console.WriteLine($"Created VertexBuffer {(nuint)_VertexBuffer.Handle.Handle:X}");
				//Get a queue
				var queue = _device.GetQueue();


				const float halfSize = 0.5f;

				Vector3 up = Vector3.UnitY;
				Vector3 down = -Vector3.UnitY;
				Vector3 forward = Vector3.UnitZ;
				Vector3 backward = -Vector3.UnitZ;
				Vector3 right = Vector3.UnitX;
				Vector3 left = -Vector3.UnitX;

// Front (+Z)
				data[0] = new Vertex(new Vector3(-halfSize, -halfSize, halfSize), forward, new Vector2(0, 1)); // Top left
				data[1] = new Vertex(new Vector3(halfSize, -halfSize, halfSize), forward, new Vector2(1, 1)); // Top right
				data[2] = new Vertex(new Vector3(halfSize, halfSize, halfSize), forward, new Vector2(1, 0)); // Bottom right
				data[3] = new Vertex(new Vector3(-halfSize, -halfSize, halfSize), forward, new Vector2(0, 1)); // Top left
				data[4] = new Vertex(new Vector3(halfSize, halfSize, halfSize), forward, new Vector2(1, 0)); // Bottom right
				data[5] = new Vertex(new Vector3(-halfSize, halfSize, halfSize), forward, new Vector2(0, 0)); // Bottom left

// Back (-Z)
				data[6] = new Vertex(new Vector3(halfSize, -halfSize, -halfSize), backward, new Vector2(0, 1));
				data[7] = new Vertex(new Vector3(-halfSize, -halfSize, -halfSize), backward, new Vector2(1, 1));
				data[8] = new Vertex(new Vector3(-halfSize, halfSize, -halfSize), backward, new Vector2(1, 0));
				data[9] = new Vertex(new Vector3(halfSize, -halfSize, -halfSize), backward, new Vector2(0, 1));
				data[10] = new Vertex(new Vector3(-halfSize, halfSize, -halfSize), backward, new Vector2(1, 0));
				data[11] = new Vertex(new Vector3(halfSize, halfSize, -halfSize), backward, new Vector2(0, 0));

// Top (+Y)
				data[12] = new Vertex(new Vector3(-halfSize, halfSize, halfSize), up, new Vector2(0, 1));
				data[13] = new Vertex(new Vector3(halfSize, halfSize, halfSize), up, new Vector2(1, 1));
				data[14] = new Vertex(new Vector3(halfSize, halfSize, -halfSize), up, new Vector2(1, 0));
				data[15] = new Vertex(new Vector3(-halfSize, halfSize, halfSize), up, new Vector2(0, 1));
				data[16] = new Vertex(new Vector3(halfSize, halfSize, -halfSize), up, new Vector2(1, 0));
				data[17] = new Vertex(new Vector3(-halfSize, halfSize, -halfSize), up, new Vector2(0, 0));

// Bottom (-Y)
				data[18] = new Vertex(new Vector3(-halfSize, -halfSize, -halfSize), down, new Vector2(0, 1));
				data[19] = new Vertex(new Vector3(halfSize, -halfSize, -halfSize), down, new Vector2(1, 1));
				data[20] = new Vertex(new Vector3(halfSize, -halfSize, halfSize), down, new Vector2(1, 0));
				data[21] = new Vertex(new Vector3(-halfSize, -halfSize, -halfSize), down, new Vector2(0, 1));
				data[22] = new Vertex(new Vector3(halfSize, -halfSize, halfSize), down, new Vector2(1, 0));
				data[23] = new Vertex(new Vector3(-halfSize, -halfSize, halfSize), down, new Vector2(0, 0));

// Right (+X)
				data[24] = new Vertex(new Vector3(halfSize, -halfSize, halfSize), right, new Vector2(0, 1));
				data[25] = new Vertex(new Vector3(halfSize, -halfSize, -halfSize), right, new Vector2(1, 1));
				data[26] = new Vertex(new Vector3(halfSize, halfSize, -halfSize), right, new Vector2(1, 0));
				data[27] = new Vertex(new Vector3(halfSize, -halfSize, halfSize), right, new Vector2(0, 1));
				data[28] = new Vertex(new Vector3(halfSize, halfSize, -halfSize), right, new Vector2(1, 0));
				data[29] = new Vertex(new Vector3(halfSize, halfSize, halfSize), right, new Vector2(0, 0));

// Left (-X)
				data[30] = new Vertex(new Vector3(-halfSize, -halfSize, -halfSize), left, new Vector2(0, 1));
				data[31] = new Vertex(new Vector3(-halfSize, -halfSize, halfSize), left, new Vector2(1, 1));
				data[32] = new Vertex(new Vector3(-halfSize, halfSize, halfSize), left, new Vector2(1, 0));
				data[33] = new Vertex(new Vector3(-halfSize, -halfSize, -halfSize), left, new Vector2(0, 1));
				data[34] = new Vertex(new Vector3(-halfSize, halfSize, halfSize), left, new Vector2(1, 0));
				data[35] = new Vertex(new Vector3(-halfSize, halfSize, -halfSize), left, new Vector2(0, 0));
				
				fixed (Vertex* vertexPtr = data)
				{
					queue.WriteBuffer(_VertexBuffer, 0, vertexPtr, (nuint)_VertexBufferSize);
				}
				Console.WriteLine($"VertexBuffer filled");
			} //Create vertex buffer

			CreateSwapchain();
		}
	}

	private WGPUTextureFormat SurfaceCapabilitiesFormat()
	{
		return _SurfaceCapabilities.Formats.FirstOrDefault(format => !format.IsSRGB(), _SurfaceCapabilities.Formats[0]);
	}

	private void CreateSwapchain()
	{
		int w = 0;
		int h = 0;
		(w, h) = GetFramebufferSizeInPixel();
		Console.WriteLine($"GetFramebufferSizeInPixel {w}, {h}");
		var surfaceConfiguration = new GPUSurfaceConfiguration
		{
			Usage = WGPUTextureUsage.RenderAttachment,
			Device = _device,
			Format = SurfaceCapabilitiesFormat(),
			// PresentMode = PresentMode.FifoRelaxed,
			PresentMode = WGPUPresentMode.Fifo,
			AlphaMode = _SurfaceCapabilities.AlphaModes[0],
			Width = (uint)w,
			Height = (uint)h,
		};

		_Surface.Configure(surfaceConfiguration);
		Console.WriteLine($"Surface Configured");

		depthTexture?.Dispose();
		depthTexture = _device.CreateTexture(new GPUTextureDescriptor
			{
				Label = "Depth buffer",
				Usage = WGPUTextureUsage.RenderAttachment,
				Dimension = WGPUTextureDimension.D2D,
				Size = new WGPUExtent3D { width = surfaceConfiguration.Width, height = surfaceConfiguration.Height, depthOrArrayLayers = 1 },
				Format = WGPUTextureFormat.Depth32Float,
			}
		);

		_depthTextureView = depthTexture.CreateView();
	}

	private double totalTime = 0;

	public override unsafe void WindowOnRender(double delta)
	{
		totalTime += delta;
		WGPUSurfaceTexture surfaceTexture = _Surface.GetCurrentTexture();
		switch (surfaceTexture.status)
		{
			case WGPUSurfaceGetCurrentTextureStatus.SuccessOptimal:
			case WGPUSurfaceGetCurrentTextureStatus.SuccessSuboptimal:
				break;

			case WGPUSurfaceGetCurrentTextureStatus.Timeout:
			case WGPUSurfaceGetCurrentTextureStatus.Outdated:
			case WGPUSurfaceGetCurrentTextureStatus.Lost:
				// Recreate swapchain,
				wgpuTextureRelease(surfaceTexture.texture);
				CreateSwapchain();
				// Skip this frame
				return;
			case WGPUSurfaceGetCurrentTextureStatus.Error:
			default:
				// Recreate swapchain,
				// wgpuTextureRelease(surfaceTexture.texture);
				// CreateSwapchain();
				// Skip this frame
				return;
		}

		// UpdateProjectionMatrix();
		WGPUTextureViewDescriptor textureViewDescriptor = new WGPUTextureViewDescriptor
		{
			label = WGPUStringView.Empty,
			baseMipLevel = 0,
			mipLevelCount = MipLevelCountUndefined,
			baseArrayLayer = 0,
			arrayLayerCount = ArrayLayerCountUndefined,
			aspect = WGPUTextureAspect.All,
			usage = WGPUTextureUsage.RenderAttachment
		};

		var currentTexture = wgpuTextureCreateView(surfaceTexture.texture, &textureViewDescriptor);

		var encoder = _device.CreateCommandEncoder();

		var queue = _device.GetQueue();

		float ratio = depthTexture!.Width / (float)depthTexture.Height;

		Vector3 cameraPos = new Vector3(0.0f, 1.0f / 2f, -1.5f) * 1.5f;
		cameraPos = Vector3.Transform(cameraPos, Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)MathExt.Radians(totalTime * 5.0)));

		Matrix4x4 p = Matrix4x4.CreatePerspectiveFieldOfView(MathExt.Radians(60.0f), ratio, 0.1f, 10000.0f);
		// Matrix4x4 m1 = Matrix4x4.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), MathExt.Radians(-90f)));
		Matrix4x4 m1 = Matrix4x4.Identity;
		Matrix4x4 m2 = Matrix4x4.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), (float)totalTime));
		// Matrix4x4 m2 = Matrix4x4.Identity;
		Matrix4x4 model = m2 * m1;
		Matrix4x4 v = Matrix4x4.CreateLookAt(cameraPos, new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0));

		var colorAttachment = new WGPURenderPassColorAttachment
		{
			view = currentTexture,
			resolveTarget = WGPUTextureView.Null,
			loadOp = WGPULoadOp.Clear,
			storeOp = WGPUStoreOp.Store,
			clearValue = new WGPUColor
			{
				r = 1,
				g = 1,
				b = 1,
				a = 1
			},
			depthSlice = DepthSliceUndefined
		};

		WGPURenderPassDepthStencilAttachment wgpuRenderPassDepthStencilAttachment = new WGPURenderPassDepthStencilAttachment
		{
			view = _depthTextureView,
			depthLoadOp = WGPULoadOp.Clear,
			depthStoreOp = WGPUStoreOp.Store,
			depthClearValue = 1,
			depthReadOnly = false,
		};
		var renderPassDescriptor = new GPURenderPassDescriptor
		{
			ColorAttachments = [colorAttachment],
			DepthStencilAttachment = ref wgpuRenderPassDepthStencilAttachment,
		};

		var renderPass = encoder.BeginRenderPass(renderPassDescriptor);

		var perFrameData = new PerFrameData()
		{
			model = model,
			normal = CreateNormalMatrix(model),
			view = v,
			proj = p,
			cameraPos = new Vector4(cameraPos, 1.0f),
		};

		queue.WriteBuffer(_PerFrameBuffer, 0, &perFrameData, (UIntPtr)sizeof(PerFrameData));

		renderPass.SetPipeline(_PipelineSkybox);
		renderPass.SetBindGroup(0, _SkyboxBindGroup);

		renderPass.Draw(36, 1, 0, 0);

		renderPass.SetPipeline(_PipelineMain);
		renderPass.SetBindGroup(0, _MainBindGroup);
		renderPass.SetVertexBuffer(0, _VertexBuffer, 0, _VertexBufferSize);
		renderPass.Draw(36, 1, 0, 0);

		renderPass.End();
		renderPass.Dispose();

		var commandBuffer = encoder.Finish();
		encoder.Dispose();
		queue.Submit(commandBuffer);
		_Surface.Present();
		_Window.SwapBuffers();

		commandBuffer.Dispose();
		wgpuTextureViewRelease(currentTexture);
		wgpuTextureRelease(surfaceTexture.texture);
	}

	public override void FramebufferResize(Vector2D<int> size)
	{
		CreateSwapchain();
	}

	public override void Dispose()
	{
		_PipelineMain.Dispose();
		_PipelineSkybox.Dispose();
		_ShaderMain.Dispose();
		_ShaderSkybox.Dispose();
		depthTexture?.Dispose();
	}
}