using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Istok.WebGPU.LowLevel.Generator;

// Tag sets used by dawn_json_generator.py:
//   include/dawn/webgpu.h:            enabled = dawn, emscripten, native, deprecated
//   Dawn proc table / native:         enabled = dawn, native, deprecated
//   webgpu-headers/webgpu.h upstream: enabled = native; disabled = dawn
//   emdawnwebgpu webgpu.h:            enabled = emscripten
string[] enabledTags = ["native"];
string[] disabledTags = ["dawn"];

using JsonDocument doc = ReadJson(Path.Combine(AppContext.BaseDirectory, "codegen", "dawn.json"));
DawnApi api = new DawnApi(doc.RootElement, enabledTags, disabledTags);

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

EmitConstants(constantsSb, api);
EmitEnums(enumsSb, api);
EmitBitmasks(enumsSb, api);
EmitStructs(structsSb, api);
EmitObjects(objectsSb, api);
EmitDelegates(delegatesSb, api);
EmitFunctions(functionsSb, api);

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

static void EmitConstants(StringBuilder constantsSb, DawnApi api)
{
	foreach ((string name, JsonElement json) in api.ByCategory("constant"))
	{
		string csType = api.CsType(json.GetProperty("type").GetString()!);
		var (csValue, modifiers) = DawnApi.ConstantValue(csType, json.GetProperty("value").GetString()!);
		constantsSb.Append("\tpublic ").Append(modifiers).Append(' ').Append(csType).Append(' ')
			.Append(name.ToPascalCase()).Append(" = ").Append(csValue).AppendLine(";");
	}
}

static void EmitEnums(StringBuilder enumsSb, DawnApi api)
{
	foreach ((string name, JsonElement json) in api.ByCategory("enum"))
	{
		enumsSb.Append("public enum ").AppendLine(api.CsType(name));
		enumsSb.AppendLine("{");
		foreach ((string valueName, long value) in api.EnumValues(json))
			enumsSb.Append('\t').Append(DawnApi.EnumValueName(valueName)).Append(" = 0x").Append(value.ToString("X8")).AppendLine(",");
		enumsSb.AppendLine("}");
		enumsSb.AppendLine();
	}
}

static void EmitBitmasks(StringBuilder enumsSb, DawnApi api)
{
	foreach ((string name, JsonElement json) in api.ByCategory("bitmask"))
	{
		enumsSb.AppendLine("[Flags]");
		enumsSb.Append("public enum ").Append(api.CsType(name)).AppendLine(" : ulong");
		enumsSb.AppendLine("{");
		foreach ((string valueName, ulong value) in api.BitmaskValues(json))
			enumsSb.Append('\t').Append(DawnApi.EnumValueName(valueName)).Append(" = 0x").Append(value.ToString("X16")).AppendLine(",");
		enumsSb.AppendLine("}");
		enumsSb.AppendLine();
	}
}

static void EmitStructs(StringBuilder structsSb, DawnApi api)
{
	foreach ((string name, JsonElement json) in api.ByCategory("callback info"))
		EmitStruct(structsSb, api, name, json, true);

	foreach ((string name, JsonElement json) in api.ByCategory("structure"))
		EmitStruct(structsSb, api, name, json, false);
}

static void EmitStruct(StringBuilder structsSb, DawnApi api, string name, JsonElement json, bool isCallbackInfo)
{
	foreach (string root in api.ChainRoots(json))
		structsSb.Append("// Can be chained in ").AppendLine(api.CsType(root));

	structsSb.AppendLine("[StructLayout(LayoutKind.Sequential)]");
	structsSb.Append("public unsafe struct ").Append(api.CsType(name)).AppendLine("()");
	structsSb.AppendLine("{");

	if (isCallbackInfo || DawnApi.IsExtensible(json))
		structsSb.AppendLine("\tpublic ChainedStruct* nextInChain;");

	if (DawnApi.IsChained(json))
	{
		string? sType = api.ChainedSType(name);
		structsSb.Append("\tpublic ChainedStruct chain");
		if (sType != null)
			structsSb.Append(" = new() { sType = ").Append(sType).Append(" }");
		structsSb.AppendLine(";");
	}

	foreach (JsonElement member in api.EnabledItems(json, "members"))
	{
		structsSb.Append("\tpublic ").Append(api.CsAnnotatedType(member)).Append(' ').Append(DawnApi.CsName(DawnApi.Name(member)));
		string? defaultValue = api.DefaultValue(member);
		if (defaultValue != null)
			structsSb.Append(" = ").Append(defaultValue);
		structsSb.AppendLine(";");
	}

	if (isCallbackInfo)
	{
		structsSb.AppendLine("\tpublic void* userdata1;");
		structsSb.AppendLine("\tpublic void* userdata2;");
	}

	structsSb.AppendLine("}");
	structsSb.AppendLine();
}

