using System.Numerics;
using System.Runtime.CompilerServices;
using AssetManager;
using Istok.WebGPU;
using Istok.WebGPU.LowLevel;
using Silk.NET.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Istok.WebGPU.View;
using static Istok.WebGPU.LowLevel.WebGPUNative;

namespace Examples;

public class Example1 : ExampleBase
{
    private GPUShaderModule   _Shader;
    private GPURenderPipeline _Pipeline;
    private GPUBuffer _VertexBuffer;
    private ulong   _VertexBufferSize;
    

    private GPUTexture     _Texture;
    private GPUTextureView _TextureView;
    private GPUSampler     _Sampler;

    private GPUBindGroup       _TextureBindGroup;
    private GPUBindGroupLayout _TextureSamplerBindGroupLayout;

    private GPUBuffer          _ProjectionMatrixBuffer;
    private GPUBindGroupLayout _ProjectionMatrixBindGroupLayout;
    private GPUBindGroup       _ProjectionMatrixBindGroup;
    private DateTime _StartTime;
    
    
    public struct Vertex
    {
        public Vertex(Vector2 position, Vector2 texCoord)
        {
            Position = position;
            TexCoord = texCoord;
        }

        public Vector2 Position;
        public Vector2 TexCoord;
    }

    static unsafe WGPUVertexAttribute[] vertexAttributes = [
        new WGPUVertexAttribute
        {
            format         = WGPUVertexFormat.Float32X2,
            offset         = 0,
            shaderLocation = 0
        },
        new WGPUVertexAttribute
        {
            format         = WGPUVertexFormat.Float32X2,
            offset         = (ulong) sizeof(Vector2),
            shaderLocation = 1
        }
    ];

    public Example1(GPUDevice device, IWebGpuView window, GPUSurfaceCapabilities surfaceCapabilities, GPUSurface surface, IResourcesProvider resourcesProvider) : base(device, window, surfaceCapabilities, surface, resourcesProvider)
    {
        _StartTime = DateTime.UtcNow;
    }

