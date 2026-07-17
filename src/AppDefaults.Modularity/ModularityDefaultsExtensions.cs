// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Hosting;
using Pocok.Modularity.Loading;

namespace Pocok.AppDefaults.Modularity;

/// <summary>Provides composition-root syntax for modularity defaults.</summary>
public static class ModularityDefaultsExtensions
{
    /// <summary>Loads conventional startup modules and returns the same builder.</summary>
    /// <exception cref="InvalidOperationException">The defaults were already applied to this builder.</exception>
    public static IHostApplicationBuilder AddPocokModularityDefaults(
        this IHostApplicationBuilder builder,
        Action<ModularityDefaultsOptions>? configureDefaults = null,
        Action<ModuleLoadOptions>? configureLoader = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        new ModularityDefaultsConfigurator(configureDefaults, configureLoader).Configure(builder);
        return builder;
    }
}
