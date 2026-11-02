using System.Numerics;
using AssetManager;
using Istok.WebGPU;
using Istok.WebGPU.View;

namespace Examples;

// Chapter07/03_Transmission: rough transmission across rows of different IOR.
public class ExampleTransmission : ExampleGLTFBase
{
    protected override string ModelPath => "Models/TransmissionRoughnessTest/TransmissionRoughnessTest.gltf";
    protected override string ExampleName => "Transmission";
    protected override GLTFMaterialType MaterialType => GLTFMaterialType.Transmission;
    protected override Vector3 CameraPosition => new(0, 0, -1.65f);
    protected override Vector3 CameraTarget => Vector3.Zero;
    protected override Matrix4x4 ModelTransform(float time) =>
        Matrix4x4.CreateRotationY(MathF.PI);

    public ExampleTransmission(GPU instance, GPUDevice device, IWebGpuView window,
        GPUSurfaceCapabilities surfaceCapabilities, GPUSurface surface, IResourcesProvider resourcesProvider)
        : base(instance, device, window, surfaceCapabilities, surface, resourcesProvider)
    {
    }
}
