"""Reject native Dawn revisions that break the shared generated C API subset."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def enabled(item):
    tags = item.get("tags")
    return tags is None or ("native" in tags and "dawn" not in tags)


def fields(items):
    return [(item["name"], item["type"], item.get("annotation"))
            for item in items if enabled(item)]


def signature(item):
    return item.get("returns", "void"), fields(item.get("args", []))


def check_api(native_json, bindings_json=None):
    bindings_json = bindings_json or ROOT / "Istok.WebGPU.LowLevel.Generator/codegen/dawn.json"
    bindings = json.loads(Path(bindings_json).read_text())
    native = json.loads(Path(native_json).read_text())
    errors = []
    for name, old in bindings.items():
        if name.startswith("_") or not isinstance(old, dict) or not enabled(old):
            continue
        new = native.get(name)
        if not isinstance(new, dict) or not enabled(new) or old["category"] != new["category"]:
            errors.append(f"{name}: removed or changed type")
            continue
        category = old["category"]
        if category == "structure":
            if (fields(old.get("members", [])) != fields(new.get("members", [])) or
                    any(old.get(key) != new.get(key) for key in ("chained", "extensible"))):
                errors.append(f"{name}: structure layout changed")
        elif category in ("enum", "bitmask"):
            values = {v["name"]: v["value"] for v in new["values"] if enabled(v)}
            for value in old["values"]:
                if enabled(value) and values.get(value["name"]) != value["value"]:
                    errors.append(f"{name}.{value['name']}: enum value changed")
        elif category == "object":
            methods = {v["name"]: v for v in new.get("methods", []) if enabled(v)}
            for method in old.get("methods", []):
                if enabled(method) and (method["name"] not in methods or
                                       signature(method) != signature(methods[method["name"]])):
                    errors.append(f"{name}.{method['name']}: method signature changed")
        elif category in ("function", "callback function") and signature(old) != signature(new):
            errors.append(f"{name}: function signature changed")
        elif category == "typedef" and old.get("type") != new.get("type"):
            errors.append(f"{name}: typedef changed")
    if errors:
        raise RuntimeError("Native Dawn is incompatible with the shared bindings:\n" + "\n".join(errors))
    print("Native Dawn C API is compatible with the shared bindings subset", flush=True)
