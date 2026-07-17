// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Scripting.Import;

namespace Pocok.Scripting.Modules;

/// <summary>Deterministic script source for tests, tools, and small hosts.</summary>
public sealed class InMemoryScriptModuleSource : IScriptModuleSource
{
    private readonly IReadOnlyDictionary<ScriptReference, ScriptModule> _modules;

    /// <summary>Creates an immutable in-memory module source.</summary>
    public InMemoryScriptModuleSource(IEnumerable<ScriptModule> modules)
    {
        ArgumentNullException.ThrowIfNull(modules);
        ScriptModule[] materialized = [.. modules];
        if (materialized.Any(static module => module is null))
            throw new ArgumentException("Modules cannot contain null entries.", nameof(modules));

        _modules = materialized.ToDictionary(static module => module.Reference);
    }

    /// <inheritdoc />
    public ValueTask<ScriptModule?> FindAsync(ScriptReference reference, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reference);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(_modules.GetValueOrDefault(reference));
    }
}
