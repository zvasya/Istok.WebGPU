using AssetManager;
using Xamarin.Essentials;

namespace ExampleAndroid;

public class ResourcesProvider : IResourcesProvider
{
	public async Task<String> LoadTextAsync(string path)
	{
		Stream? stream = await FileSystem.OpenAppPackageFileAsync(path);
		StreamReader reader = new StreamReader(stream);
		return await reader.ReadToEndAsync();
	}

	public async Task<byte[]> LoadBytesAsync(string path)
	{
		Stream? stream = await FileSystem.OpenAppPackageFileAsync(path);
		MemoryStream logoMemory = new MemoryStream();
		await stream.CopyToAsync(logoMemory);
		return logoMemory.GetBuffer();
	}
}