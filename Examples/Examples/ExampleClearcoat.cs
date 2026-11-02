using System.Numerics;
using AssetManager;
using Istok.WebGPU;
using Istok.WebGPU.View;

namespace Examples;

// Chapter07/01_Clearcoat: transparent coating over the woven surface.
public class ExampleClearcoat : ExampleGLTFBase
{
    protected override string ModelPath => "Models/ClearcoatWicker/ClearcoatWicker.gltf";
    protected override string ExampleName => "Clearcoat";
    protected override GLTFMaterialType MaterialType => GLTFMaterialType.Clearcoat;
    protected override Vector3 CameraPosition => new(0, -0.2f, 1.8f);
    protected override Vector3 CameraTarget => new(0, -0.5f, 0);
    protected override Matrix4x4 ModelTransform(float time) =>
        Matrix4x4.CreateRotationY(time) * Matrix4x4.CreateTranslation(0, -1, 0);

    public ExampleClearcoat(GPU instance, GPUDevice device, IWebGpuView window,
        GPUSurfaceCapabilities surfaceCapabilities, GPUSurface surface, IResourcesProvider resourcesProvider)
        : base(instance, device, window, surfaceCapabilities, surface, resourcesProvider)
    {
    }
}
