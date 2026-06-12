using System.Text;
using Silk.NET.Core.Native;

namespace Istok.WebGPU;

public static class WGPUStringViewExtension
{
	public readonly struct Scope(IntPtr labelPtr) : IDisposable
	{
		public void Dispose()
		{
			if (labelPtr != IntPtr.Zero)
				SilkMarshal.Free(labelPtr);
		}
	}

	extension(string? str)
	{
		public Scope ToWGPUStringView(out WGPUStringView stringView)
		{
			unsafe
			{
				if (str == null)
				{
					stringView = new WGPUStringView() { data = null, length = Strlen };
					return new Scope(0);
				}

				IntPtr labelPtr = SilkMarshal.StringToPtr(str);
				stringView = new WGPUStringView() { data = (byte*)labelPtr, length = (UIntPtr)(Encoding.UTF8.GetByteCount(str) + 1)};
				return new Scope(labelPtr);
			}
		}
	}
	
	extension(WGPUStringView stringView)
	{
		public string ToStr()
		{
			unsafe
			{
				return SilkMarshal.SpanToString(new Span<byte>(stringView.data, (int)stringView.length), NativeStringEncoding.LPTStr);
			}
		}

		public static WGPUStringView Empty =>
			new WGPUStringView()
			{
				data = null,
				length = Strlen
			};
	}
}