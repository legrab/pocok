// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Scripting.Import;
using ScriptImportParser = Pocok.Scripting.Import.ScriptImportParser;

namespace Pocok.Scripting.Modules;

/// <summary>Resolves transitive imports with deterministic ordering and cycle diagnostics.</summary>
public sealed class ScriptModuleResolver
{
    private readonly IScriptModuleSource _source;

    /// <summary>Creates a resolver over an explicit module source.</summary>
    public ScriptModuleResolver(IScriptModuleSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
    }

    /// <summary>Resolves a root module and all reachable imports.</summary>
    public async ValueTask<ScriptImportResolution> ResolveAsync(
        ScriptReference root,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(root);
        var visited = new HashSet<ScriptReference>();
        var active = new HashSet<ScriptReference>();
        var scripts = new List<ImportedScriptContent>();
        var diagnostics = new List<ScriptImportDiagnostic>();
        await CollectAsync(root, visited, active, scripts, diagnostics, cancellationToken);
        return new ScriptImportResolution(scripts, diagnostics);
    }

    private async ValueTask CollectAsync(
        ScriptReference reference,
        HashSet<ScriptReference> visited,
        HashSet<ScriptReference> active,
        List<ImportedScriptContent> scripts,
        List<ScriptImportDiagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!visited.Add(reference))
        {
            if (active.Contains(reference))
                diagnostics.Add(new ScriptImportDiagnostic(reference, "scripting.import.cycle",
                    $"Import cycle detected at {reference.Name} from {reference.Module}."));
            return;
        }

        ScriptModule? module = await _source.FindAsync(reference, cancellationToken);
        if (module is null)
        {
            diagnostics.Add(new ScriptImportDiagnostic(reference, "scripting.import.missing",
                $"Script {reference.Name} from {reference.Module} was not found."));
            return;
        }

        active.Add(reference);
        foreach (ScriptReference imported in ScriptImportParser.FindImports(module.Content))
            await CollectAsync(imported, visited, active, scripts, diagnostics, cancellationToken);

        active.Remove(reference);
        scripts.Add(new ImportedScriptContent(reference, module.Content));
    }
}
