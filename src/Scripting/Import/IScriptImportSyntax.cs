// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors


using Pocok.Scripting.Execution;

namespace Pocok.Scripting.Import;

/// <summary>Parses and removes import directives for one engine syntax.</summary>
public interface IScriptImportSyntax
{
    /// <summary>Gets the engine owning the syntax.</summary>
    public ScriptEngineId EngineId { get; }
    /// <summary>Finds imports in source order.</summary>
    public IReadOnlyList<ScriptReference> FindImports(string? content);
    /// <summary>Removes import directives before execution.</summary>
    public string RemoveImports(string content);
    /// <summary>Formats a safe generated import marker.</summary>
    public string ImportedComment(ScriptReference reference, int depth);
}
