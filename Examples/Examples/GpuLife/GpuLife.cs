using System.Numerics;
using System.Runtime.CompilerServices;
using AssetManager;
using CommunityToolkit.HighPerformance;
using Istok.WebGPU;
using Istok.WebGPU.LowLevel;
using Silk.NET.Maths;
using Istok.WebGPU.View;
using static Istok.WebGPU.LowLevel.WebGPUNative;

namespace Examples.GpuLife;

public class GpuLife : ExampleBase
{
	public GpuLife(GPUDevice device, IWebGpuView window, GPUSurfaceCapabilities surfaceCapabilities, GPUSurface surface, IResourcesProvider resourcesProvider) : base(device, window, surfaceCapabilities, surface, resourcesProvider)
	{
	}

	Input input = new Input();

	int colourAmt;
	Vector3[] colours;

	float[,] matrix;

	const float multistep = 1;
	uint particleAmt = Options.ParticleAmt;

	private LinkedListEngine _engine;
	GPURenderPipeline renderPipeline;
	GPUBuffer uniformBuffer;
	GPUBuffer simBuffer;
	GPUBuffer cameraBuffer;
	private GPUBuffer matrixBuffer;
	GPUBuffer colourBuffer;
	GPUBindGroup renderBindGroup;

	float[,] MakeRandomMatrix()
	{
		float[,] rows = new float[colourAmt,colourAmt];
		for (var i = 0; i < colourAmt; i++)
		{
			for (int j = 0; j < colourAmt; j++)
			{
				rows[i,j] = (Random.Shared.NextSingle() * 2f - 1f);
			}
		}

		return rows;
	}

	public override async Task OnLoad()
	{
		string renderShaders = await _ResourcesProvider.LoadTextAsync("Shaders/example_gpulife_render.wgsl");

		colourAmt = Options.ColourAmt;
		colours = new Vector3[colourAmt];
		for (int i = 0; i < colourAmt; i++)
		{
			colours[i] = Utils.HslToRgb((float)i / colourAmt * 360f, 1f, 0.5f);
		}

		matrix = MakeRandomMatrix();

		uniformBuffer = _device.CreateBuffer(new GPUBufferDescriptor()
		{
			Size = (ulong)(Unsafe.SizeOf<Uniforms>()),
			Usage = WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst,
			Label = "uniformBuffer",
		});

		simBuffer = _device.CreateBuffer(new GPUBufferDescriptor
		{
			Size = (ulong)Unsafe.SizeOf<Sim>(),
			Usage = WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst,
			Label = "simBuffer",
		});

		cameraBuffer = _device.CreateBuffer(new GPUBufferDescriptor()
		{
			Size = (ulong)Unsafe.SizeOf<Input.Tcamera>(),
			Usage = WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst,
			Label = "cameraBuffer",
		});
		_engine = new LinkedListEngine();
		await _engine.Setup(_device, _ResourcesProvider);


		GPUShaderModule renderModule = _device.CreateShaderModule(new GPUShaderModuleDescriptor() { Code = renderShaders });

		unsafe
		{
			const int vertexAttributesCount = 3;
			WGPUVertexAttribute* vertexAttributes = stackalloc WGPUVertexAttribute[vertexAttributesCount]
			{
				new WGPUVertexAttribute { shaderLocation = 0, offset = 0, format = WGPUVertexFormat.Float32X2 },
				new WGPUVertexAttribute { shaderLocation = 1, offset = 8, format = WGPUVertexFormat.Float32X2 },
				new WGPUVertexAttribute { shaderLocation = 2, offset = 16, format = WGPUVertexFormat.Float32 }
			};
			uint particleStride = (uint)Unsafe.SizeOf<Particle>();

			renderPipeline = _device.CreateRenderPipeline(new GPURenderPipelineDescriptor()
			{
				Layout = GPUPipelineLayout.Auto,
				Vertex = new GPUVertexState()
				{
					Module = renderModule,
					EntryPoint = "vertex",
					Buffers =
					[
						new WGPUVertexBufferLayout()
						{
							arrayStride = particleStride,
							stepMode = WGPUVertexStepMode.Instance,
							attributes = vertexAttributes,
							attributeCount = vertexAttributesCount,
						},
					],
				},
				Fragment = new GPUFragmentState()
				{
					Module = renderModule,
					EntryPoint = "fragment",
					Targets =
					[
						new GPUColorTargetState
						{
							Format = _SurfaceCapabilities.Formats[0],
							Blend = new GPUBlendState()
							{
								Color = new GPUBlendComponent()
								{
									SrcFactor = WGPUBlendFactor.SrcAlpha,
									DstFactor = WGPUBlendFactor.OneMinusSrcAlpha,
									Operation = WGPUBlendOperation.Add,
								},
								Alpha = new GPUBlendComponent()
								{
									SrcFactor = WGPUBlendFactor.One,
									DstFactor = WGPUBlendFactor.Zero,
									Operation = WGPUBlendOperation.Add,
								},
							},
						},
					],
				},
				Primitive = new GPUPrimitiveState()
				{
					Topology = WGPUPrimitiveTopology.TriangleStrip,
				},
			});
		}

		// setupTimestamp(device, 'render');

		StartParticles();

		// Options.BindOptions(device, simData, simBuffer);

		CreateSwapchain();
	}

