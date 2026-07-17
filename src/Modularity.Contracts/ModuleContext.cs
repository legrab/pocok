// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Collections.ObjectModel;
using Microsoft.Extensions.Configuration;

namespace Pocok.Modularity.Contracts;

/// <summary>Supplies validated host information to a module during service registration.</summary>
public sealed class ModuleContext
{
    /// <summary>Initializes a module registration context.</summary>
    public ModuleContext(
        ModuleIdentity identity,
        string baseDirectory,
        IConfiguration configuration,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);
        ArgumentNullException.ThrowIfNull(configuration);

        Identity = identity;
        BaseDirectory = Path.GetFullPath(baseDirectory);
        Configuration = configuration;
        Dictionary<string, string> metadataCopy = metadata is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(metadata, StringComparer.Ordinal);
        Metadata = new ReadOnlyDictionary<string, string>(metadataCopy);
    }

    /// <summary>Gets the module identity.</summary>
    public ModuleIdentity Identity { get; }

    /// <summary>Gets the absolute plugin base directory.</summary>
    public string BaseDirectory { get; }

    /// <summary>Gets the module-specific configuration view supplied by the loader.</summary>
    public IConfiguration Configuration { get; }

    /// <summary>Gets immutable manifest metadata.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
