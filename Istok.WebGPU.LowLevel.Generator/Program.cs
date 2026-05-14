using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Istok.WebGPU.LowLevel.Generator;
using static Istok.WebGPU.LowLevel.Generator.WgpuTypes;

using JsonDocument doc = ReadJson(Path.Combine(AppContext.BaseDirectory, "codegen", "webgpu.json"));
JsonElement root = doc.RootElement;

StringBuilder enumsSb = new StringBuilder();
StringBuilder constantsSb = new StringBuilder();
StringBuilder objectsSb = new StringBuilder();
StringBuilder structsSb = new StringBuilder();
StringBuilder functionsSb = new StringBuilder();
StringBuilder delegatesSb = new StringBuilder();

AddHeader(enumsSb);
AddHeader(constantsSb);
AddHeader(objectsSb);
AddHeader(structsSb, "using System.Runtime.InteropServices;");
AddHeader(functionsSb, "using System.Runtime.InteropServices;");
AddHeader(delegatesSb, "using System.Runtime.InteropServices;");

StartClass(constantsSb);
StartClass(functionsSb);

EmitConstants(constantsSb, root);
EmitEnums(enumsSb, root);
EmitBitflags(enumsSb, root);
EmitStructs(structsSb, functionsSb, root);
EmitObjects(objectsSb, functionsSb, root);
EmitCallbacks(delegatesSb, structsSb, root);
EmitFunctions(functionsSb, root);

EndClass(constantsSb);
EndClass(functionsSb);

string path = Path.Combine(Directory.GetParent(Path.GetDirectoryName(GetSourceFilePathName())!)!.FullName, "Istok.WebGPU.LowLevel", "Generated");

File.WriteAllText(Path.Combine(path, "Enums.cs"), enumsSb.ToString());
File.WriteAllText(Path.Combine(path, "Constants.cs"), constantsSb.ToString());
File.WriteAllText(Path.Combine(path, "Objects.cs"), objectsSb.ToString());
File.WriteAllText(Path.Combine(path, "Structs.cs"), structsSb.ToString());
File.WriteAllText(Path.Combine(path, "Functions.cs"), functionsSb.ToString());
File.WriteAllText(Path.Combine(path, "Delegates.cs"), delegatesSb.ToString());

return;

static JsonDocument ReadJson(string path)
{
	ReadOnlyMemory<byte> codegenReadOnlySpan = File.ReadAllBytes(path);
	JsonDocument doc = JsonDocument.Parse(codegenReadOnlySpan);
	return doc;
}

static string GetSourceFilePathName([CallerFilePath] string? callerFilePath = null)
	=> callerFilePath ?? "";

static void AddHeader(StringBuilder stringBuilder, params string[] additionalUsings)
{
	foreach (string additional in additionalUsings)
		stringBuilder.AppendLine(additional);
	stringBuilder.AppendLine("using Silk.NET.Core;").AppendLine();
	stringBuilder.AppendLine("namespace Istok.WebGPU.LowLevel;");
	stringBuilder.AppendLine();
}

static void StartClass(StringBuilder stringBuilder)
{
	stringBuilder.AppendLine("public static unsafe partial class WebGPUNative");
	stringBuilder.AppendLine("{");
}

static void EndClass(StringBuilder stringBuilder)
{
	stringBuilder.AppendLine("}");
}

static void EmitConstants(StringBuilder constantsSb, JsonElement root)
{
	if (!root.TryGetProperty("constants", out JsonElement constantsEl))
		return;

	foreach (JsonElement c in constantsEl.EnumerateArray())
	{
		string? name = c.GetProperty("name").GetString();
		string? value = c.GetProperty("value").GetString();
		var (csType, csValue, csModifiers) = MapConstant(value);
		constantsSb.Append("\tpublic ").Append(csModifiers).Append(" ").Append(csType).Append(" ")
			.Append(name!.ToPascalCase()).Append(" = ").Append(csValue).AppendLine(";");
	}
}

