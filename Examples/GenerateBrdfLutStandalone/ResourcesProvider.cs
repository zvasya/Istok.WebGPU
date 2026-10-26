using AssetManager;

namespace GenerateBrdfLutStandalone;

internal sealed class ResourcesProvider(string assetsDirectory) : IResourcesProvider
{
    public Task<string> LoadTextAsync(string path) =>
        File.ReadAllTextAsync(Path.Combine(assetsDirectory, path));

    public Task<byte[]> LoadBytesAsync(string path) =>
        File.ReadAllBytesAsync(Path.Combine(assetsDirectory, path));
}
