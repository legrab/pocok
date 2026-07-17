// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Collections.ObjectModel;

namespace Pocok.Modularity.Catalog;

/// <summary>Stores immutable startup module discovery results.</summary>
public sealed class ModuleCatalog : IModuleCatalog
{
    /// <summary>Initializes a module catalog.</summary>
    public ModuleCatalog(
        IReadOnlyList<ModuleDescriptor> modules,
        IReadOnlyList<ModuleDiagnostic>? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(modules);
        Modules = new ReadOnlyCollection<ModuleDescriptor>(modules.ToArray());
        Diagnostics =
            new ReadOnlyCollection<ModuleDiagnostic>(diagnostics?.ToArray() ?? Array.Empty<ModuleDiagnostic>());
    }

    /// <inheritdoc />
    public IReadOnlyList<ModuleDescriptor> Modules { get; }

    /// <inheritdoc />
    public IReadOnlyList<ModuleDiagnostic> Diagnostics { get; }
}