    public override async Task OnLoad()
    {
        { //Load shader
            string shaderCode = await _ResourcesProvider.LoadTextAsync("Shaders/example1_shader.wgsl");
            
            if (shaderCode.Length == 0)
                throw new ArgumentException("Shader source must not be empty.", nameof(shaderCode));
            
            var shaderModuleDescriptor = new GPUShaderModuleDescriptor
            {
                Code = shaderCode
            };

            _Shader = _device.CreateShaderModule(shaderModuleDescriptor);

            Console.WriteLine($"Created shader {(nuint) _Shader.Handle.Handle:X}");
        } //Load shader

        Image<Rgba32> image;
        byte[] logoBytes = await _ResourcesProvider.LoadBytesAsync("Textures/WebGPU_logo.png");
        image = Image.Load<Rgba32>(logoBytes);

        unsafe
        {
            {
                //Create texture and texture view
                var viewFormat = WGPUTextureFormat.RGBA8Unorm;

                var descriptor = new GPUTextureDescriptor
                {
                    Size = new WGPUExtent3D { width = (uint)image.Width, height = (uint)image.Height, depthOrArrayLayers = 1 },
                    Format = WGPUTextureFormat.RGBA8Unorm,
                    Usage = WGPUTextureUsage.CopyDst | WGPUTextureUsage.TextureBinding,
                    MipLevelCount = 1,
                    SampleCount = 1,
                    Dimension = WGPUTextureDimension.D2D,
                    ViewFormats = [viewFormat],
                };

                _Texture = _device.CreateTexture(descriptor);

                var viewDescriptor = new GPUTextureViewDescriptor
                {
                    Format = WGPUTextureFormat.RGBA8Unorm,
                    Dimension = WGPUTextureViewDimension.D2D,
                    Aspect = WGPUTextureAspect.All,
                    MipLevelCount = 1,
                    ArrayLayerCount = 1,
                    BaseArrayLayer = 0,
                    BaseMipLevel = 0
                };

                _TextureView = _Texture.CreateView(viewDescriptor);

                FillTexture(_device, _Texture, image);
            } //Create texture and texture view

            {
                //Create sampler
                var descriptor = new GPUSamplerDescriptor
                {
                    Compare = WGPUCompareFunction.Undefined,
                    MipmapFilter = WGPUMipmapFilterMode.Linear,
                    MagFilter = WGPUFilterMode.Linear,
                    MinFilter = WGPUFilterMode.Linear,
                    MaxAnisotropy = 1
                };

                _Sampler = _device.CreateSampler(descriptor);
            } //Create sampler

            {
                //Create bind group for sampler and textureview


                var layoutDescriptor = new GPUBindGroupLayoutDescriptor
                {
                    Entries = stackalloc WGPUBindGroupLayoutEntry[]
                    {
                        new WGPUBindGroupLayoutEntry
                        {
                            binding = 0,
                            texture = new WGPUTextureBindingLayout
                            {
                                multisampled = false,
                                sampleType = WGPUTextureSampleType.Float,
                                viewDimension = WGPUTextureViewDimension.D2D
                            },
                            visibility = WGPUShaderStage.Fragment
                        },
                        new WGPUBindGroupLayoutEntry
                        {
                            binding = 1,
                            sampler = new WGPUSamplerBindingLayout
                            {
                                type = WGPUSamplerBindingType.Filtering
                            },
                            visibility = WGPUShaderStage.Fragment
                        }
                    }
                };

                _TextureSamplerBindGroupLayout = _device.CreateBindGroupLayout(layoutDescriptor);

                Span<WGPUBindGroupEntry> bindGroupEntries = stackalloc WGPUBindGroupEntry[2]
                {
                    new WGPUBindGroupEntry
                    {
                        binding = 0,
                        textureView = _TextureView.Handle
                    },
                    new WGPUBindGroupEntry
                    {
                        binding = 1,
                        sampler = _Sampler.Handle
                    }
                };

                var descriptor = new GPUBindGroupDescriptor
                {
                    Entries = bindGroupEntries,
                    Layout = _TextureSamplerBindGroupLayout
                };

                _TextureBindGroup = _device.CreateBindGroup(descriptor);
            } //Create bind group for sampler and texture view

            {
                //Create buffer to store projection matrix
                var descriptor = new GPUBufferDescriptor
                {
                    Size = (ulong)sizeof(Matrix4x4),
                    Usage = WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst,
                    MappedAtCreation = false
                };

                _ProjectionMatrixBuffer = _device.CreateBuffer(descriptor);

                UpdateProjectionMatrix();
            } //Create buffer to store projection matrix

            {
                //Create bind group for projection matrix
                _ProjectionMatrixBindGroupLayout = _device.CreateBindGroupLayout
                (
                    new GPUBindGroupLayoutDescriptor
                    {
                        Entries = stackalloc WGPUBindGroupLayoutEntry[]
                        {
                            new WGPUBindGroupLayoutEntry
                            {

                                binding = 0,
                                buffer = new WGPUBufferBindingLayout
                                {
                                    type = WGPUBufferBindingType.Uniform,
                                    minBindingSize = (ulong)sizeof(Matrix4x4)
                                },
                                visibility = WGPUShaderStage.Vertex,
                            }
                        }
                    }
                );

                _ProjectionMatrixBindGroup = _device.CreateBindGroup
                (
                    new GPUBindGroupDescriptor
                    {
                        Entries = stackalloc WGPUBindGroupEntry[]
                        {
                            new WGPUBindGroupEntry
                            {
                                binding = 0,
                                buffer = _ProjectionMatrixBuffer.Handle,
                                size = (ulong)sizeof(Matrix4x4)
                            }
                        },
                        Layout = _ProjectionMatrixBindGroupLayout
                    }
                );
            } //Create bind group for projection matrix


            fixed (WGPUVertexAttribute* vertexAttributesPtr = &vertexAttributes[0])
            {
                Span<WGPUVertexBufferLayout> vertexBufferLayout =
                [
                    new WGPUVertexBufferLayout
                    {
                        attributes = vertexAttributesPtr,
                        attributeCount = (UIntPtr)vertexAttributes.Length,
                        stepMode = WGPUVertexStepMode.Vertex,
                        arrayStride = (ulong)sizeof(Vertex)
                    }
                ];
                //Create vertex buffer layout

                {
                    //Create pipeline
                    var blendState = new GPUBlendState
                    {
                        Color = new GPUBlendComponent
                        {
                            SrcFactor = WGPUBlendFactor.SrcAlpha,
                            DstFactor = WGPUBlendFactor.OneMinusSrcAlpha,
                            Operation = WGPUBlendOperation.Add
                        },
                        Alpha = new GPUBlendComponent
                        {
                            SrcFactor = WGPUBlendFactor.One,
                            DstFactor = WGPUBlendFactor.OneMinusSrcAlpha,
                            Operation = WGPUBlendOperation.Add
                        }
                    };

                    var colorTargetState = new GPUColorTargetState
                    {
                        Format = _SurfaceCapabilities.Formats[0],
                        Blend = blendState,
                        WriteMask = WGPUColorWriteMask.All
                    };
                    
                    var fragmentState = new GPUFragmentState
                    {
                        Module = _Shader,
                        Targets = [colorTargetState],
                        EntryPoint = "fs_main"
                    };

                    var pipelineLayoutDescriptor = new GPUPipelineLayoutDescriptor
                    {
                        BindGroupLayouts = [_TextureSamplerBindGroupLayout, _ProjectionMatrixBindGroupLayout]
                    };

                    var pipelineLayout = _device.CreatePipelineLayout(pipelineLayoutDescriptor);

                    var renderPipelineDescriptor = new GPURenderPipelineDescriptor
                    {
                        Vertex = new GPUVertexState
                        {
                            Module = _Shader,
                            EntryPoint = "vs_main",
                            Buffers = vertexBufferLayout,
                        },
                        Fragment = fragmentState,
                        DepthStencil = ref Unsafe.NullRef<WGPUDepthStencilState>(),
                        Layout = pipelineLayout
                    };

                    _Pipeline = _device.CreateRenderPipeline(renderPipelineDescriptor);

                }
                Console.WriteLine($"Created pipeline {(nuint)_Pipeline.Handle.Handle:X}");
            } //Create pipeline

            {
                //Create vertex buffer
                var descriptor = new GPUBufferDescriptor
                {
                    Size = _VertexBufferSize = (ulong)(sizeof(Vertex) * 6),
                    Usage = WGPUBufferUsage.Vertex | WGPUBufferUsage.CopyDst
                };

                _VertexBuffer = _device.CreateBuffer(descriptor);
                Console.WriteLine($"Created VertexBuffer {(nuint)_VertexBuffer.Handle.Handle:X}");
                //Get a queue
                var queue = _device.GetQueue();

                var data = stackalloc Vertex[6];

                const float xPos = -128;
                const float yPos = -128;
                const float width = 256;
                const float height = 256;

                //Fill data with a quad with a CCW front face
                data[0] = new Vertex(new Vector2(xPos, yPos), new Vector2(0, 1)); //Top left
                data[1] = new Vertex(new Vector2(xPos + width, yPos), new Vector2(1, 1)); //Top right
                data[2] = new Vertex(new Vector2(xPos + width, yPos + height), new Vector2(1, 0)); //Bottom right
                data[3] = new Vertex(new Vector2(xPos, yPos), new Vector2(0, 1)); //Top left
                data[4] = new Vertex(new Vector2(xPos + width, yPos + height), new Vector2(1, 0)); //Bottom right
                data[5] = new Vertex(new Vector2(xPos, yPos + height), new Vector2(0, 0)); //Bottom left

                //Write the data to the buffer
                queue.WriteBuffer(_VertexBuffer, 0, data, (nuint)_VertexBufferSize);

                //Create a new command encoder
                var commandEncoder = _device.CreateCommandEncoder();

                //Finish the command encoder
                var commandBuffer = commandEncoder.Finish();
                commandEncoder.Dispose();
                queue.Submit(commandBuffer);
                commandBuffer.Dispose();
                Console.WriteLine($"VertexBuffer filled");
            } //Create vertex buffer

            CreateSwapchain();
        }
    }