static void EmitEnums(StringBuilder enumSb, JsonElement root)
{
	if (!root.TryGetProperty("enums", out JsonElement enumsEl))
		return;

	foreach (JsonElement e in enumsEl.EnumerateArray())
	{
		string? name = e.GetProperty("name").GetString();
		enumSb.Append("public enum ").AppendLine(ToKnownType(name!));
		enumSb.AppendLine("{");

		int i = 0;
		foreach (JsonElement entry in e.GetProperty("entries").EnumerateArray())
		{
			if (entry.ValueKind == JsonValueKind.Null)
			{
				i++;
				continue;
			}

			string? entryName = entry.GetProperty("name").GetString()!;
			entryName = ToValidEnumName(entryName);
			enumSb.Append("\t").Append(entryName.ToPascalCase()).Append(" = ").Append(i).AppendLine(",");
			i++;
		}

		enumSb.AppendLine("}");
		enumSb.AppendLine();
	}
}

static void EmitBitflags(StringBuilder enumSb, JsonElement root)
{
	if (!root.TryGetProperty("bitflags", out JsonElement bitflagsEl))
		return;

	foreach (JsonElement b in bitflagsEl.EnumerateArray())
	{
		string? name = b.GetProperty("name").GetString();
		
		var entries = b.GetProperty("entries");
		int idx = 0;

		enumSb.AppendLine("[Flags]");
		enumSb.Append("public enum ").Append(ToKnownType(name!)).AppendLine(" : ulong");
		enumSb.AppendLine("{");

		idx = 0;
		foreach (JsonElement entry in entries.EnumerateArray())
		{
			if (entry.ValueKind == JsonValueKind.Null)
			{
				idx++;
				continue;
			}

			string entryName = entry.GetProperty("name").GetString()!;
			if (entry.TryGetProperty("value_combination", out JsonElement combEl))
			{
				entryName = ToValidEnumName(entryName);
				enumSb.Append("\t").Append(entryName.ToPascalCase()).Append(" = ");
				enumSb.AppendJoin(" | ", combEl.EnumerateArray().Select(combName => ToValidEnumName(combName.GetString()!).ToPascalCase()));
				enumSb.AppendLine(",");
			}
			else
			{
				entryName = ToValidEnumName(entryName);
				enumSb.Append("\t").Append(entryName.ToPascalCase()).Append(" = ").Append(idx == 0 ? 0UL : 1UL << (idx - 1)).AppendLine(",");
			}

			idx++;
		}

		enumSb.AppendLine("}");
		enumSb.AppendLine();
	}
}

static void EmitStructs(StringBuilder structsSb, StringBuilder functionsSb, JsonElement root)
{
	if (!root.TryGetProperty("structs", out JsonElement structsEl))
		return;

	foreach (JsonElement s in structsEl.EnumerateArray())
	{
		string? name = s.GetProperty("name").GetString()!;
		string? structType = s.TryGetProperty("type", out JsonElement typeEl) ? typeEl.GetString() : null;

		structsSb.AppendLine("[StructLayout(LayoutKind.Sequential)]");
		structsSb.Append("public unsafe struct ").Append(ToKnownType(name)).AppendLine("()");
		structsSb.AppendLine("{");

		EmitChainPrefix(structsSb, structType);

		if (s.TryGetProperty("members", out JsonElement membersEl))
		{
			foreach (JsonElement m in membersEl.EnumerateArray())
			{
				EmitMember(structsSb, m);
			}
		}

		structsSb.AppendLine("}");
		structsSb.AppendLine();

		bool freeMembers = s.TryGetProperty("free_members", out JsonElement freeEl) && freeEl.GetBoolean();
		if (freeMembers)
		{
			string firstArg = ToKnownType(name) + " " + name.ToCamelCase();
			AddFunction(functionsSb, name.ToPascalCase() + "FreeMembers", firstArg);
		}
	}
}

