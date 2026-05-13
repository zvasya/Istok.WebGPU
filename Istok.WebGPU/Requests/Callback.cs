using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Istok.WebGPU.Requests;

public static class Callback<T>
{
	static uint _requestId;
	
	class UserData(uint id)
	{
		public readonly uint Id = id;
	}
	
	static readonly ConcurrentDictionary<uint, T> Requests =  new ConcurrentDictionary<uint, T>();

	public static GCHandle Register(T result)
	{
		var id = Interlocked.Increment(ref _requestId);
		
		var pin = GCHandle.Alloc(new UserData(id), GCHandleType.Pinned);

		Requests.TryAdd(id, result);
		return pin;
	}

	public static bool GetResult(GCHandle handle, [MaybeNullWhen(false)] out T result)
	{
		UserData? userData = (UserData?)handle.Target;
		
		if (userData != null && Requests.Remove(userData.Id, out var taskCompletionSource))
		{
			result = taskCompletionSource;
			handle.Free();
			return true;
		}
		
		result = default;
		return false;
	}
}