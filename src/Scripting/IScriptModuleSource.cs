// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Scripting;

/// <summary>Loads neutral script modules without imposing persistence or DI.</summary>
public interface IScriptModuleSource
{
    /// <summary>Finds one module or returns null when it is not available.</summary>
    ValueTask<ScriptModule?> FindAsync(ScriptReference reference, CancellationToken cancellationToken = default);
}
