// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Scripting.Import;

/// <summary>One resolved script body in dependency-first depth-first order.</summary>
public sealed record ImportedScriptContent(ScriptReference Reference, string Content);

/// <summary>Describes an import graph diagnostic.</summary>
public sealed record ScriptImportDiagnostic(ScriptReference Reference, string Code, string Message);

/// <summary>Contains resolved script bodies and non-throwing graph diagnostics.</summary>
public sealed record ScriptImportResolution(
    IReadOnlyList<ImportedScriptContent> Scripts,
    IReadOnlyList<ScriptImportDiagnostic> Diagnostics);

/// <summary>Contains source after import directives have been expanded.</summary>
public sealed record InjectedScript(
    string Content,
    IReadOnlyList<ScriptReference> Imports,
    IReadOnlyList<ScriptImportDiagnostic> Diagnostics);
