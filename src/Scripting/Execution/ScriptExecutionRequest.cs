// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors


namespace Pocok.Scripting.Execution;

/// <summary>Defines one bounded script execution request.</summary>
public sealed record ScriptExecutionRequest
{
    /// <summary>Creates an execution request for an explicit engine.</summary>
    public ScriptExecutionRequest(ScriptEngineId engineId, string identifier, string source)
    {
        if (string.IsNullOrWhiteSpace(engineId.Value))
            throw new ArgumentException("An explicit engine identifier is required.", nameof(engineId));
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentNullException.ThrowIfNull(source);
        EngineId = engineId;
        Identifier = identifier;
        Source = source;
    }

    /// <summary>Gets the selected engine.</summary>
    public ScriptEngineId EngineId { get; }

    /// <summary>Gets the diagnostic request identifier.</summary>
    public string Identifier { get; }

    /// <summary>Gets source submitted to the engine.</summary>
    public string Source { get; }

    /// <summary>Gets whether a non-null result is required.</summary>
    public bool ExpectResult { get; init; }

    /// <summary>Gets explicitly allowed scalar or function bindings.</summary>
    public IReadOnlyList<ScriptBinding> Bindings { get; init; } = [];
}
