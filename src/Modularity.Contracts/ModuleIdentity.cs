// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Modularity;

/// <summary>Identifies one discovered module independently from its implementation assembly.</summary>
public sealed record ModuleIdentity
{
    /// <summary>Initializes a validated module identity.</summary>
    public ModuleIdentity(string id, Version version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(version);
        Id = id;
        Version = version;
    }

    /// <summary>Gets the stable module identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the module version.</summary>
    public Version Version { get; }
}
