using System.Numerics;
using System.Runtime.CompilerServices;
using AssetManager;
using Istok.WebGPU;
using Istok.WebGPU.LowLevel;
using Silk.NET.Maths;
using SkiaSharp;
using Istok.WebGPU.View;
using static Istok.WebGPU.LowLevel.WebGPUNative;

namespace Examples;

public class ExampleSkia : ExampleBase
{
    private GPUShaderModule   _Shader;
    private GPURenderPipeline _Pipeline;
    private GPUBuffer _VertexBuffer;
    private ulong _VertexBufferSize;

    // private GPUTexture     _Texture;
    // private GPUTextureView _TextureView;
    // private GPUSampler     _Sampler;

    // private GPUBindGroup       _TextureBindGroup;
    // private GPUBindGroupLayout _TextureSamplerBindGroupLayout;

    private GPUBuffer          _ProjectionMatrixBuffer;
    private GPUBindGroupLayout _ProjectionMatrixBindGroupLayout;
    private GPUBindGroup       _ProjectionMatrixBindGroup;
    private DateTime _StartTime;

    const int FrameHistory = 300;
    readonly double[] _frameMs = new double[FrameHistory];
    int _frameCount;
    int _frameWrite;

    SKGraphiteContext? _graphite;
    SKGraphiteRecorder? _recorder;
    bool _hudFailureLogged;
    readonly SKFont _hudFont = new() { Size = 15 };
    readonly SKPaint _hudBackground = new() { Color = new SKColor(0, 0, 0, 170), IsAntialias = true };
    readonly SKPaint _hudText = new() { Color = SKColors.White, IsAntialias = true };
    readonly SKPaint _hudGraph = new()
    {
        Color = SKColors.LimeGreen,
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.5f,
        StrokeJoin = SKStrokeJoin.Round
    };
    readonly SKPaint _hudAverage = new()
    {
        Color = SKColors.Orange,
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1f
    };
    readonly SKPathBuilder _hudPath = new();
    
    
    public struct Vertex
    {
        public Vertex(Vector2 position, Vector2 texCoord, Vector3 color)
        {
            Position = position;
            TexCoord = texCoord;
            Color = color;
        }

