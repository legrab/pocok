// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Reflection;
using System.Runtime.Loader;

namespace Pocok.Modularity;

internal sealed class PluginLoadContext(string entryAssemblyPath, IEnumerable<string> sharedAssemblies)
    : AssemblyLoadContext($"Pocok.Plugin:{Path.GetFileNameWithoutExtension(entryAssemblyPath)}")
{
    private readonly AssemblyDependencyResolver _resolver = new(entryAssemblyPath);
    private readonly HashSet<string> _sharedAssemblies = new(sharedAssemblies, StringComparer.OrdinalIgnoreCase);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name is { } simpleName && _sharedAssemblies.Contains(simpleName))
        {
            Assembly? loaded = Default.Assemblies.FirstOrDefault(candidate =>
                AssemblyName.ReferenceMatchesDefinition(candidate.GetName(), assemblyName));
            return loaded ?? Default.LoadFromAssemblyName(assemblyName);
        }

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is null ? null : LoadFromAssemblyPath(path);
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path is null ? nint.Zero : LoadUnmanagedDllFromPath(path);
    }
}
