using AssetManager;

namespace ExampleBrowser;

public class ResourcesProvider(HttpClient http) : IResourcesProvider
{
	public async Task<string> LoadTextAsync(string path)
	{
		Stream? stream = await http.GetStreamAsync(path);
		StreamReader reader = new StreamReader(stream);
		return await reader.ReadToEndAsync();
	}

	public async Task<byte[]> LoadBytesAsync(string path)
	{
		Stream? stream = await http.GetStreamAsync(path);
		MemoryStream logoMemory = new MemoryStream();
		await stream.CopyToAsync(logoMemory);
		return logoMemory.GetBuffer();
	}
}