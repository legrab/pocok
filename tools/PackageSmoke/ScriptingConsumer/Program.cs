// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Scripting.Execution;

var adapter = new FakeAdapter();
var runner = new ScriptRunner(new ScriptEngineRegistry([adapter]));
ScriptResult<int> result = await runner.ExecuteAsync<int>(
    new ScriptExecutionRequest(adapter.Descriptor.Id, "smoke", "ignored")
    {
        ExpectResult = true
    });
return result.IsSuccess && result.Value == 42 ? 0 : 1;

sealed class FakeAdapter : IScriptEngineAdapter, IScriptValidator
{
    public ScriptEngineDescriptor Descriptor { get; } = new(
        new ScriptEngineId("smoke"),
        "Smoke",
        true,
        new ScriptEngineCapabilities(true, true, false, false, false));

    public IScriptValidator Validator => this;

    public ScriptEngineId EngineId => Descriptor.Id;

    public ValueTask<ScriptValidationResult> ValidateAsync(
        ScriptExecutionRequest request,
        ScriptExecutionOptions options,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ScriptValidationResult.Valid());

    public ValueTask<ScriptResult<object?>> ExecuteAsync(
        ValidatedScript script,
        ScriptExecutionOptions options,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ScriptResult.Success<object?>(42));
}
