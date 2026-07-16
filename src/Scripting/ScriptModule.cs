// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Scripting;

/// <summary>Stores one neutral script module body.</summary>
public sealed record ScriptModule
{
    /// <summary>Creates a script module.</summary>
    public ScriptModule(string name, string module, string content)
    {
        Reference = new ScriptReference(name, module);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        Content = content;
    }

    /// <summary>Gets the module's reference.</summary>
    public ScriptReference Reference { get; }

    /// <summary>Gets the JavaScript source.</summary>
    public string Content { get; }
}
