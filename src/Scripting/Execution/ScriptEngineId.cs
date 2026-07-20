// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors


namespace Pocok.Scripting.Execution;

/// <summary>Identifies one registered script engine.</summary>
public readonly record struct ScriptEngineId
{
    /// <summary>Creates a normalized engine identifier.</summary>
    public ScriptEngineId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim().ToLowerInvariant();
        if (value.Any(static c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_'))
            throw new ArgumentException("Engine identifiers may contain only ASCII letters, digits, hyphens, and underscores.", nameof(value));
        Value = value;
    }

    /// <summary>Gets the normalized identifier.</summary>
    public string Value { get; }

    /// <summary>Gets the standard JavaScript engine identifier.</summary>
    public static ScriptEngineId JavaScript => new("javascript");

    /// <summary>Gets the standard C# engine identifier.</summary>
    public static ScriptEngineId CSharp => new("csharp");

    /// <summary>Gets the standard Python engine identifier.</summary>
    public static ScriptEngineId Python => new("python");

    /// <inheritdoc />
    public override string ToString() => Value;
}