static void EmitChainPrefix(StringBuilder structsSb, string? structType)
{
	switch (structType)
	{
		case "extensible":
		case "extensible_callback_arg":
			structsSb.AppendLine("\t\tpublic ChainedStruct* nextInChain;");
			break;
		case "extension":
			structsSb.AppendLine("\t\tpublic ChainedStruct chain;");
			break;
	}
}

static void EmitMember(StringBuilder sb, JsonElement member)
{
	string? memberName = member.GetProperty("name").GetString()!;
	string? type = member.GetProperty("type").GetString()!;
	string? pointer = member.TryGetProperty("pointer", out JsonElement pEl) ? pEl.GetString() : null;

	if (TryGetArrayInner(type, out string inner))
	{
		string countName = SingularizeSnake(memberName) + "_count";
		sb.Append("\t\tpublic UIntPtr ").Append(countName.ToCamelCase()).AppendLine(";");
		sb.Append("\t\tpublic ").Append(ToKnownType(inner)).Append("* ").Append(memberName.ToCamelCase()).AppendLine(";");
		return;
	}

	sb.Append("\t\tpublic ").Append(ToKnownType(type));
	if (pointer != null)
		sb.Append("*");
	sb.Append(" ").Append(memberName.ToCamelCase());

	if (TryGetMemberDefault(member, out string csExpression))
	{
		sb.Append(" = ").Append(csExpression);
	}
	sb.AppendLine(";");
}

static void EmitObjects(StringBuilder objectsSb, StringBuilder functionsSb, JsonElement root)
{
	if (!root.TryGetProperty("objects", out JsonElement objectsEl))
		return;

	foreach (JsonElement o in objectsEl.EnumerateArray())
	{
		string? name = o.GetProperty("name").GetString()!;
		string prefix = name.ToPascalCase();
		string firstArg = ToKnownType(name) + " " + name.ToCamelCase();

		if (o.TryGetProperty("methods", out JsonElement methodsEl))
		{
			foreach (JsonElement method in methodsEl.EnumerateArray())
			{
				string? methodName = method.GetProperty("name").GetString()!;
				string fullName = prefix + methodName.ToPascalCase();
				ParseFunction(functionsSb, fullName, firstArg, method);
			}
		}

		AddFunction(functionsSb, prefix + "AddRef", firstArg);
		AddFunction(functionsSb, prefix + "Release", firstArg);

		string type = ToKnownType(name);
		objectsSb.Append("public record struct ").Append(type).AppendLine("(IntPtr Handle)");
		objectsSb.AppendLine("{");
		objectsSb.AppendLine("\tpublic readonly IntPtr Handle = Handle;");
		objectsSb.Append("\tpublic static ").Append(type).Append(" Null => new ").Append(type).AppendLine("(IntPtr.Zero);");
		objectsSb.Append("\tpublic static implicit operator ").Append(type).Append("(IntPtr handle) => new ").Append(type).AppendLine("(handle);");
		objectsSb.AppendLine("}");
		objectsSb.AppendLine();
	}
}

static void EmitCallbacks(StringBuilder delegatesSb, StringBuilder structsSb, JsonElement root)
{
	if (!root.TryGetProperty("callbacks", out JsonElement callbacksEl))
		return;

	foreach (JsonElement c in callbacksEl.EnumerateArray())
	{
		string? name = c.GetProperty("name").GetString()!;
		string callbackName = ToKnownType(name) + "Callback";

		delegatesSb.Append("public unsafe readonly struct ").AppendLine(callbackName);
		delegatesSb.AppendLine("{");
		delegatesSb.AppendLine("\tprivate readonly void* _handle;");

		string unmanagedDelegatePtr = GetUnmanagedDelegateType(c);
		delegatesSb.Append("\tpublic ").Append(unmanagedDelegatePtr).Append(" Handle => (").Append(unmanagedDelegatePtr).AppendLine(") _handle;");
		delegatesSb.Append("\tpublic ").Append(callbackName).Append("(").Append(unmanagedDelegatePtr).AppendLine(" ptr) => _handle = ptr;");

		delegatesSb.AppendLine("}");
		delegatesSb.AppendLine();

		string? style = c.TryGetProperty("style", out JsonElement styleEl) ? styleEl.GetString() : null;
		EmitCallbackInfoStruct(structsSb, name, style);
	}
}

