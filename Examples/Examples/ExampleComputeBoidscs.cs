using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AssetManager;
using Istok.WebGPU;
using Istok.WebGPU.LowLevel;
using Silk.NET.Maths;
using Istok.WebGPU.View;
using static Istok.WebGPU.LowLevel.WebGPUNative;

namespace Examples;

public class ExampleComputeBoids : ExampleBase
{
    const int numParticles = 1500;
    private GPUShaderModule   spriteShaderModule;
    private GPUShaderModule   computeShaderModule;
    private GPURenderPipeline renderPipeline;
    private GPUComputePipeline computePipeline;
    private GPUBuffer spriteVertexBuffer;
    private GPUBuffer simParamBuffer;
    private SimParams simParams;

    private GPUBuffer[] particleBuffers;
    private GPUBindGroup[] particleBindGroups;
    
    int t = 0;
    int computePassDurationSum = 0;
    int renderPassDurationSum = 0;
    int timerSamples = 0;

    [StructLayout(LayoutKind.Sequential)]
    struct SimParams
    {
        public float deltaT;
        public float rule1Distance;
        public float rule2Distance;
        public float rule3Distance;
        public float rule1Scale;
        public float rule2Scale;
        public float rule3Scale;
    }

    public ExampleComputeBoids(GPUDevice device, IWebGpuView window, GPUSurfaceCapabilities surfaceCapabilities, GPUSurface surface, IResourcesProvider resourcesProvider) : base(device, window, surfaceCapabilities, surface, resourcesProvider)
    {
        
    }

