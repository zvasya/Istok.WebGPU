using System.Numerics;
using AssetManager;
using Istok.WebGPU;
using Istok.WebGPU.View;

namespace Examples;

// Chapter07/02_Sheen: Charlie reflection on the velvet chair.
public class ExampleSheen : ExampleGLTFBase
{
    protected override string ModelPath => "Models/SheenChair/SheenChair.gltf";
    protected override string ExampleName => "Sheen";
    protected override GLTFMaterialType MaterialType => GLTFMaterialType.Sheen;
    protected override Vector3 CameraPosition => new(0.85f, -0.25f, 1.45f);
    protected override Vector3 CameraTarget => new(0, -0.65f, 0);
    protected override Matrix4x4 ModelTransform(float time) =>
        Matrix4x4.CreateRotationY(time) * Matrix4x4.CreateTranslation(0, -1, 0);

    public ExampleSheen(GPU instance, GPUDevice device, IWebGpuView window,
        GPUSurfaceCapabilities surfaceCapabilities, GPUSurface surface, IResourcesProvider resourcesProvider)
        : base(instance, device, window, surfaceCapabilities, surface, resourcesProvider)
    {
    }
}
