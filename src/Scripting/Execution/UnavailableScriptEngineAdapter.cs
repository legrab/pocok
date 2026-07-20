// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors


namespace Pocok.Scripting.Execution;

/// <summary>Represents a configured engine that is intentionally unavailable in the current deployment.</summary>
public sealed class UnavailableScriptEngineAdapter : IScriptEngineAdapter, IScriptValidator
{
    /// <summary>Creates an unavailable engine descriptor.</summary>
    public UnavailableScriptEngineAdapter(ScriptEngineId id, string language, string code, string message,
        ScriptEngineCapabilities? capabilities = null)
    {
        Descriptor = new(id, language, false, capabilities ?? new(false, false, false, false, false), code, message);
    }
    /// <inheritdoc />
    public ScriptEngineDescriptor Descriptor { get; }
    /// <inheritdoc />
    public IScriptValidator Validator => this;
    /// <inheritdoc />
    public ScriptEngineId EngineId => Descriptor.Id;
    /// <inheritdoc />
    public ValueTask<ScriptValidationResult> ValidateAsync(ScriptExecutionRequest request, ScriptExecutionOptions options,
        CancellationToken cancellationToken = default) => ValueTask.FromResult(ScriptValidationResult.From([
            new(Descriptor.UnavailableCode ?? "scripting.engine.unavailable", Descriptor.UnavailableMessage ?? "The engine is unavailable.")
        ]));
    /// <inheritdoc />
    public ValueTask<ScriptResult<object?>> ExecuteAsync(ValidatedScript script, ScriptExecutionOptions options,
        CancellationToken cancellationToken = default) => ValueTask.FromResult(ScriptResult.Failed<object?>(new(
            Descriptor.UnavailableCode ?? "scripting.engine.unavailable", Descriptor.UnavailableMessage ?? "The engine is unavailable.")));
}
