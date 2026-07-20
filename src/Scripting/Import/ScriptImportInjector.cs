// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors


using Pocok.Scripting.Modules;

namespace Pocok.Scripting.Import;

/// <summary>Expands engine-specific imports into one source document.</summary>
public sealed class ScriptImportInjector
{
    private readonly ScriptModuleResolver _resolver;
    private readonly IScriptImportSyntax _syntax;

    /// <summary>Creates an injector.</summary>
    public ScriptImportInjector(ScriptModuleResolver resolver, IScriptImportSyntax syntax)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _syntax = syntax ?? throw new ArgumentNullException(nameof(syntax));
    }

    /// <summary>Expands all resolvable imports while retaining graph diagnostics.</summary>
    public async ValueTask<InjectedScript> InjectAsync(string script, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(script);
        IReadOnlyList<ScriptReference> imports = _syntax.FindImports(script);
        if (imports.Count == 0) return new(script, [], []);
        var resolved = new List<ImportedScriptContent>();
        var diagnostics = new List<ScriptImportDiagnostic>();
        var seen = new HashSet<ScriptReference>();
        foreach (ScriptReference import in imports)
        {
            ScriptImportResolution resolution = await _resolver.ResolveAsync(import, cancellationToken).ConfigureAwait(false);
            diagnostics.AddRange(resolution.Diagnostics);
            foreach (ImportedScriptContent content in resolution.Scripts)
                if (seen.Add(content.Reference)) resolved.Add(content);
        }
        var sections = new List<string>();
        foreach (ImportedScriptContent content in resolved)
        {
            sections.Add(_syntax.ImportedComment(content.Reference, 0));
            sections.Add(_syntax.RemoveImports(content.Content).Trim());
        }
        sections.Add(_syntax.RemoveImports(script).Trim());
        return new(string.Join(Environment.NewLine + Environment.NewLine, sections.Where(static value => value.Length > 0)), imports, diagnostics);
    }
}
