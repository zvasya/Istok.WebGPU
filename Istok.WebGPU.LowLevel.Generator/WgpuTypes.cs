namespace Istok.WebGPU.LowLevel.Generator;

public static class WgpuTypes
{
	private static readonly Dictionary<string, string> KnownType = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		{ "bool", "Bool32"},
		// { "char", "char"},
		{ "double", "double"},
		{ "float", "float"},
		{ "size_t", "UIntPtr"},
		{ "int", "int"},
		{ "int32_t", "int"},
		{ "uint8_t", "byte"},
		{ "uint16_t", "ushort"},
		{ "uint32_t", "uint"},
		{ "uint64_t", "ulong"},
		{ "void", "void"},
		{ "void *", "void *"},
		{ "void const *", "void *"},

		// webgpu.json primitive names
		{ "uint8", "byte"},
		{ "uint16", "ushort"},
		{ "uint32", "uint"},
		{ "uint64", "ulong"},
		{ "int32", "int"},
		{ "usize", "UIntPtr"},
		{ "float32", "float"},
		{ "nullable_float32", "float"},
		{ "float64_supertype", "double"},
		{ "c_void", "void"},

		// webgpu.json string variants
		{ "out_string", "WGPUStringView"},
		{ "nullable_string", "WGPUStringView"},
		{ "string_with_default_empty", "WGPUStringView"},
		{ "string view", "WGPUStringView"},
	};

	public static string ToKnownType(string type)
	{
		if (type.StartsWith("enum.", StringComparison.InvariantCulture)
			|| type.StartsWith("bitflag.", StringComparison.InvariantCulture)
			|| type.StartsWith("struct.", StringComparison.InvariantCulture)
			|| type.StartsWith("object.", StringComparison.InvariantCulture))
		{
			int dot = type.IndexOf('.');
			return "WGPU" + type[(dot + 1)..].ToPascalCase();
		}

		if (type.StartsWith("callback.", StringComparison.InvariantCulture))
		{
			return "WGPU" + type["callback.".Length..].ToPascalCase() + "CallbackInfo";
		}

		if (KnownType.TryGetValue(type, out var result))
			return result;

		return "WGPU" + type.ToPascalCase();
	}
	
	
	public static string SingularizeSnake(string name)
	{
		if (name.EndsWith("ies", StringComparison.InvariantCulture))
			return name[..^3] + "y";
		if (name.EndsWith("s", StringComparison.InvariantCulture) && !name.EndsWith("ss", StringComparison.InvariantCulture))
			return name[..^1];
		return name;
	}
	
	public static (string csType, string csValue, string csModifiers) MapConstant(string? value)
	{
		return value?.ToLowerInvariant() switch
		{
			"uint32_max" => ("uint", "uint.MaxValue", "const"),
			"uint64_max" => ("ulong", "ulong.MaxValue", "const"),
			"usize_max" => ("UIntPtr", "UIntPtr.MaxValue", "static readonly"),
			"nan" => ("float", "float.NaN", "const"),
			_ => throw new NotImplementedException($"Unknown constant type {value}"),
		};
	}
	
	public static string ToValidEnumName(string name)
	{
		if (name.Length > 0 && char.IsDigit(name[0]))
			return "d" + name;
		return name;
	}
	
	public static bool TryGetArrayInner(string type, out string inner)
	{
		const string prefix = "array<";
		if (type.StartsWith(prefix, StringComparison.InvariantCulture) && type.EndsWith(">", StringComparison.InvariantCulture))
		{
			inner = type.Substring(prefix.Length, type.Length - prefix.Length - 1);
			return true;
		}
		inner = "";
		return false;
	}

}