	private int alternate = 0;
	private GPUDevice device => _device;

	public void Tick(GPUCommandEncoder commandEncoder)
	{
		if (_device?.Handle == default)
			return;

		_engine.Tick(_device, commandEncoder, alternate, particleAmt);
		alternate = (alternate + 1) % 2;
	}

	void render(GPUTextureView contextTextureView, GPUCommandEncoder commandEncoder)
	{
		// if (renderPipeline?.Handle == default || particleBuffers != default || device.Handle != default) return;

		var renderPassDescriptor = new GPURenderPassDescriptor()
		{
			ColorAttachments =
			[
				new WGPURenderPassColorAttachment()
				{
					view = contextTextureView,
					clearValue = new WGPUColor() { r = 0, g = 0, b = 0, a = 0 },
					loadOp = WGPULoadOp.Clear,
					storeOp = WGPUStoreOp.Store,
				},
			],
		};

		var passEncoder = commandEncoder.BeginRenderPass(renderPassDescriptor
			// linkRenderTimestamp(device, renderPassDescriptor, 'render'),
		);
		passEncoder.SetPipeline(renderPipeline);
		passEncoder.SetVertexBuffer(0, particleBuffers[(alternate + 1) % 2]);
		passEncoder.SetBindGroup(0, renderBindGroup);
		passEncoder.Draw(6, particleAmt, 0, 0);
		passEncoder.End();

		// resolveTimestamp(commandEncoder, 'render');
	}

	private LinkedListEngine.Pair<GPUBuffer> particleBuffers;

	private void StartParticles()
	{
		if (
			device.Handle == default ||
			uniformBuffer.Handle == default ||
			renderPipeline.Handle == default ||
			simBuffer.Handle == default ||
			cameraBuffer.Handle == default
		)
			return;

		uint sizeOfParticle = (uint)Unsafe.SizeOf<Particle>();
		
		uint bufferSize = particleAmt * sizeOfParticle;
		particleBuffers = new LinkedListEngine.Pair<GPUBuffer>(
			device.CreateBuffer(
				new GPUBufferDescriptor()
				{
					Size = bufferSize,
					Usage =
						WGPUBufferUsage.Storage |
						WGPUBufferUsage.Vertex |
						WGPUBufferUsage.CopyDst,
				}),
			device.CreateBuffer(new GPUBufferDescriptor()
			{
				Size = bufferSize,
				Usage =
					WGPUBufferUsage.Storage |
					WGPUBufferUsage.Vertex |
					WGPUBufferUsage.CopyDst,
			})
		);

		alternate = 0;
		Span<Particle> data = new Particle[(int)particleAmt];
		var pi = 0;
		while (pi < particleAmt)
		{
			float spawnAmt = ((Random.Shared.NextSingle() * (particleAmt - pi)) / colourAmt) * 5f;
			float c = Random.Shared.Next(0, colourAmt);

			float a = Random.Shared.NextSingle() * MathF.PI * 2f;
			float d = Random.Shared.NextSingle() * Options.OptionParams.WorldSize * 0.9f;

			float x = MathF.Cos(a) * d;
			float y = MathF.Sin(a) * d;
			for (int i = 0; i < spawnAmt; i++)
			{
				a = Random.Shared.NextSingle() * MathF.PI * 2f;
				d = (MathF.Pow(Random.Shared.NextSingle(), 3f) / 10f) * Options.OptionParams.WorldSize;
				data[pi] = new Particle
				{
					pos = new Vector2(x + MathF.Cos(a) * d, y + MathF.Sin(a) * d),
					vel = Vector2.Zero,
					colour = c
				};
				pi++;
			}
		}

		unsafe
		{
			fixed (void* dataPtr = data)
			{
				device.Queue.WriteBuffer(particleBuffers[0], 0, dataPtr, (UIntPtr)data.Length * sizeOfParticle);
			}
		}

		Options.SetSim(device, simBuffer);

		matrixBuffer = device.CreateBuffer(new GPUBufferDescriptor()
		{
			Size = (ulong)(colourAmt * colourAmt * 4),
			Usage = WGPUBufferUsage.Storage | WGPUBufferUsage.CopyDst,
		});

		Span<float> matrixData = matrix.AsSpan();

		unsafe
		{
			fixed (float* dataPtr = matrixData)
			{
				device.Queue.WriteBuffer(matrixBuffer, 0, dataPtr, (UIntPtr)(matrixData.Length * sizeof(float)));
			}
		}

		colourBuffer = device.CreateBuffer(new GPUBufferDescriptor()
		{
			Size = (ulong)(colourAmt * Unsafe.SizeOf<Vector3>()),
			Usage = WGPUBufferUsage.Storage | WGPUBufferUsage.CopyDst,
		});

		unsafe
		{
			fixed (Vector3* dataPtr = colours)
				device.Queue.WriteBuffer(colourBuffer, 0, dataPtr, (UIntPtr)(colours.Length * sizeof(Vector3)));
		}

		_engine.Start(
			device,
			uniformBuffer,
			simBuffer,
			matrixBuffer,
			particleBuffers,
			particleAmt,
			Options.Params.Cells
		);

		renderBindGroup = device.CreateBindGroup(new GPUBindGroupDescriptor()
		{
			Layout = renderPipeline.GetBindGroupLayout(0),
			Entries =
			[
				new WGPUBindGroupEntry()
				{
					binding = 0,
					buffer = uniformBuffer,
					size = uniformBuffer.Size,
				},
				new WGPUBindGroupEntry()
				{
					binding = 1,
					buffer = cameraBuffer,
					size = cameraBuffer.Size
				},

				new WGPUBindGroupEntry()
				{
					binding = 2,
					buffer = colourBuffer,
					size = colourBuffer.Size,
				},
			],
		});
	}

