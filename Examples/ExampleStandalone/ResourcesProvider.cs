using AssetManager;

namespace ExampleStandalone;

public class ResourcesProvider : IResourcesProvider
{
	public Task<String> LoadTextAsync(string path)
	{
		return Task.FromResult(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, path)));
	}

	public Task<byte[]> LoadBytesAsync(string path)
	{
		return Task.FromResult(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, path)));
	}
}