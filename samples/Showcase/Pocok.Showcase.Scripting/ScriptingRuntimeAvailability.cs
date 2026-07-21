// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Scripting.Execution;

namespace Pocok.Showcase.Scripting;

/// <summary>
/// Adds a runtime availability gate around an engine whose startup probe may disable it.
/// </summary>
public sealed class RuntimeGatedScriptEngineAdapter : IScriptEngineAdapter
{
    private readonly IScriptEngineAdapter _inner;
    private RuntimeFailure? _failure;

    public RuntimeGatedScriptEngineAdapter(IScriptEngineAdapter inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public ScriptEngineDescriptor Descriptor
    {
        get
        {
            RuntimeFailure? failure = Volatile.Read(ref _failure);
            return failure is null
                ? _inner.Descriptor
                : _inner.Descriptor with
                {
                    IsAvailable = false,
                    UnavailableCode = failure.Code,
                    UnavailableMessage = failure.Message
                };
        }
    }

    public IScriptValidator Validator => _inner.Validator;

    public ValueTask<ScriptResult<object?>> ExecuteAsync(
        ValidatedScript script,
        ScriptExecutionOptions options,
        CancellationToken cancellationToken = default)
    {
        return _inner.ExecuteAsync(script, options, cancellationToken);
    }

    public bool Disable(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return Interlocked.CompareExchange(
            ref _failure,
            new RuntimeFailure(code, message),
            null) is null;
    }

    private sealed record RuntimeFailure(string Code, string Message);
}

/// <summary>Tracks runtime-gated engines configured by the Showcase module.</summary>
public sealed class ScriptingRuntimeAvailability
{
    private readonly Dictionary<ScriptEngineId, RuntimeGatedScriptEngineAdapter> _gates;

    public ScriptingRuntimeAvailability(IEnumerable<RuntimeGatedScriptEngineAdapter> gates)
    {
        ArgumentNullException.ThrowIfNull(gates);
        _gates = gates.ToDictionary(static gate => gate.Descriptor.Id);
    }

    public bool TryDisable(ScriptEngineId engineId, string code, string message)
    {
        return _gates.TryGetValue(engineId, out RuntimeGatedScriptEngineAdapter? gate)
            && gate.Disable(code, message);
    }
}
