using System.Numerics;
using AssetManager;
using Istok.WebGPU;
using Istok.WebGPU.View;

namespace Examples;

// Chapter07/05_IOR: refraction through amber with IOR 1.55.
public sealed class ExampleIOR : ExampleGLTFBase
{
    protected override string ModelPath => "Models/MosquitoInAmber/MosquitoInAmber.gltf";
    protected override string ExampleName => "IOR";
    protected override GLTFMaterialType MaterialType => GLTFMaterialType.Transmission;
    protected override Vector3 CameraPosition => new(0, -0.4f, 0.2f);
    protected override Vector3 CameraTarget => new(0, -0.5f, 0);
    protected override Matrix4x4 ModelTransform(float time) =>
        Matrix4x4.CreateTranslation(0, -0.5f, 0);

    public ExampleIOR(GPU instance, GPUDevice device, IWebGpuView window,
        GPUSurfaceCapabilities surfaceCapabilities, GPUSurface surface, IResourcesProvider resourcesProvider)
        : base(instance, device, window, surfaceCapabilities, surface, resourcesProvider)
    {
    }
}
