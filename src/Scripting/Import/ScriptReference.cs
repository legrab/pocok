// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors


using Pocok.Scripting.Execution;

namespace Pocok.Scripting.Import;

/// <summary>Identifies a named script inside one engine-specific logical module.</summary>
public sealed record ScriptReference
{
    /// <summary>Creates a reference.</summary>
    public ScriptReference(ScriptEngineId engineId, string name, string module)
    {
        if (string.IsNullOrWhiteSpace(engineId.Value))
            throw new ArgumentException("An explicit engine identifier is required.", nameof(engineId));
        EngineId = engineId;
        Name = Normalize(name, nameof(name));
        Module = Normalize(module, nameof(module));
    }

    /// <summary>Gets the owning engine.</summary>
    public ScriptEngineId EngineId { get; }

    /// <summary>Gets the script name.</summary>
    public string Name { get; }

    /// <summary>Gets the logical module.</summary>
    public string Module { get; }

    private static string Normalize(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Any(static c => !char.IsLetterOrDigit(c) && c != '_'))
            throw new ArgumentException("Names may contain only letters, digits, and underscores.", parameterName);
        return value;
    }
}
