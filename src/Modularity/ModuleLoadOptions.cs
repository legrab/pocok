// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Modularity;

/// <summary>Controls trusted startup plugin discovery.</summary>
public sealed class ModuleLoadOptions
{
    /// <summary>Gets plugin roots searched for manifests.</summary>
    public IList<string> Directories { get; } = new List<string>();

    /// <summary>Gets assembly simple names resolved from the default load context.</summary>
    public ISet<string> SharedAssemblyNames { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets or sets the manifest filename.</summary>
    public string ManifestFileName { get; set; } = "pocok.module.json";

    /// <summary>Gets or sets whether plugin roots are searched recursively.</summary>
    public bool SearchRecursively { get; set; } = true;

    /// <summary>Gets or sets whether absent plugin roots are ignored with a diagnostic.</summary>
    public bool IgnoreMissingDirectories { get; set; } = true;

    /// <summary>Gets or sets whether optional plugin failures also stop startup.</summary>
    public bool ThrowOnOptionalFailure { get; set; }

    /// <summary>Adds one plugin root.</summary>
    public ModuleLoadOptions AddDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Directories.Add(path);
        return this;
    }

    /// <summary>Shares the assembly containing <typeparamref name="T" /> with plugins.</summary>
    public ModuleLoadOptions ShareAssemblyContaining<T>()
    {
        var name = typeof(T).Assembly.GetName().Name;
        if (!string.IsNullOrWhiteSpace(name)) SharedAssemblyNames.Add(name);

        return this;
    }

    /// <summary>Shares an assembly by simple name with plugins.</summary>
    public ModuleLoadOptions ShareAssembly(string assemblyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyName);
        SharedAssemblyNames.Add(assemblyName);
        return this;
    }
}
