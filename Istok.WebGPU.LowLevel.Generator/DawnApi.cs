using System.Globalization;
using System.Text.Json;

namespace Istok.WebGPU.LowLevel.Generator;

public sealed class DawnApi
{
	private static readonly Dictionary<string, string> NativeTypes = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		{ "bool", "Bool32" },
		{ "char", "byte" },
		{ "double", "double" },
		{ "float", "float" },
		{ "int", "int" },
		{ "int32_t", "int" },
		{ "size_t", "UIntPtr" },
		{ "uint8_t", "byte" },
		{ "uint16_t", "ushort" },
		{ "uint32_t", "uint" },
		{ "uint64_t", "ulong" },
		{ "void", "void" },
		{ "void *", "void*" },
		{ "void const *", "void*" },
	};

	private static readonly HashSet<string> CSharpKeywords = new HashSet<string>(StringComparer.Ordinal)
	{
		"abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
		"continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
		"false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
		"internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
		"params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
		"sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
		"uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
	};

	private readonly string[] _enabledTags;
	private readonly string[] _disabledTags;
	private readonly Dictionary<string, JsonElement> _types = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
	private readonly Dictionary<string, bool> _structNeedsInit = new Dictionary<string, bool>(StringComparer.Ordinal);

	public DawnApi(JsonElement root, string[] enabledTags, string[] disabledTags)
	{
		_enabledTags = enabledTags;
		_disabledTags = disabledTags;
		foreach (JsonProperty property in root.EnumerateObject())
		{
			if (property.Name.StartsWith('_') || !IsEnabled(property.Value))
				continue;
			_types.Add(property.Name, property.Value);
		}
	}

	// item_is_enabled && !item_is_disabled from dawn_json_generator.py: an item is kept when it has
	// at least one enabled tag and no disabled tags.
	public bool IsEnabled(JsonElement item)
	{
		if (!item.TryGetProperty("tags", out JsonElement tags))
			return true;
		string?[] itemTags = tags.EnumerateArray().Select(tag => tag.GetString()).ToArray();
		return itemTags.Any(_enabledTags.Contains) && !itemTags.Any(_disabledTags.Contains);
	}

	public IEnumerable<JsonElement> EnabledItems(JsonElement parent, string property)
	{
		if (!parent.TryGetProperty(property, out JsonElement items))
			yield break;

		foreach (JsonElement item in items.EnumerateArray())
		{
			if (IsEnabled(item))
				yield return item;
		}
	}

	public IEnumerable<(string Name, JsonElement Json)> ByCategory(string category)
	{
		return _types
			.Where(t => Category(t.Value) == category)
			.Select(t => (t.Key, t.Value))
			.OrderBy(t => SortKey(t.Key), StringComparer.Ordinal);
	}

	public IEnumerable<JsonElement> Methods(JsonElement obj)
	{
		return EnabledItems(obj, "methods").OrderBy(m => SortKey(Name(m)), StringComparer.Ordinal);
	}

	public static string Name(JsonElement item) => item.GetProperty("name").GetString()!;

	public static string Category(JsonElement json) => json.GetProperty("category").GetString()!;

	public static string Annotation(JsonElement member)
		=> member.TryGetProperty("annotation", out JsonElement annotation) ? annotation.GetString()! : "value";

	// Same ordering as Name.__lt__ in dawn_json_generator.py.
	public static string SortKey(string name) => name.Replace(" ", "").ToLowerInvariant();

	// Name.CamelCase() from dawn_json_generator.py, used for C symbol names.
	public static string CName(string name)
		=> string.Concat(name.Split(' ').Select(chunk => char.ToUpperInvariant(chunk[0]) + chunk[1..]));

	public static string CsName(string name)
	{
		string result = name.ToCamelCase();
		return CSharpKeywords.Contains(result) ? "@" + result : result;
	}

	public static string EnumValueName(string name)
	{
		if (char.IsDigit(name[0]))
			name = "d" + name;
		return name.ToPascalCase();
	}

	public string CsType(string type)
	{
		if (!_types.TryGetValue(type, out JsonElement json))
			throw new InvalidOperationException($"Type '{type}' is referenced but filtered out by the current tags; adjust enabled/disabled tags.");
		return Category(json) switch
		{
			"native" => NativeTypes.TryGetValue(type, out string? cs) ? cs : throw new NotSupportedException($"Unknown native type '{type}'."),
			"typedef" => CsType(json.GetProperty("type").GetString()!),
			_ => "WGPU" + type.ToPascalCase(),
		};
	}

	public string CsAnnotatedType(JsonElement member)
	{
		string cs = CsType(member.GetProperty("type").GetString()!);
		return Annotation(member) switch
		{
			"value" => cs,
			"*" or "const*" => cs + "*",
			"const*const*" => cs + "**",
			var annotation => throw new NotSupportedException($"Unknown annotation '{annotation}'."),
		};
	}

	public string CsReturnType(JsonElement function)
	{
		if (!function.TryGetProperty("returns", out JsonElement returns))
			return "void";
		if (returns.ValueKind == JsonValueKind.String)
			return CsType(returns.GetString()!);
		return CsAnnotatedType(returns);
	}

	public IEnumerable<(string Name, long Value)> EnumValues(JsonElement enumJson)
	{
		foreach (JsonElement value in EnabledItems(enumJson, "values"))
		{
			string?[] tags = value.TryGetProperty("tags", out JsonElement tagsEl)
				? tagsEl.EnumerateArray().Select(t => t.GetString()).ToArray()
				: [];

			long prefix = 0;
			if (tags.Contains("dawn"))
				prefix = 0x0005_0000;
			else if (tags.Contains("emscripten"))
				prefix = 0x0004_0000;
			if (prefix == 0 && tags.Contains("native"))
				prefix = 0x0001_0000;

			yield return (Name(value), value.GetProperty("value").GetInt64() + prefix);
		}
	}

	public IEnumerable<(string Name, ulong Value)> BitmaskValues(JsonElement bitmaskJson)
	{
		return EnabledItems(bitmaskJson, "values").Select(v => (Name(v), v.GetProperty("value").GetUInt64()));
	}

	public bool HasFreeMembersFunction(JsonElement structure)
	{
		bool output = GetString(structure, "chained") == "out"
		              || GetString(structure, "extensible") == "out"
		              || (structure.TryGetProperty("out", out JsonElement outEl) && outEl.GetBoolean());
		if (!output)
			return false;

		return EnabledItems(structure, "members")
			.Any(m => Annotation(m) != "value" || m.GetProperty("type").GetString() == "string view");
	}

	public IEnumerable<string> ChainRoots(JsonElement structure)
	{
		if (!structure.TryGetProperty("chain roots", out JsonElement roots))
			return [];
		return roots.EnumerateArray().Select(r => r.GetString()!).Where(_types.ContainsKey);
	}

	public string? ChainedSType(string structName)
	{
		JsonElement sType = _types["s type"];
		return EnumValues(sType).Any(v => v.Name == structName) ? "WGPUSType." + EnumValueName(structName) : null;
	}

	// Mirrors render_c_default_value from generator/templates/api.h; returns null for zero-initialized members.
	public string? DefaultValue(JsonElement member)
	{
		if (Annotation(member) != "value")
			return null;

		string type = member.GetProperty("type").GetString()!;
		JsonElement typeJson = _types[type];
		string? defaultText = member.TryGetProperty("default", out JsonElement defEl)
			? defEl.ValueKind == JsonValueKind.String ? defEl.GetString() : defEl.GetRawText()
			: null;

		switch (Category(typeJson))
		{
			case "enum":
			{
				var values = EnumValues(typeJson).ToList();
				string? valueName = values.Any(v => v.Name == "undefined") ? "undefined" : defaultText;
				return valueName == null ? null : EnumDefault(type, values, valueName);
			}
			case "bitmask":
			{
				string valueName = defaultText ?? "none";
				var values = BitmaskValues(typeJson).Select(v => (v.Name, (long)v.Value)).ToList();
				return EnumDefault(type, values, valueName);
			}
			case "structure":
			case "callback info":
				return defaultText != "zero" && StructNeedsInit(type) ? "new()" : null;
			case "native":
				return defaultText == null ? null : NativeDefault(type, defaultText);
			default:
				return null;
		}
	}

	private string? EnumDefault(string type, List<(string Name, long Value)> values, string valueName)
	{
		foreach ((string name, long value) in values)
		{
			if (name == valueName)
				return value == 0 ? null : CsType(type) + "." + EnumValueName(name);
		}
		throw new NotSupportedException($"Unknown default value '{valueName}' for type '{type}'.");
	}

	private string? NativeDefault(string type, string text)
	{
		if (type == "bool")
			return text == "true" ? "true" : null;
		if (text == "nullptr")
			return null;
		if (_types.TryGetValue(text, out JsonElement constant) && Category(constant) == "constant")
			return "WebGPUNative." + text.ToPascalCase();

		if (type is "float" or "double")
		{
			double value = double.Parse(text.TrimEnd('f', 'F'), CultureInfo.InvariantCulture);
			if (value == 0)
				return null;
			return value.ToString("R", CultureInfo.InvariantCulture) + (type == "float" ? "f" : "");
		}

		ulong number = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
			? ulong.Parse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture)
			: ulong.Parse(text, CultureInfo.InvariantCulture);
		return number == 0 ? null : text;
	}

	public bool StructNeedsInit(string type)
	{
		if (_structNeedsInit.TryGetValue(type, out bool result))
			return result;

		JsonElement json = _types[type];
		result = (IsChained(json) && ChainedSType(type) != null)
		         || EnabledItems(json, "members").Any(m => DefaultValue(m) != null);
		_structNeedsInit[type] = result;
		return result;
	}

	public static (string CsValue, string Modifiers) ConstantValue(string csType, string value)
	{
		string modifiers = csType == "UIntPtr" ? "static readonly" : "const";
		if (value.EndsWith("_MAX", StringComparison.Ordinal))
			return (csType + ".MaxValue", modifiers);
		if (value == "NAN")
			return (csType + ".NaN", modifiers);
		throw new NotSupportedException($"Unknown constant value '{value}'.");
	}

	public static bool IsExtensible(JsonElement structure) => GetString(structure, "extensible") != null;

	public static bool IsChained(JsonElement structure) => GetString(structure, "chained") != null;

	private static string? GetString(JsonElement json, string property)
		=> json.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