	// double lastTime = 0;
// const deltaVs: number[] = [];

	void Update(double delta, uint width, uint height, GPUTextureView textureView)
	{
		// requestAnimationFrame(update);
		// if (device.Handle  || !context) return;

		// var start = lastTime + delta;
		// const delta = (start - lastTime) / 1000;
		// deltaVs.push(start - lastTime);
		// if (deltaVs.length > 1000) {
		//   deltaVs.splice(0, 1);
		// }
		// lastTime = start;

		// camera.x = lerp5(camera.x, tcamera.x, delta * 50);
		// camera.y = lerp5(camera.y, tcamera.y, delta * 50);
		// camera.zoom = lerp5(camera.zoom, tcamera.zoom, delta * 50);

		var commandEncoder = device.CreateCommandEncoder();

		if (uniformBuffer.Handle != default)
		{
			Uniforms uniformData = new Uniforms()
			{
				aspect = (float)width / height,
				mouse = input.mouse,
				size = (1f / Options.OptionParams.R) * 0.015f,
			};

			unsafe
			{
				device.Queue.WriteBuffer(uniformBuffer, 0, &uniformData, (UIntPtr)(sizeof(Uniforms)));
			}
		}

		if (cameraBuffer.Handle != default)
		{
			unsafe
			{
				Input.Tcamera cameraData = input.camera;
				device.Queue.WriteBuffer(cameraBuffer, 0, &cameraData, (UIntPtr)sizeof(Input.Tcamera));
			}
		}

		for (var i = 0; i < multistep; i++)
		{
			Tick(commandEncoder);
		}

		render(textureView, commandEncoder);

		GPUCommandBuffer commands = commandEncoder.Finish();
		device.Queue.Submit(commands);

		// device.PopErrorScope().then((error) => {
		//   if (error) {
		//     // some weird bug happened with timestamps, just disable it and restart the simulation
		//     window.location.href +=
		//       (window.location.search ? '&' : '?') + 'noTimestamp';
		//   }
		// });

		// const cpuTime = performance.now() - start;
		// globalPerformanceParams.cpu = cpuTime;
		// updateTotal();

		// for (const engine2 in engines) {
		// if (engine == engine2) {
		//   engines[engine2].updateDisplays(
		//     performanceParams[engine2 as keyof typeof performanceParams],
		//   );
		// }
		// }

		// readTimestamp('render').then((time) => {
		//   globalPerformanceParams.render = time;
		//   updateTotal();
		// });

		// fpsc++;
	}

	public override void WindowOnRender(double delta)
	{
		WGPUSurfaceTexture currentSurfaceTexture = _Surface.GetCurrentTexture();
		switch (currentSurfaceTexture.status)
		{
			case WGPUSurfaceGetCurrentTextureStatus.SuccessOptimal:
			case WGPUSurfaceGetCurrentTextureStatus.SuccessSuboptimal:
				break;

			case WGPUSurfaceGetCurrentTextureStatus.Timeout:
			case WGPUSurfaceGetCurrentTextureStatus.Outdated:
			case WGPUSurfaceGetCurrentTextureStatus.Lost:
				// Recreate swapchain,
				wgpuTextureRelease(currentSurfaceTexture.texture);
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

		var surfaceTexture = new GPUTexture(currentSurfaceTexture.texture, null);

		var textureView = surfaceTexture.CreateView();

		Update(delta, surfaceTexture.Width, surfaceTexture.Height, textureView);
		_Surface.Present();
		_Window.SwapBuffers();
	}

	public override void FramebufferResize(Vector2D<int> size)
	{
		CreateSwapchain();
	}

	public override void Dispose()
	{
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
			Format = _SurfaceCapabilities.Formats[0],
			// PresentMode = PresentMode.FifoRelaxed,
			PresentMode = WGPUPresentMode.Fifo,
			AlphaMode = _SurfaceCapabilities.AlphaModes[0],
			Width = (uint)w,
			Height = (uint)h,
		};

		_Surface.Configure(surfaceConfiguration);
		Console.WriteLine($"Surface Configured");
	}
}