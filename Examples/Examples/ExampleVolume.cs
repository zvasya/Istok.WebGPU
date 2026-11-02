using System.Numerics;
using AssetManager;
using Istok.WebGPU;
using Istok.WebGPU.View;

namespace Examples;

// Chapter07/04_Volume: thickness-dependent absorption in the dragon.
public sealed class ExampleVolume : ExampleGLTFBase
{
    protected override string ModelPath => "Models/DragonAttenuation/DragonAttenuation.gltf";
    protected override string ExampleName => "Volume";
    protected override GLTFMaterialType MaterialType => GLTFMaterialType.Transmission;
    protected override Vector3 CameraPosition => new(0, 1, -4.8f);
    protected override Vector3 CameraTarget => new(0, 0.45f, 0);
    protected override float NearPlane => 0.1f;
    protected override float FarPlane => 1000;
    protected override Matrix4x4 ModelTransform(float time) =>
        Matrix4x4.CreateRotationY(MathF.PI);

    public ExampleVolume(GPU instance, GPUDevice device, IWebGpuView window,
        GPUSurfaceCapabilities surfaceCapabilities, GPUSurface surface, IResourcesProvider resourcesProvider)
        : base(instance, device, window, surfaceCapabilities, surface, resourcesProvider)
    {
    }
}
