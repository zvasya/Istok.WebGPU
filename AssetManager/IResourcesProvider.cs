namespace AssetManager;

public interface IResourcesProvider
{
	Task<String> LoadTextAsync(string path);
	Task<byte[]> LoadBytesAsync(string path);
}