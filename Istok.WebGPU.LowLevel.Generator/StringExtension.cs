namespace Istok.WebGPU.LowLevel.Generaror;

public static class StringExtension
{
	extension(string text)
	{
		public string ToCamelCase()
		{
			return Internal(text, false);
		}

		public string ToPascalCase()
		{
			return Internal(text, true);
		}

		static string Internal(string str, bool flag)
		{
			Span<char> span = stackalloc char[str.Length];
			ReadOnlySpan<char> source = str.AsSpan();
			int index = 0;
			int length = str.Length;
			for (int i = 0; i < length; i++)
			{
				char c = source[i];
				if (char.IsLetter(c))
				{
					if (flag)
					{
						span[index++] = Char.ToUpperInvariant(source[i]);
						flag = false;
					}
					else
						span[index++] = c;
				}
				else
				{
					flag = true;
					if (char.IsDigit(c))
						span[index++] = c;
				}
				
			}

			return new string(span[..index]);
		}
	}
}