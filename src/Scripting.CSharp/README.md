# Pocok.Scripting.CSharp

Trusted-local C# adapter for the engine-neutral `Pocok.Scripting` runner. Validation and compilation occur in a fresh
killable framework-dependent child process. Roslyn remains inside private worker assets and is not exposed as a
transitive compile dependency.

```csharp
var adapter = new CSharpScriptEngineAdapter(new CSharpScriptEngineOptions
{
    DotNetHostPath = dotnetPath,
    AllowedImports = ["MyApplication.Scripting"],
    AllowedReferencePaths = [trustedContractsAssembly]
});
var runner = new ScriptRunner(new ScriptEngineRegistry([adapter]));
```

The .NET host is resolved only from `CSharpScriptEngineOptions.DotNetHostPath`, then `DOTNET_HOST_PATH`. Packaged
consumers receive integrity-checked worker assets through `buildTransitive`. Repository-source and other explicitly
managed deployments may point `POCOK_CSHARP_WORKER_DIRECTORY` at a built worker directory containing `worker.sha256`.

Default imports are limited to `System`, `System.Collections.Generic`, `System.Linq`, `System.Threading`, and
`System.Threading.Tasks`. Request text cannot add references. `#r`, `#load`, unsafe/native code, reflection, dynamic
loading, process, registry, filesystem, network, environment, and arbitrary host-object access are denied by default.
Host-configured additions are intersected with explicit import/reference allowlists.

Validation, process separation, bounded streams, timeout, and kill-tree cancellation are guardrails, not an
operating-system sandbox. Do not enable this adapter for anonymous hostile workloads.
