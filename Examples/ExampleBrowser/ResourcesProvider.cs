using AssetManager;

namespace ExampleBrowser;

public class ResourcesProvider(HttpClient http) : IResourcesProvider
{
	public async Task<string> LoadTextAsync(string path)
	{
		using Stream stream = await http.GetStreamAsync(path);
		using StreamReader reader = new StreamReader(stream);
		return await reader.ReadToEndAsync();
	}

	public async Task<byte[]> LoadBytesAsync(string path)
	{
		using Stream stream = await http.GetStreamAsync(path);
		using MemoryStream memory = new MemoryStream();
		await stream.CopyToAsync(memory);
		return memory.ToArray();
	}
}