    private static unsafe void FillTexture(GPUDevice device, GPUTexture texture, Image<Rgba32> image)
    {
        var queue = device.GetQueue();

        var commandEncoderDescriptor = new GPUCommandEncoderDescriptor();

        var commandEncoder = device.CreateCommandEncoder(commandEncoderDescriptor);
            
            
        image.ProcessPixelRows
        (
            x =>
            {
                for (int i = 0; i < x.Height; i++)
                {
                    var imageRow = x.GetRowSpan(i);

                    var imageCopyTexture = new WGPUTexelCopyTextureInfo
                    {
                        texture  = texture.Handle,
                        mipLevel = 0,
                        origin   = new WGPUOrigin3D{ y = (uint)i},
                        aspect   = WGPUTextureAspect.All,
                    };

                    var layout = new WGPUTexelCopyBufferLayout
                    {
                        bytesPerRow  = (uint) (x.Width * sizeof(Rgba32)),
                        rowsPerImage = (uint) x.Height
                    };
                    // layout.Offset = layout.BytesPerRow * (uint) i;

                    var extent = new WGPUExtent3D
                    {
                        width              = (uint) x.Width,
                        height             = 1,
                        depthOrArrayLayers = 1
                    };

                    fixed (void* dataPtr = imageRow)
                        queue.WriteTexture(imageCopyTexture, dataPtr, (nuint) (sizeof(Rgba32) * imageRow.Length), layout, extent);
                }
            }
        );

        var commandBuffer = commandEncoder.Finish();
            
        queue.Submit(commandBuffer);
    }