static void EmitObjects(StringBuilder objectsSb, DawnApi api)
{
	foreach ((string name, _) in api.ByCategory("object"))
	{
		string type = api.CsType(name);
		objectsSb.Append("public record struct ").Append(type).AppendLine("(IntPtr Handle)");
		objectsSb.AppendLine("{");
		objectsSb.AppendLine("\tpublic readonly IntPtr Handle = Handle;");
		objectsSb.Append("\tpublic static ").Append(type).Append(" Null => new ").Append(type).AppendLine("(IntPtr.Zero);");
		objectsSb.Append("\tpublic static implicit operator ").Append(type).Append("(IntPtr handle) => new ").Append(type).AppendLine("(handle);");
		objectsSb.AppendLine("}");
		objectsSb.AppendLine();
	}
}

static void EmitDelegates(StringBuilder delegatesSb, DawnApi api)
{
	foreach ((string name, JsonElement json) in api.ByCategory("function pointer"))
		EmitDelegate(delegatesSb, api, name, json, false);

	foreach ((string name, JsonElement json) in api.ByCategory("callback function"))
		EmitDelegate(delegatesSb, api, name, json, true);
}

static void EmitDelegate(StringBuilder delegatesSb, DawnApi api, string name, JsonElement json, bool withUserdata)
{
	StringBuilder pointerType = new StringBuilder("delegate* unmanaged[Cdecl]<");
	foreach (JsonElement arg in api.EnabledItems(json, "args"))
		pointerType.Append(api.CsAnnotatedType(arg)).Append(", ");
	if (withUserdata)
		pointerType.Append("void*, void*, ");
	pointerType.Append(api.CsReturnType(json)).Append('>');

	string type = api.CsType(name);
	delegatesSb.Append("public unsafe readonly struct ").AppendLine(type);
	delegatesSb.AppendLine("{");
	delegatesSb.AppendLine("\tprivate readonly void* _handle;");
	delegatesSb.Append("\tpublic ").Append(pointerType).Append(" Handle => (").Append(pointerType).AppendLine(") _handle;");
	delegatesSb.Append("\tpublic ").Append(type).Append('(').Append(pointerType).AppendLine(" ptr) => _handle = ptr;");
	delegatesSb.AppendLine("}");
	delegatesSb.AppendLine();
}

static void EmitFunctions(StringBuilder functionsSb, DawnApi api)
{
	foreach ((string name, JsonElement json) in api.ByCategory("function").OrderBy(f => f.Name == "get proc address"))
		EmitFunction(functionsSb, api, DawnApi.CName(name), null, json);

	// Same order and extra methods as c_methods_sorted_by_parent in dawn_json_generator.py.
	var parents = api.ByCategory("object").Concat(api.ByCategory("structure"))
		.OrderBy(t => DawnApi.SortKey(t.Name), StringComparer.Ordinal);

	foreach ((string name, JsonElement json) in parents)
	{
		string self = api.CsType(name) + " " + DawnApi.CsName(name);
		string prefix = DawnApi.CName(name);

		if (DawnApi.Category(json) == "object")
		{
			foreach (JsonElement method in api.Methods(json))
				EmitFunction(functionsSb, api, prefix + DawnApi.CName(DawnApi.Name(method)), self, method);
			EmitFunction(functionsSb, api, prefix + "AddRef", self, null);
			EmitFunction(functionsSb, api, prefix + "Release", self, null);
		}
		else if (api.HasFreeMembersFunction(json))
		{
			EmitFunction(functionsSb, api, prefix + "FreeMembers", self, null);
		}
	}
}

static void EmitFunction(StringBuilder functionsSb, DawnApi api, string cName, string? self, JsonElement? json)
{
	functionsSb.Append("\t[LibraryImport(WebGPULib, EntryPoint = \"wgpu").Append(cName).AppendLine("\")]");
	functionsSb.Append("\tpublic static partial ").Append(json is { } returnsJson ? api.CsReturnType(returnsJson) : "void");
	functionsSb.Append(" wgpu").Append(cName).Append('(');

	List<string> args = [];
	if (self != null)
		args.Add(self);
	if (json is { } argsJson)
	{
		foreach (JsonElement arg in api.EnabledItems(argsJson, "args"))
			args.Add(api.CsAnnotatedType(arg) + " " + DawnApi.CsName(DawnApi.Name(arg)));
	}

	functionsSb.AppendJoin(", ", args).AppendLine(");");
}
