// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Collections.ObjectModel;
using Pocok.Modularity.Contracts;

namespace Pocok.Modularity.Catalog;

/// <summary>Describes one discovered plugin and its final registration outcome.</summary>
public sealed class ModuleDescriptor
{
    /// <summary>Initializes an immutable module descriptor.</summary>
    public ModuleDescriptor(
        ModuleIdentity? identity,
        string manifestPath,
        string? entryAssemblyPath,
        bool required,
        ModuleStatus status,
        IReadOnlyList<ModuleDiagnostic> diagnostics)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);
        ArgumentNullException.ThrowIfNull(diagnostics);
        Identity = identity;
        ManifestPath = Path.GetFullPath(manifestPath);
        EntryAssemblyPath = entryAssemblyPath is null ? null : Path.GetFullPath(entryAssemblyPath);
        Required = required;
        Status = status;
        Diagnostics = new ReadOnlyCollection<ModuleDiagnostic>(diagnostics.ToArray());
    }

    /// <summary>Gets the validated identity, or null when validation failed before identity creation.</summary>
    public ModuleIdentity? Identity { get; }

    /// <summary>Gets the absolute manifest path.</summary>
    public string ManifestPath { get; }

    /// <summary>Gets the absolute entry assembly path when resolved.</summary>
    public string? EntryAssemblyPath { get; }

    /// <summary>Gets whether this plugin was required.</summary>
    public bool Required { get; }

    /// <summary>Gets the final outcome.</summary>
    public ModuleStatus Status { get; }

    /// <summary>Gets diagnostics produced for the plugin.</summary>
    public IReadOnlyList<ModuleDiagnostic> Diagnostics { get; }
}
