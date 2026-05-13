using AssetManager;

namespace ExampleiOS;

public class ResourcesProvider : IResourcesProvider
{
	public Task<string> LoadTextAsync(string path)
	{
		return Task.FromResult(File.ReadAllText(path));
	}

	public Task<byte[]> LoadBytesAsync(string path)
	{
		return Task.FromResult(File.ReadAllBytes(path));
	}
}