    public override async Task OnLoad()
    {
        { //Load shader
            string spriteWGSL = await _ResourcesProvider.LoadTextAsync("Shaders/example_compute_boids_sprite_shader.wgsl");
            if (spriteWGSL.Length == 0)
                throw new ArgumentException("Shader source must not be empty.", nameof(spriteWGSL));
            string updateSpritesWGSL = await _ResourcesProvider.LoadTextAsync("Shaders/example_compute_boids_update_sprite_shader.wgsl");
            if (updateSpritesWGSL.Length == 0)
                throw new ArgumentException("Shader source must not be empty.", nameof(updateSpritesWGSL));


            spriteShaderModule = _device.CreateShaderModule(new GPUShaderModuleDescriptor
            {
                Code = spriteWGSL
            });
            
            computeShaderModule = _device.CreateShaderModule(new GPUShaderModuleDescriptor
            {
                Code = updateSpritesWGSL
            });

            Console.WriteLine($"Created shader {(nuint) spriteShaderModule.Handle.Handle:X}");
        } //Load shader

        unsafe
        {
            WGPUTextureFormat presentationFormat = _SurfaceCapabilities.Formats[0];
            //create renderPipeline
            { 
                WGPUVertexAttribute* instancedParticleBufferAttributes = stackalloc WGPUVertexAttribute[]
                {
                    // instance position
                    new WGPUVertexAttribute
                    {
                        shaderLocation = 0,
                        offset = 0,
                        format = WGPUVertexFormat.Float32X2,
                    },
                    // instance velocity
                    new WGPUVertexAttribute
                    {
                        shaderLocation = 1,
                        offset = 2 * 4,
                        format = WGPUVertexFormat.Float32X2,
                    },
                };

                WGPUVertexAttribute* vertexBufferAttributes = stackalloc WGPUVertexAttribute[]
                {
                    // vertex positions
                    new WGPUVertexAttribute
                    {
                        shaderLocation = 2,
                        offset = 0,
                        format = WGPUVertexFormat.Float32X2,
                    }
                };


                renderPipeline = _device.CreateRenderPipeline(new GPURenderPipelineDescriptor()
                    {
                        Layout = GPUPipelineLayout.Auto,
                        Vertex = new GPUVertexState
                        {
                            Module = spriteShaderModule,
                            Buffers =
                            [
                                // instanced particles buffer
                                new WGPUVertexBufferLayout
                                {
                                    arrayStride = 4 * 4,
                                    stepMode = WGPUVertexStepMode.Instance,
                                    attributes = instancedParticleBufferAttributes,
                                    attributeCount = 2,
                                },
                                // vertex buffer
                                new WGPUVertexBufferLayout
                                {
                                    arrayStride = 2 * 4,
                                    stepMode = WGPUVertexStepMode.Vertex,
                                    attributes = vertexBufferAttributes,
                                    attributeCount = 1,
                                },
                            ],
                        },
                        Fragment = new GPUFragmentState
                        {
                            Module = spriteShaderModule,
                            Targets = [new GPUColorTargetState
                            {
                                Format = presentationFormat,
                            }]
                        },
                        Primitive = new GPUPrimitiveState()
                        {
                            Topology = WGPUPrimitiveTopology.TriangleList
                        }
                    }
                    );
            }
            //create computePipeline
            {
                computePipeline = _device.CreateComputePipeline(new GPUComputePipelineDescriptor
                {
                    Layout = GPUPipelineLayout.Auto,
                    Compute = new GPUComputeState()
                    {
                        Module = computeShaderModule,
                    },
                });
            }

            //create spriteVertexBuffer
            {
                Span<float> vertexBufferData = stackalloc float[]
                {
                    -0.01f, -0.02f, 0.01f,
                    -0.02f, 0.0f, 0.02f,
                };

                var spriteVertexBufferDescriptor = new GPUBufferDescriptor()
                {
                    Size = (ulong)(vertexBufferData.Length * sizeof(float)),
                    Usage = WGPUBufferUsage.Vertex,
                    MappedAtCreation = true,
                };
                spriteVertexBuffer = _device.CreateBuffer(spriteVertexBufferDescriptor);
                
                //fill spriteVertexBuffer
                vertexBufferData.CopyTo(spriteVertexBuffer.GetMappedRange<float>(0, vertexBufferData.Length));
                spriteVertexBuffer.Unmap();
            }
            
            simParams = new SimParams()
            {
                deltaT = 0.04f,
                rule1Distance = 0.1f,
                rule2Distance = 0.025f,
                rule3Distance = 0.025f,
                rule1Scale = 0.02f,
                rule2Scale = 0.05f,
                rule3Scale = 0.005f,
            };
            
            ulong simParamBufferSize = (ulong)sizeof(SimParams);
            GPUBufferDescriptor simParamBufferDescriptor = new GPUBufferDescriptor()
            {
                Size = simParamBufferSize,
                Usage = WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst,
            };
            
            simParamBuffer = _device.CreateBuffer(simParamBufferDescriptor);
            UpdateSimParams();
            
            var initialParticleData = new float[numParticles * 4];
            for (int i = 0; i < numParticles; ++i) {
                initialParticleData[4 * i + 0] = 2 * (Random.Shared.NextSingle() - 0.5f);
                initialParticleData[4 * i + 1] = 2 * (Random.Shared.NextSingle() - 0.5f);
                initialParticleData[4 * i + 2] = 2 * (Random.Shared.NextSingle() - 0.5f) * 0.1f;
                initialParticleData[4 * i + 3] = 2 * (Random.Shared.NextSingle() - 0.5f) * 0.1f;
            }

            particleBuffers = new GPUBuffer[2];
            particleBindGroups = new GPUBindGroup[2];
            for (int i = 0; i < 2; i++) {
                GPUBufferDescriptor gpuBufferDescriptor = new GPUBufferDescriptor()
                {
                    Size = (ulong)(initialParticleData.Length * sizeof(float)),
                    Usage = WGPUBufferUsage.Vertex | WGPUBufferUsage.Storage,
                    MappedAtCreation = true,
                };
                particleBuffers[i] = _device.CreateBuffer(gpuBufferDescriptor);
                initialParticleData.CopyTo(particleBuffers[i].GetMappedRange<float>(0, initialParticleData.Length));
                particleBuffers[i].Unmap();
            }


            for (int i = 0; i < 2; ++i)
            {
                GPUBindGroupLayout gpuBindGroupLayout = computePipeline.GetBindGroupLayout(0);
                particleBindGroups[i] = _device.CreateBindGroup(new GPUBindGroupDescriptor()
                {
                    Layout = gpuBindGroupLayout,
                    Entries =
                    [
                        new WGPUBindGroupEntry() { binding = 0, buffer = simParamBuffer.Handle, size = simParamBuffer.Size},
                        new WGPUBindGroupEntry() { binding = 1, buffer = particleBuffers[i].Handle, size = particleBuffers[i].Size },
                        new WGPUBindGroupEntry() { binding = 2, buffer = particleBuffers[(i + 1) % 2].Handle, size = particleBuffers[(i + 1) % 2].Size },
                    ],
                });
            }
        }
        CreateSwapchain();
    }

