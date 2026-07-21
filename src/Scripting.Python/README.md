# Pocok.Scripting.Python

Trusted-local CPython 3.14 adapter for the engine-neutral `Pocok.Scripting` runner. It starts a fresh external process
with `-I -S`, a sanitized environment, bounded streams, AST validation, and kill-tree timeout/cancellation. It provides
no Python.NET or CLR bridge.

```csharp
var adapter = new PythonScriptEngineAdapter(new PythonScriptEngineOptions
{
    PythonExecutable = pythonPath,
    AllowedImports = ["math"]
});
var runner = new ScriptRunner(new ScriptEngineRegistry([adapter]));
```

The executable is resolved only from `PythonScriptEngineOptions.PythonExecutable`, then `POCOK_PYTHON_EXECUTABLE`, and
must probe as CPython 3.14.x. Packaged consumers receive the private worker through `buildTransitive`. Repository-source
deployments may set `POCOK_PYTHON_WORKER_PATH` explicitly.

Imports default to none. `eval`, `exec`, `compile`, dynamic import, dunder traversal,
process/environment/filesystem/network access, and non-allowlisted imports are rejected before execution. Only bounded
JSON-compatible values cross the process boundary.

Validation and process isolation are guardrails, not an operating-system sandbox. Do not enable this adapter for
anonymous hostile workloads.
