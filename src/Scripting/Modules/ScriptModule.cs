// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors


using Pocok.Scripting.Execution;
using Pocok.Scripting.Import;

namespace Pocok.Scripting.Modules;

/// <summary>Stores one engine-specific script module body.</summary>
public sealed record ScriptModule
{
    /// <summary>Creates a module.</summary>
    public ScriptModule(ScriptEngineId engineId, string name, string module, string content)
    {
        Reference = new ScriptReference(engineId, name, module);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        Content = content;
    }
    /// <summary>Gets the reference.</summary>
    public ScriptReference Reference { get; }
    /// <summary>Gets source content.</summary>
    public string Content { get; }
}
