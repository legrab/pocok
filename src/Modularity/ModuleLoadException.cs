// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Modularity;

/// <summary>Stops startup when a required plugin or configured optional plugin fails.</summary>
public sealed class ModuleLoadException : Exception
{
    /// <summary>Initializes a module loading exception with the partial diagnostic catalog.</summary>
    public ModuleLoadException(string message, ModuleCatalog catalog, Exception? innerException = null)
        : base(message, innerException)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        Catalog = catalog;
    }

    /// <summary>Gets the immutable partial catalog produced before startup was stopped.</summary>
    public ModuleCatalog Catalog { get; }
}
