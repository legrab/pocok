import ast
import json
import sys

DENIED_NAMES = {
    "eval", "exec", "compile", "__import__", "open", "input", "breakpoint",
    "globals", "locals", "vars", "getattr", "setattr", "delattr"
}
DENIED_MODULES = {
    "os", "sys", "subprocess", "socket", "pathlib", "shutil", "ctypes",
    "importlib", "inspect", "builtins", "multiprocessing", "threading"
}
SAFE_BUILTINS = {
    "abs": abs,
    "all": all,
    "any": any,
    "bool": bool,
    "dict": dict,
    "enumerate": enumerate,
    "float": float,
    "int": int,
    "len": len,
    "list": list,
    "max": max,
    "min": min,
    "range": range,
    "round": round,
    "set": set,
    "sorted": sorted,
    "str": str,
    "sum": sum,
    "tuple": tuple,
    "zip": zip,
}


def fail(code, message, node=None):
    return {
        "success": False,
        "result": None,
        "code": code,
        "message": message,
        "line": getattr(node, "lineno", None),
        "column": getattr(node, "col_offset", None) + 1 if node is not None else None,
    }


def validate(tree, allowed_imports):
    for node in ast.walk(tree):
        if isinstance(node, ast.Name) and node.id in DENIED_NAMES:
            return fail(
                "scripting.python.capability_denied",
                "The source requests a denied Python capability.",
                node,
            )
        if isinstance(node, ast.Attribute) and node.attr.startswith("__"):
            return fail(
                "scripting.python.dunder_denied",
                "Dunder traversal is not allowed.",
                node,
            )
        if isinstance(node, (ast.Import, ast.ImportFrom)):
            modules = (
                [alias.name.split(".")[0] for alias in node.names]
                if isinstance(node, ast.Import)
                else [(node.module or "").split(".")[0]]
            )
            if any(
                module in DENIED_MODULES or module not in allowed_imports
                for module in modules
            ):
                return fail(
                    "scripting.python.import_denied",
                    "The requested Python import is not allowlisted.",
                    node,
                )
    return None


def restricted_importer(allowed_imports):
    def import_module(name, globals_map=None, locals_map=None, fromlist=(), level=0):
        root = name.split(".")[0]
        if level != 0 or root not in allowed_imports or root in DENIED_MODULES:
            raise ImportError("The requested Python import is not allowlisted.")
        return __import__(name, globals_map, locals_map, fromlist, level)

    return import_module


def execute(request, tree, allowed_imports):
    builtins_map = dict(SAFE_BUILTINS)
    builtins_map["__import__"] = restricted_importer(allowed_imports)
    globals_map = {
        "__builtins__": builtins_map,
        "bindings": request.get("bindings", {}),
    }

    result = None
    body = list(tree.body)
    if body and isinstance(body[-1], ast.Expr):
        expression = ast.Expression(body.pop().value)
        if body:
            module = ast.Module(body=body, type_ignores=[])
            ast.fix_missing_locations(module)
            exec(compile(module, "<pocok>", "exec"), globals_map, globals_map)
        ast.fix_missing_locations(expression)
        result = eval(compile(expression, "<pocok>", "eval"), globals_map, globals_map)
    else:
        exec(compile(tree, "<pocok>", "exec"), globals_map, globals_map)

    if request.get("expectResult") and result is None:
        return fail(
            "scripting.result.missing",
            "The script was expected to return a value.",
        )

    return {
        "success": True,
        "result": result,
        "code": None,
        "message": None,
        "line": None,
        "column": None,
    }


def main():
    try:
        request = json.load(sys.stdin)
        if request.get("protocolVersion") != 1:
            print(json.dumps(fail("scripting.python.protocol_version", "The Python worker protocol version is unsupported."), separators=(",", ":")))
            return
        source = request.get("source", "")
        tree = ast.parse(source, mode="exec")
        allowed_imports = set(request.get("allowedImports", []))
        denied = validate(tree, allowed_imports)
        if denied:
            print(json.dumps(denied, separators=(",", ":")))
            return

        operation = request.get("operation")
        if operation == "validate":
            response = {
                "success": True,
                "result": None,
                "code": None,
                "message": None,
                "line": None,
                "column": None,
            }
        elif operation == "execute":
            response = execute(request, tree, allowed_imports)
        else:
            response = fail("scripting.python.protocol", "Unknown worker operation.")

        print(json.dumps(response, separators=(",", ":")))
    except SyntaxError as error:
        print(json.dumps({
            "success": False,
            "result": None,
            "code": "scripting.python.syntax",
            "message": "Python syntax is invalid.",
            "line": error.lineno,
            "column": error.offset,
        }, separators=(",", ":")))
    except Exception:
        print(json.dumps(
            fail("scripting.python.execution", "Python execution failed safely."),
            separators=(",", ":"),
        ))


if __name__ == "__main__":
    main()
