// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Scripting;

/// <summary>Defines one isolated JavaScript execution request.</summary>
public sealed record ScriptExecutionRequest
{
    /// <summary>Creates an execution request.</summary>
    public ScriptExecutionRequest(string identifier, string script)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentNullException.ThrowIfNull(script);
        Identifier = identifier;
        Script = script;
    }

    /// <summary>Gets the diagnostic request identifier.</summary>
    public string Identifier { get; }

    /// <summary>Gets the JavaScript source.</summary>
    public string Script { get; }

    /// <summary>Gets or sets whether a non-null result is required.</summary>
    public bool ExpectResult { get; init; }

    /// <summary>Gets or sets the explicitly allowed bindings.</summary>
    public IReadOnlyList<ScriptBinding> Bindings { get; init; } = [];
}
