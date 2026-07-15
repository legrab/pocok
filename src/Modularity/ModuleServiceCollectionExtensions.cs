// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Pocok.Modularity;

/// <summary>Registers trusted startup modules through the standard service collection.</summary>
public static class ModuleServiceCollectionExtensions
{
    /// <summary>Discovers modules, registers their services, and adds the resulting catalog.</summary>
    public static IServiceCollection AddPocokModules(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<ModuleLoadOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configure);

        if (services.Any(descriptor => descriptor.ServiceType == typeof(IModuleCatalog))) return services;

        var options = new ModuleLoadOptions();
        configure(options);
        ModuleCatalog catalog = ModuleLoader.Load(services, configuration, options);
        services.AddSingleton(catalog);
        services.AddSingleton<IModuleCatalog>(catalog);
        return services;
    }
}