static void EmitCallbackInfoStruct(StringBuilder structsSb, string callbackName, string? style)
{
	string structName = "WGPU" + callbackName.ToPascalCase() + "CallbackInfo";
	string callbackTypeName = "WGPU" + callbackName.ToPascalCase() + "Callback";

	structsSb.AppendLine("\t[StructLayout(LayoutKind.Sequential)]");
	structsSb.Append("\tpublic unsafe struct ").AppendLine(structName);
	structsSb.AppendLine("\t{");
	structsSb.AppendLine("\t\tpublic ChainedStruct* nextInChain;");
	if (style == "callback_mode")
		structsSb.AppendLine("\t\tpublic WGPUCallbackMode mode;");
	structsSb.Append("\t\tpublic ").Append(callbackTypeName).AppendLine(" callback;");
	structsSb.AppendLine("\t\tpublic void* userdata1;");
	structsSb.AppendLine("\t\tpublic void* userdata2;");
	structsSb.AppendLine("\t}");
}

static void EmitFunctions(StringBuilder functionsSb, JsonElement root)
{
	if (!root.TryGetProperty("functions", out JsonElement functionsEl))
		return;

	foreach (JsonElement f in functionsEl.EnumerateArray())
	{
		string name = f.GetProperty("name").GetString()!;
		ParseFunction(functionsSb, name.ToPascalCase(), null, f);
	}
}

static string GetUnmanagedDelegateType(JsonElement jsonElement)
{
	StringBuilder sb = new StringBuilder();
	sb.Append("delegate* unmanaged[Cdecl]<");

	if (jsonElement.TryGetProperty("args", out JsonElement argsEl))
	{
		foreach (JsonElement arg in argsEl.EnumerateArray())
		{
			AppendArgType(sb, arg);
			sb.Append(", ");
		}
	}

	sb.Append("void*, void*, ");

	sb.Append(GetReturns(jsonElement));
	sb.Append(">");
	return sb.ToString();
}

static void AppendArgType(StringBuilder sb, JsonElement arg)
{
	string? type = arg.GetProperty("type").GetString()!;
	string? pointer = arg.TryGetProperty("pointer", out JsonElement pEl) ? pEl.GetString() : null;

	if (TryGetArrayInner(type, out string inner))
	{
		sb.Append("UIntPtr, ");
		sb.Append(ToKnownType(inner)).Append("*");
		return;
	}

	sb.Append(ToKnownType(type));
	if (pointer != null)
		sb.Append("*");
}

static string GetReturns(JsonElement jsonElement)
{
	if (!jsonElement.TryGetProperty("returns", out JsonElement returnsEl))
	{
		// Methods/functions with a top-level `callback` key implicitly return WGPUFuture.
		if (jsonElement.TryGetProperty("callback", out _))
			return "WGPUFuture";
		return "void";
	}

	if (returnsEl.ValueKind == JsonValueKind.String)
		return ToKnownType(returnsEl.GetString()!);

	string type = returnsEl.GetProperty("type").GetString()!;
	string mapped = ToKnownType(type);
	if (returnsEl.TryGetProperty("pointer", out JsonElement pEl) && pEl.GetString() != null)
		mapped += "*";
	return mapped;
}

static void AddFunction(StringBuilder functionsSb, string functionName, string? firstArg)
{
	// functionsSb.Append("\t[DllImport(\"wgpu_native\", CallingConvention = CallingConvention.Cdecl, EntryPoint = \"").Append("wgpu").Append(functionName).AppendLine("\")]");
	// functionsSb.Append("\tpublic static extern void wgpu").Append(functionName).Append("(").Append(firstArg).AppendLine(");");
	functionsSb.Append("\t[LibraryImport(WebGPULib, EntryPoint = \"").Append("wgpu").Append(functionName).AppendLine("\")]");
	functionsSb.Append("\tpublic static partial void wgpu").Append(functionName).Append("(").Append(firstArg).AppendLine(");");
}