    private unsafe void UpdateSimParams()
    {
        SimParams simData = simParams;
        _device.Queue.WriteBuffer(
            simParamBuffer,
            0,
            &simData,
            (UIntPtr)sizeof(SimParams)
        );
    }
    
    private void CreateSwapchain()
    {
        int w = 0;
        int h = 0;
        (w,h) = GetFramebufferSizeInPixel();
        Console.WriteLine($"GetFramebufferSizeInPixel {w}, {h}");
        var surfaceConfiguration = new GPUSurfaceConfiguration
        {
            Usage = WGPUTextureUsage.RenderAttachment,
            Device = _device,
            Format = _SurfaceCapabilities.Formats[0],
            PresentMode = WGPUPresentMode.Fifo,
            AlphaMode = _SurfaceCapabilities.AlphaModes[0],
            Width = (uint) w,
            Height = (uint) h,
        };
        
        _Surface.Configure(surfaceConfiguration);
        Console.WriteLine($"Surface Configured");
    }

    

    public override unsafe void WindowOnRender(double delta)
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

        GPURenderPassDescriptor renderPassDescriptor = new GPURenderPassDescriptor
        {
            ColorAttachments =
            [
                new GPURenderPassColorAttachment
                {
                    View = textureView,
                    ClearValue = new WGPUColor { r = 0, g = 0, b = 0, a = 1 },
                    LoadOp = WGPULoadOp.Clear,
                    StoreOp = WGPUStoreOp.Store,
                }
            ],
        };

        var computePassDescriptor = new GPUComputePassDescriptor();
        var commandEncoder = _device.CreateCommandEncoder();
        {
            var passEncoder = commandEncoder.BeginComputePass(computePassDescriptor);
            passEncoder.SetPipeline(computePipeline);
            passEncoder.SetBindGroup(0, particleBindGroups[t % 2]);
            passEncoder.DispatchWorkgroups((uint)MathF.Ceiling(numParticles / 64f));
            passEncoder.End();
            // passEncoder.Dispose();// TODO Do i need this? 
        }
        {
            var passEncoder = commandEncoder.BeginRenderPass(renderPassDescriptor);
            passEncoder.SetPipeline(renderPipeline);
            passEncoder.SetVertexBuffer(0, particleBuffers[(t + 1) % 2]);
            passEncoder.SetVertexBuffer(1, spriteVertexBuffer);
            passEncoder.Draw(3, numParticles, 0, 0);
            passEncoder.End();
            // passEncoder.Dispose();// TODO Do i need this?
        }

        
        var commandBuffer = commandEncoder.Finish();
        // commandEncoder.Dispose();// TODO Do i need this?
        _device.Queue.Submit(commandBuffer);
        _Surface.Present();
        _Window.SwapBuffers();
        // commandBuffer.Dispose();// TODO Do i need this?
        ++t;
    }

    public override void FramebufferResize(Vector2D<int> size)
    {
        CreateSwapchain();
    }

    public override void Dispose()
    {
        foreach (GPUBuffer particleBuffer in particleBuffers) 
            particleBuffer.Dispose();
        foreach (GPUBindGroup particleBindGroup in particleBindGroups)
        {
            particleBindGroup.Dispose();
        }
        spriteVertexBuffer.Dispose();
        simParamBuffer.Dispose();
        renderPipeline.Dispose();
        computePipeline.Dispose();
        spriteShaderModule.Dispose();
        computeShaderModule.Dispose();
        
    }
}