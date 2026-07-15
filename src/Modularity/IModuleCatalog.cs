// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Modularity;

/// <summary>Exposes immutable startup module discovery results.</summary>
public interface IModuleCatalog
{
    /// <summary>Gets discovered plugin descriptors in deterministic manifest-path order.</summary>
    public IReadOnlyList<ModuleDescriptor> Modules { get; }

    /// <summary>Gets diagnostics not associated with one valid manifest.</summary>
    public IReadOnlyList<ModuleDiagnostic> Diagnostics { get; }
}
