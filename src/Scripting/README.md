# Pocok.Scripting

Engine-neutral contracts for bounded script execution. Hosts explicitly register `IScriptEngineAdapter` implementations
in a `ScriptEngineRegistry`, then construct `ScriptRunner` from that registry. The core package contains no language
runtime.

```csharp
var registry = new ScriptEngineRegistry(adapters);
var runner = new ScriptRunner(registry);
var request = new ScriptExecutionRequest(ScriptEngineId.JavaScript, "job-42", source)
{
    ExpectResult = true
};
ScriptResult<object?> result = await runner.ExecuteAsync(request, new ScriptExecutionOptions
{
    Timeout = TimeSpan.FromSeconds(1),
    MaxOutputBytes = 32 * 1024
}, cancellationToken);
```

## Boundaries

Validation always runs before adapter execution. Validators are fail-closed guardrails, not an operating-system sandbox.
Common source, output, timeout, and cancellation rules are enforced by the runner. Engine-specific limits are requested
only when the descriptor says the adapter can enforce them. Safe failures never include source, raw stderr, environment
values, absolute paths, or worker exceptions.

Imports remain explicit and engine-aware through `IScriptImportSyntax`, `ScriptModuleResolver`, and
`ScriptImportInjector`. Persistence, file watching, remote acquisition, and multi-file workspaces are outside this alpha
contract.