static void ParseFunction(StringBuilder functionsSb, string functionName, string? firstArg, JsonElement jsonElement)
{
	// functionsSb.Append("\t[DllImport(\"wgpu_native\", CallingConvention = CallingConvention.Cdecl, EntryPoint = \"").Append("wgpu").Append(functionName).AppendLine("\")]");
	// functionsSb.Append("\tpublic static extern ");
	functionsSb.Append("\t[LibraryImport(WebGPULib, EntryPoint = \"").Append("wgpu").Append(functionName).AppendLine("\")]");
	functionsSb.Append("\tpublic static partial ");

	functionsSb.Append(GetReturns(jsonElement));

	functionsSb.Append(" wgpu").Append(functionName).Append("(");

	bool first = true;

	if (firstArg != null)
	{
		functionsSb.Append(firstArg);
		first = false;
	}

	if (jsonElement.TryGetProperty("args", out JsonElement argsEl))
	{
		foreach (JsonElement arg in argsEl.EnumerateArray())
		{
			string? argName = arg.GetProperty("name").GetString()!;
			string? type = arg.GetProperty("type").GetString()!;

			if (!first)
				functionsSb.Append(", ");
			
			if (TryGetArrayInner(type, out string inner))
			{
				string countName = SingularizeSnake(argName) + "_count";
				functionsSb.Append("UIntPtr ").Append(countName.ToCamelCase());
				functionsSb.Append(", ");
				functionsSb.Append(ToKnownType(inner)).Append("* ").Append(argName.ToCamelCase());
			}
			else
			{
				functionsSb.Append(ToKnownType(type));
				string? pointer = arg.TryGetProperty("pointer", out JsonElement pEl) ? pEl.GetString() : null;
				if (pointer != null)
					functionsSb.Append("*");
				functionsSb.Append(" ").Append(argName.ToCamelCase());
			}
			first = false;
		}
	}

	if (jsonElement.TryGetProperty("callback", out JsonElement callbackEl))
	{
		if (!first)
			functionsSb.Append(", ");
		functionsSb.Append(ToKnownType(callbackEl.GetString()!)).Append(" callbackInfo");
	}

	functionsSb.AppendLine(");");
}

static bool TryGetMemberDefault(JsonElement member, out string csExpression)
{
	csExpression = "";
	if (!member.TryGetProperty("default", out JsonElement defEl))
		return false;

	string type = member.GetProperty("type").GetString()!;

	switch (defEl.ValueKind)
	{
		case JsonValueKind.Null:
			return false;

		case JsonValueKind.False:
			csExpression = "false";
			return true;

		case JsonValueKind.True:
			csExpression = "true";
			return true;

		case JsonValueKind.Number:
		{
			string raw = defEl.GetRawText();
			csExpression = raw; // TODO: process float?
			return true;
		}

		case JsonValueKind.String:
		{
			string value = defEl.GetString()!;

			if (value == "zero")
				return false;

			if (value.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
			{
				csExpression = value;
				return true;
			}

			const string constantPrefix = "constant.";
			if (value.StartsWith(constantPrefix, StringComparison.InvariantCulture))
			{
				csExpression = "WebGPUNative." + value[constantPrefix.Length..].ToPascalCase();
				return true;
			}

			if (type.StartsWith("enum.", StringComparison.InvariantCulture)
			    || type.StartsWith("bitflag.", StringComparison.InvariantCulture))
			{
				csExpression = ToKnownType(type) + "." + ToValidEnumName(value).ToPascalCase();
				return true;
			}

			throw new NotSupportedException($"Unknown default value '{value}' for type '{type}'.");
		}

		default:
			throw new NotSupportedException($"Unsupported default JSON kind '{defEl.ValueKind}' for type '{type}'.");
	}
}
