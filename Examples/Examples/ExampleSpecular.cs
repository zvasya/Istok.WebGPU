using System.Numerics;
using AssetManager;
using Istok.WebGPU;
using Istok.WebGPU.View;

namespace Examples;

// Chapter07/06_Specular: silk combines KHR_materials_specular with sheen.
public sealed class ExampleSpecular : ExampleGLTFBase
{
    protected override string ModelPath => "Models/SpecularSilkPouf/SpecularSilkPouf.gltf";
    protected override string ExampleName => "Specular";
    protected override GLTFMaterialType MaterialType => GLTFMaterialType.Specular;
    protected override Vector3 CameraPosition => new(0, -0.5f, -1);
    protected override Vector3 CameraTarget => new(0, -1, 0);
    protected override Matrix4x4 ModelTransform(float time) =>
        Matrix4x4.CreateRotationY(time) * Matrix4x4.CreateTranslation(0, -1, 0);

    public ExampleSpecular(GPU instance, GPUDevice device, IWebGpuView window,
        GPUSurfaceCapabilities surfaceCapabilities, GPUSurface surface, IResourcesProvider resourcesProvider)
        : base(instance, device, window, surfaceCapabilities, surface, resourcesProvider)
    {
    }
}
