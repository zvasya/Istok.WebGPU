using System.Runtime.CompilerServices;
using AssetManager;
using Istok.WebGPU;
using Istok.WebGPU.LowLevel;

namespace Examples.GpuLife;

public class LinkedListEngine
{
	private GPUShaderModule? constructModule;
	private GPUComputePipeline? constructPipeline;
	private GPUShaderModule? simModule;
	private GPUComputePipeline? simPipeline;
  
  GPUBuffer? headsBuffer;
  GPUBuffer? headsInitBuffer;

  Pair<GPUBindGroup>? constructBindGroups;
  Pair<GPUBindGroup>? simBindGroups;
  
  const int workgroupSize = 64;
  
	public async Task Setup(GPUDevice device, IResourcesProvider resourcesProvider) {
		
		string constructShader  = await resourcesProvider.LoadTextAsync("Shaders/example_gpulife_construct.wgsl");
		string simShader = await resourcesProvider.LoadTextAsync("Shaders/example_gpulife_sim.wgsl");

		constructModule = device.CreateShaderModule(new GPUShaderModuleDescriptor()
		{
			Code = constructShader,
		});

		// setupTimestamp(device, 'construct');

		constructPipeline = device.CreateComputePipeline( new GPUComputePipelineDescriptor()
		{
			Layout = GPUPipelineLayout.Auto,
			Compute = new GPUComputeState() 
			{
				Module = constructModule,
				EntryPoint = "main",
			},
		});

		//

		 
		simModule = device.CreateShaderModule( new GPUShaderModuleDescriptor()
		{
			Code = simShader,
		});

		// setupTimestamp(device, 'sim');
 
		simPipeline = device.CreateComputePipeline(new GPUComputePipelineDescriptor()
		{
			Layout = GPUPipelineLayout.Auto,
			Compute = new GPUComputeState() {
				Module = simModule,
				EntryPoint = "main",
			},
		});
	}

  public readonly struct Pair<T>(T item1, T item2)
  {
    public T this[int index] =>
      index switch
      {
        0 => item1,
        1 => item2,
        _ => throw new ArgumentOutOfRangeException(nameof(index)),
      };
    
    public static implicit operator Pair<T>((T, T) pair) => new Pair<T>(pair.Item1, pair.Item2);
  }
  
	
  public void Start(
    GPUDevice device,
    GPUBuffer uniformBuffer,
    GPUBuffer simBuffer,
    GPUBuffer matrixBuffer,
    Pair<GPUBuffer> particleBuffers,
    uint particleAmt,
    int cellAmt
  ) {
    if (constructPipeline == null || simPipeline == null)
      return;

    headsBuffer = device.CreateBuffer(new GPUBufferDescriptor()
      {
        Size = (ulong)((1 + cellAmt) * sizeof(uint)),
        Usage = WGPUBufferUsage.Storage | WGPUBufferUsage.CopyDst,
        Label = "headsBuffer",
      }
    );

    headsInitBuffer = device.CreateBuffer(new GPUBufferDescriptor()
      {
        Size = (ulong)((1 + cellAmt) * sizeof(uint)),
        Usage = WGPUBufferUsage.CopySrc,
        MappedAtCreation = true,
        Label = "headsInitBuffer",
      }
    );
    
    Span<uint> buffer = headsInitBuffer.GetMappedRange<uint>();

    buffer[0] = 0;
    buffer[1..].Fill(0xffffffff);

    headsInitBuffer.Unmap();

    GPUBuffer linkedListBuffer = device.CreateBuffer( new GPUBufferDescriptor(){
      Size = (ulong)(Unsafe.SizeOf<ListParticle>() * particleAmt),
      Usage = WGPUBufferUsage.Storage | WGPUBufferUsage.CopyDst,
      Label = "linkedListBuffer",
    });

    var constructGroups = new GPUBindGroup[2];
    for (var i = 0; i < 2; i++)
    {
      constructGroups[i] =
        device.CreateBindGroup(new GPUBindGroupDescriptor()
          {
            Layout = constructPipeline.GetBindGroupLayout(0),
            Entries =
            [
              new WGPUBindGroupEntry()
              {
                binding = 0,
                buffer = simBuffer,
                size = simBuffer.Size,
              },
              new WGPUBindGroupEntry()
              {
                binding = 1,
                buffer = particleBuffers[i],
                size = particleBuffers[i].Size,
              },
              new WGPUBindGroupEntry()
              {
                binding = 2,
                buffer = headsBuffer,
                size = headsBuffer.Size,
              },
              new WGPUBindGroupEntry()
              {
                binding = 3,
                buffer = linkedListBuffer,
                size = linkedListBuffer.Size,
              },
            ],
          }
        );
    }
    constructBindGroups = (constructGroups[0], constructGroups[1]);

    var simGroups = new GPUBindGroup[2];
    for (var i = 0; i < 2; i++) {
      simGroups[i] = device.CreateBindGroup(new GPUBindGroupDescriptor()
      {
        Layout = simPipeline.GetBindGroupLayout(0),
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
            buffer = simBuffer,
            size = simBuffer.Size,
          },
          new WGPUBindGroupEntry()
          {
            binding = 2,
            buffer = matrixBuffer,
            size = matrixBuffer.Size,
          },
          new WGPUBindGroupEntry()
          {
            binding = 3,
            buffer = particleBuffers[i],
            size = particleBuffers[i].Size,
          },
          new WGPUBindGroupEntry()
          {
            binding = 4,
            buffer = particleBuffers[1 - i],
            size = particleBuffers[1 - i].Size,
          },
          new WGPUBindGroupEntry()
          {
            binding = 5,
            buffer = headsBuffer,
            size = headsBuffer.Size,
          },
          new WGPUBindGroupEntry()
          {
            binding = 6,
            buffer = linkedListBuffer,
            size = linkedListBuffer.Size,
          },
        ],
      });
    }
    simBindGroups = (simGroups[0], simGroups[1]);
  }

  public void Tick(
    GPUDevice device,
    GPUCommandEncoder commandEncoder,
    int alternate,
    uint particleAmt
  )
  {
    if (
      constructPipeline == null ||
      simPipeline == null ||
      constructBindGroups == null ||
      headsInitBuffer == null ||
      headsBuffer == null ||
      simBindGroups == null
    )
      return;

    commandEncoder.CopyBufferToBuffer(headsInitBuffer, headsBuffer);

    var constructPassEncoder = commandEncoder.BeginComputePass(
      // linkComputeTimestamp(device, "construct"),
    );
    constructPassEncoder.SetPipeline(constructPipeline);
    constructPassEncoder.SetBindGroup(0, constructBindGroups.Value[alternate]);
    constructPassEncoder.DispatchWorkgroups((uint)Math.Ceiling((double)particleAmt / workgroupSize));
    constructPassEncoder.End();

    // resolveTimestamp(commandEncoder, 'construct');

    var simPassEncoder = commandEncoder.BeginComputePass(
      // linkComputeTimestamp(device, 'sim'),
    );
    simPassEncoder.SetPipeline(simPipeline);
    simPassEncoder.SetBindGroup(0, simBindGroups.Value[alternate]);
    simPassEncoder.DispatchWorkgroups((uint)Math.Ceiling((double)particleAmt / workgroupSize));
    simPassEncoder.End();

    // resolveTimestamp(commandEncoder, 'sim');
  }

  // public void UpdateDisplays(params: Record<string, number>) {
  //   readTimestamp('construct').then((time) => {
  //     params.construct = time;
  //   });
  //   readTimestamp('sim').then((time) => {
  //     params.sim = time;
  //   });
  // }
}