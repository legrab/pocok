// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Scripting.Import;

/// <summary>Identifies a named script inside a logical module.</summary>
public sealed record ScriptReference
{
    /// <summary>Creates a script reference.</summary>
    public ScriptReference(string name, string module)
    {
        Name = Normalize(name, nameof(name));
        Module = Normalize(module, nameof(module));
    }

    /// <summary>Gets the script name.</summary>
    public string Name { get; }

    /// <summary>Gets the logical module name.</summary>
    public string Module { get; }

    private static string Normalize(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Any(static character => !char.IsLetterOrDigit(character) && character != '_'))
            throw new ArgumentException("Names may contain only letters, digits, and underscores.", parameterName);

        return value;
    }
}
