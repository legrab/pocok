// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.AppDefaults.Modularity;

/// <summary>Controls the conventional application policy applied to Pocok.Modularity.</summary>
public sealed class ModularityDefaultsOptions
{
    /// <summary>Gets the policy configuration section.</summary>
    public const string DefaultSectionName = "Pocok:Modularity";

    /// <summary>Gets or sets the plugin directory, relative to content root unless absolute.</summary>
    public string PluginDirectory { get; set; } = "plugins";

    /// <summary>Gets or sets whether manifests are found recursively.</summary>
    public bool SearchRecursively { get; set; } = true;

    /// <summary>Gets or sets whether an absent plugin directory is ignored.</summary>
    public bool IgnoreMissingDirectory { get; set; } = true;

    /// <summary>Gets or sets whether optional plugin failures stop startup.</summary>
    public bool ThrowOnOptionalFailure { get; set; }

    /// <summary>Gets additional assembly simple names shared with plugins.</summary>
    public ISet<string> SharedAssemblyNames { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}
