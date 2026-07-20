// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors


using Pocok.Scripting.Import;

namespace Pocok.Scripting.Modules;

/// <summary>Resolves transitive imports with deterministic ordering and cycle diagnostics.</summary>
public sealed class ScriptModuleResolver
{
    private readonly IScriptModuleSource _source;
    private readonly IScriptImportSyntax _syntax;

    /// <summary>Creates a resolver over an explicit source and syntax.</summary>
    public ScriptModuleResolver(IScriptModuleSource source, IScriptImportSyntax syntax)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _syntax = syntax ?? throw new ArgumentNullException(nameof(syntax));
    }

    /// <summary>Resolves one root and all reachable imports.</summary>
    public async ValueTask<ScriptImportResolution> ResolveAsync(ScriptReference root, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(root);
        if (root.EngineId != _syntax.EngineId)
            return new([], [new(root, "scripting.import.engine_mismatch", "The import syntax does not match the referenced engine.")]);
        var visited = new HashSet<ScriptReference>();
        var active = new HashSet<ScriptReference>();
        var scripts = new List<ImportedScriptContent>();
        var diagnostics = new List<ScriptImportDiagnostic>();
        await CollectAsync(root, visited, active, scripts, diagnostics, cancellationToken).ConfigureAwait(false);
        return new(scripts, diagnostics);
    }

    private async ValueTask CollectAsync(ScriptReference reference, HashSet<ScriptReference> visited,
        HashSet<ScriptReference> active, List<ImportedScriptContent> scripts,
        List<ScriptImportDiagnostic> diagnostics, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!visited.Add(reference))
        {
            if (active.Contains(reference)) diagnostics.Add(new(reference, "scripting.import.cycle",
                $"Import cycle detected at {reference.Name} from {reference.Module}."));
            return;
        }
        ScriptModule? module = await _source.FindAsync(reference, cancellationToken).ConfigureAwait(false);
        if (module is null)
        {
            diagnostics.Add(new(reference, "scripting.import.missing", $"Script {reference.Name} from {reference.Module} was not found."));
            return;
        }
        active.Add(reference);
        foreach (ScriptReference imported in _syntax.FindImports(module.Content))
            await CollectAsync(imported, visited, active, scripts, diagnostics, cancellationToken).ConfigureAwait(false);
        active.Remove(reference);
        scripts.Add(new(reference, module.Content));
    }
}
