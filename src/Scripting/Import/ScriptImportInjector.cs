// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Scripting.Modules;

namespace Pocok.Scripting.Import;

/// <summary>Expands neutral import directives into one executable source document.</summary>
public sealed class ScriptImportInjector
{
    private readonly ScriptModuleResolver _resolver;

    /// <summary>Creates an injector over an explicit module resolver.</summary>
    public ScriptImportInjector(ScriptModuleResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolver = resolver;
    }

    /// <summary>Expands all resolvable imports while retaining graph diagnostics.</summary>
    public async ValueTask<InjectedScript> InjectAsync(string script, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(script);
        IReadOnlyList<ScriptReference> imports = ScriptImportParser.FindImports(script);
        if (imports.Count == 0) return new InjectedScript(script, [], []);

        var resolved = new List<ImportedScriptContent>();
        var diagnostics = new List<ScriptImportDiagnostic>();
        var seen = new HashSet<ScriptReference>();
        foreach (ScriptReference import in imports)
        {
            ScriptImportResolution resolution = await _resolver.ResolveAsync(import, cancellationToken);
            diagnostics.AddRange(resolution.Diagnostics);
            foreach (ImportedScriptContent content in resolution.Scripts)
                if (seen.Add(content.Reference))
                    resolved.Add(content);
        }

        var sections = new List<string>();
        foreach (ImportedScriptContent content in resolved)
        {
            sections.Add(ScriptImportParser.ImportedComment(content.Reference, 0));
            sections.Add(ScriptImportParser.RemoveImports(content.Content).Trim());
        }

        sections.Add(ScriptImportParser.RemoveImports(script).Trim());
        return new InjectedScript(
            string.Join(Environment.NewLine + Environment.NewLine,
                sections.Where(static section => section.Length > 0)),
            imports,
            diagnostics);
    }
}