    private unsafe void UpdateProjectionMatrix()
    {
        var queue = _device.GetQueue();

        var commandEncoder = _device.CreateCommandEncoder();

        var modelPosition = new Vector3(0, 0, 0);
        var modelRotation = Quaternion.CreateFromAxisAngle(new Vector3(0,0,1), (float)((DateTime.UtcNow - _StartTime).TotalMilliseconds / 500f));//new Quaternion(0, 0, 0, 1);
        Matrix4x4 model = Matrix4x4.CreateFromQuaternion(modelRotation) * Matrix4x4.CreateTranslation(modelPosition);;
        
        var cameraPosition = new Vector3(140, -140, 120);
        var cameraRotation = new Quaternion(0.4964f, 0.205616f, 0.322752f, 0.779192f);
        
        var cameraMatrix = Matrix4x4.CreateFromQuaternion(cameraRotation) * Matrix4x4.CreateTranslation(cameraPosition);
        Matrix4x4.Invert(cameraMatrix, out var view);
        var projectionMatrix = Matrix4x4.CreateOrthographic( _Window.Size.X, _Window.Size.Y, 0, 1000);

        var mvp = model * view * projectionMatrix;
        queue.WriteBuffer(_ProjectionMatrixBuffer, 0, &mvp, (nuint) sizeof(Matrix4x4));

        var commandBuffer = commandEncoder.Finish();
        commandEncoder.Dispose();
        queue.Submit(commandBuffer);
        commandBuffer.Dispose();
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
            // PresentMode = PresentMode.FifoRelaxed,
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

        UpdateProjectionMatrix();
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

        var colorAttachment = new WGPURenderPassColorAttachment
        {
            view          = currentTexture,
            resolveTarget = WGPUTextureView.Null,
            loadOp        = WGPULoadOp.Clear,
            storeOp       = WGPUStoreOp.Store,
            clearValue = new WGPUColor
            {
                r = 0,
                g = 1,
                b = 0,
                a = 1
            },
            depthSlice = _Surface.DepthSliceUndefined
        };

        var renderPassDescriptor = new GPURenderPassDescriptor
        {
            ColorAttachments = stackalloc WGPURenderPassColorAttachment[] { colorAttachment }
        };

        var renderPass = encoder.BeginRenderPass(renderPassDescriptor);

        renderPass.SetPipeline(_Pipeline);
        renderPass.SetBindGroup(0, _TextureBindGroup);
        renderPass.SetBindGroup(1, _ProjectionMatrixBindGroup);
        renderPass.SetVertexBuffer(0, _VertexBuffer, 0, _VertexBufferSize);
        renderPass.Draw(6, 1, 0, 0);

        renderPass.End();
        renderPass.Dispose();

        var queue = _device.GetQueue();

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
        UpdateProjectionMatrix();
    }

    public override void Dispose()
    {
        _Pipeline.Dispose();
        _Shader.Dispose();
    }
}