// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Modularity.Loading;

/// <summary>Defines the deployment and compatibility metadata for one plugin directory.</summary>
public sealed class ModuleManifest
{
    /// <summary>Gets or sets the manifest schema version.</summary>
    public int ManifestVersion { get; set; } = 1;

    /// <summary>Gets or sets the stable plugin identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the semantic assembly version text.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Gets or sets the entry assembly path relative to the manifest.</summary>
    public string EntryAssembly { get; set; } = string.Empty;

    /// <summary>Gets or sets whether loading failure prevents all module registration.</summary>
    public bool Required { get; set; }

    /// <summary>Gets or sets a module-specific configuration section.</summary>
    public string? ConfigurationSection { get; set; }

    /// <summary>Gets or sets supported OS names: windows, linux, or osx.</summary>
    public IList<string> SupportedOperatingSystems { get; set; } = new List<string>();

    /// <summary>Gets or sets supported process architectures such as x64 or arm64.</summary>
    public IList<string> SupportedArchitectures { get; set; } = new List<string>();

    /// <summary>Gets or sets additional assembly simple names shared with the default context.</summary>
    public IList<string> SharedAssemblies { get; set; } = new List<string>();

    /// <summary>Gets or sets non-secret module metadata.</summary>
    public IDictionary<string, string> Metadata { get; set; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
}