        public Vector2 Position;
        public Vector2 TexCoord;
        private Vector3 Color;
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
        },
        new WGPUVertexAttribute
        {
            format         = WGPUVertexFormat.Float32X3,
            offset         = (ulong) sizeof(Vector2) + (ulong) sizeof(Vector2),
            shaderLocation = 2
        }
    ];

    public ExampleSkia(GPU instance, GPUDevice device, IWebGpuView window, GPUSurfaceCapabilities surfaceCapabilities, GPUSurface surface, IResourcesProvider resourcesProvider)
        : base(instance, device, window, surfaceCapabilities, surface, resourcesProvider)
    {
        _StartTime = DateTime.UtcNow;
    }

    public override async Task OnLoad()
    {
        { //Load shader
            string shaderCode = await _ResourcesProvider.LoadTextAsync("Shaders/example0_shader.wgsl");
            
            if (shaderCode.Length == 0)
                throw new ArgumentException("Shader source must not be empty.", nameof(shaderCode));
            
            var shaderModuleDescriptor = new GPUShaderModuleDescriptor
            {
                Code = shaderCode
            };

            _Shader = _device.CreateShaderModule(shaderModuleDescriptor);
            
            Console.WriteLine($"Created shader {(nuint) _Shader.Handle.Handle:X}");
        } //Load shader
        
        unsafe
        {
        { //Create buffer to store projection matrix
            var descriptor = new GPUBufferDescriptor
            {
                Size             = (ulong) sizeof(Matrix4x4),
                Usage            = WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst,
                MappedAtCreation = false
            };

            _ProjectionMatrixBuffer = _device.CreateBuffer(descriptor);

            UpdateProjectionMatrix();
        } //Create buffer to store projection matrix

        { //Create bind group for projection matrix
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
                    Entries    = stackalloc WGPUBindGroupEntry[]
                    {
                        new WGPUBindGroupEntry
                        {
                            binding = 0,
                            buffer  = _ProjectionMatrixBuffer.Handle,
                            size = (ulong) sizeof(Matrix4x4)
                        }
                    },
                    Layout     = _ProjectionMatrixBindGroupLayout
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
                },
            ];
            //Create vertex buffer layout
            
            { //Create pipeline
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
                    Format    = _SurfaceCapabilities.Formats[0],
                    Blend     = blendState,
                    WriteMask = WGPUColorWriteMask.All
                };

                var fragmentState = new GPUFragmentState
                {
                    Module      = _Shader,
                    Targets     = [colorTargetState],
                    EntryPoint  = "fs_main"
                };

                var pipelineLayoutDescriptor = new GPUPipelineLayoutDescriptor
                {
                    BindGroupLayouts     = [_ProjectionMatrixBindGroupLayout]
                };

                var pipelineLayout = _device.CreatePipelineLayout(pipelineLayoutDescriptor);
                
                var renderPipelineDescriptor = new GPURenderPipelineDescriptor
                {
                    Vertex = new GPUVertexState()
                    {
                        Module      = _Shader,
                        EntryPoint  = "vs_main",
                        Buffers     = vertexBufferLayout,
                    },
                    Fragment     = fragmentState,
                    DepthStencil = ref Unsafe.NullRef<WGPUDepthStencilState>(),
                    Layout       = pipelineLayout
                };

                _Pipeline = _device.CreateRenderPipeline(renderPipelineDescriptor);
       
            }
            Console.WriteLine($"Created pipeline {(nuint) _Pipeline.Handle.Handle:X}");
        } //Create pipeline

        { //Create vertex buffer
            var descriptor = new GPUBufferDescriptor
            {
                Size  = _VertexBufferSize = (ulong) (sizeof(Vertex) * 6),
                Usage = WGPUBufferUsage.Vertex | WGPUBufferUsage.CopyDst
            };

            _VertexBuffer = _device.CreateBuffer(descriptor);
            Console.WriteLine($"Created VertexBuffer {(nuint) _VertexBuffer.Handle.Handle:X}");
            //Get a queue
            var queue = _device.GetQueue();

            Span<Vertex> data = stackalloc Vertex[6];

            const float xPos   = -128;
            const float yPos   = -128;
            const float width  = 256;
            const float height = 256;
            
            //Fill data with a quad with a CCW front face
            data[0] = new Vertex(new Vector2(xPos, yPos), new Vector2(0, 1), new Vector3(1,0,0)); //Top left
            data[1] = new Vertex(new Vector2(xPos + width, yPos), new Vector2(1, 1), new Vector3(0,1,0));  //Top right
            data[2] = new Vertex(new Vector2(xPos + width, yPos + height), new Vector2(1, 0), new Vector3(1,1,1));   //Bottom right
            data[3] = new Vertex(new Vector2(xPos, yPos), new Vector2(0, 1), new Vector3(1,0,0)); //Top left
            data[4] = new Vertex(new Vector2(xPos + width, yPos + height), new Vector2(1, 0), new Vector3(1,1,1));   //Bottom right
            data[5] = new Vertex(new Vector2(xPos, yPos + height), new Vector2(0, 0), new Vector3(0,0,1));  //Bottom left
            
            //Write the data to the buffer
            fixed (Vertex* vertexPtr = data)
            {
                queue.WriteBuffer(_VertexBuffer, 0, vertexPtr, (nuint) _VertexBufferSize);
            }

            //Create a new command encoder
            var commandEncoder = _device.CreateCommandEncoder();

            //Finish the command encoder
            var commandBuffer = commandEncoder.Finish();
            commandEncoder.Dispose();
            queue.Submit( commandBuffer);
            commandBuffer.Dispose();
            Console.WriteLine($"VertexBuffer filled");
        } //Create vertex buffer

        CreateSwapchain();
        }
        
        var backendContext = new SKGraphiteDawnBackendContext
        {
            WgpuInstance = (IntPtr)_instance.Instance.Handle,
            WgpuDevice = _device.Handle.Handle,
            WgpuQueue = _device.Queue.Handle.Handle,
        };
        
        _graphite = SKGraphiteContext.CreateDawn(backendContext, new SKGraphiteContextOptions
            {
                InternalMultisampleCount = 1,
            })
            ?? throw new InvalidOperationException("SKGraphiteContext.CreateDawn returned null.");
        _recorder = _graphite.CreateRecorder(-1, static (recorder, image, mipmapped) => image.ToTextureImage(recorder, mipmapped))
            ?? throw new InvalidOperationException("SKGraphiteContext.CreateRecorder returned null.");
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
            Usage = WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.CopySrc,
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

    

    void RecordFrame(double deltaSeconds)
    {
        _frameMs[_frameWrite] = deltaSeconds * 1000.0;
        _frameWrite = (_frameWrite + 1) % FrameHistory;
        if (_frameCount < FrameHistory)
            _frameCount++;
    }

    void DrawFrameStats(WGPUTexture texture)
    {
        if (_graphite == null || _recorder == null || _frameCount == 0)
            return;

        var format = _SurfaceCapabilities.Formats[0];
        using var backendTexture = SKGraphiteBackendTexture.CreateDawn(texture.Handle);
        if (backendTexture == null || !backendTexture.IsValid)
        {
            LogHudOnce($"Skia HUD: CreateDawn rejected swapchain texture {format}.");
            return;
        }

        using var surface = SKSurface.Create(_recorder, backendTexture, ToColorType(format));
        if (surface == null)
        {
            LogHudOnce($"Skia HUD: SKSurface.Create returned null for {format}.");
            return;
        }

        float width = wgpuTextureGetWidth(texture);
        const float margin = 12;
        const float pad = 8;
        const float textH = 18;
        const float graphH = 72;
        float graphW = Math.Min(300, Math.Max(32, width - margin * 2 - pad * 2));
        float panelW = graphW + pad * 2;
        float panelH = textH + graphH + pad * 2 + 4;
        float panelX = margin;
        float panelY = margin;
        float graphLeft = panelX + pad;
        float graphTop = panelY + pad + textH + 4;
        float graphBottom = graphTop + graphH;

        int oldest = _frameCount < FrameHistory ? 0 : _frameWrite;
        double sum = 0;
        double max = 1;
        for (int i = 0; i < _frameCount; i++)
        {
            double sample = _frameMs[(oldest + i) % FrameHistory];
            sum += sample;
            if (sample > max)
                max = sample;
        }

        double current = _frameMs[(_frameWrite + FrameHistory - 1) % FrameHistory];
        double average = sum / _frameCount;
        double fps = average > 0 ? 1000.0 / average : 0;
        float scale = (float)(max * 1.05);

        var canvas = surface.Canvas;
        canvas.DrawRoundRect(panelX, panelY, panelW, panelH, 6, 6, _hudBackground);
        canvas.DrawText($"{current:0.00} ms    avg {average:0.00} ms    {fps:0} FPS", panelX + pad, panelY + pad + 14, SKTextAlign.Left, _hudFont, _hudText);

        float yAverage = graphBottom - (float)(average / scale) * graphH;
        canvas.DrawLine(graphLeft, yAverage, graphLeft + graphW, yAverage, _hudAverage);

        _hudPath.Reset();
        for (int i = 0; i < _frameCount; i++)
        {
            float x = graphLeft + (_frameCount == 1 ? 0 : graphW * i / (_frameCount - 1));
            float y = graphBottom - (float)(_frameMs[(oldest + i) % FrameHistory] / scale) * graphH;
            if (i == 0)
                _hudPath.MoveTo(x, y);
            else
                _hudPath.LineTo(x, y);
        }
        using var graphPath = _hudPath.Detach();
        canvas.DrawPath(graphPath, _hudGraph);

        using var recording = _recorder.Snap();
        if (recording == null)
        {
            LogHudOnce("Skia HUD: recorder snap was empty.");
            return;
        }

        var status = _graphite.InsertRecording(recording);
        if (status != SKGraphiteInsertStatus.Success)
        {
            LogHudOnce($"Skia HUD: insert {status}.");
            return;
        }

        _graphite.Submit(new SKGraphiteSubmitInfo { Sync = false });
    }

    static SKColorType ToColorType(WGPUTextureFormat format) => format switch
    {
        WGPUTextureFormat.BGRA8Unorm or WGPUTextureFormat.BGRA8UnormSrgb => SKColorType.Bgra8888,
        WGPUTextureFormat.RGBA16Float => SKColorType.RgbaF16,
        _ => SKColorType.Rgba8888,
    };

    void LogHudOnce(string message)
    {
        if (_hudFailureLogged)
            return;
        _hudFailureLogged = true;
        Console.WriteLine(message);
    }

    public override unsafe void WindowOnRender(double delta)
    {
        RecordFrame(delta);
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
        var currentTexture = wgpuTextureCreateView(surfaceTexture.texture, null);

        var encoder = _device.CreateCommandEncoder();

        var colorAttachment = new WGPURenderPassColorAttachment
        {
            view          = currentTexture,
            resolveTarget = WGPUTextureView.Null,
            loadOp        = WGPULoadOp.Clear,
            storeOp       = WGPUStoreOp.Store,
            clearValue = new WGPUColor
            {
                r = 0.1,
                g = 0.1,
                b = 0.1,
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
        renderPass.SetBindGroup(0, _ProjectionMatrixBindGroup);
        renderPass.SetVertexBuffer(0, _VertexBuffer, 0, _VertexBufferSize);
        renderPass.Draw(6, 1, 0, 0);

        renderPass.End();
        renderPass.Dispose();

        var queue = _device.GetQueue();
        var commandBuffer = encoder.Finish();
        encoder.Dispose();
        queue.Submit(commandBuffer);
        commandBuffer.Dispose();
        wgpuTextureViewRelease(currentTexture);

        DrawFrameStats(surfaceTexture.texture);
        _Surface.Present();
        _Window.SwapBuffers();
        wgpuTextureRelease(surfaceTexture.texture);
    }

    public override void FramebufferResize(Vector2D<int> size)
    {
        CreateSwapchain();
        UpdateProjectionMatrix();
    }

    public override void Dispose()
    {
        _hudPath.Dispose();
        _hudAverage.Dispose();
        _hudGraph.Dispose();
        _hudText.Dispose();
        _hudBackground.Dispose();
        _hudFont.Dispose();
        _recorder?.Dispose();
        _graphite?.Dispose();
        _Pipeline.Dispose();
        _Shader.Dispose();
    }
}