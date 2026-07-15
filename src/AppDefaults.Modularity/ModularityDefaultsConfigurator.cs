// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pocok.Modularity;

namespace Pocok.AppDefaults.Modularity;

/// <summary>Applies a conventional plugin-directory policy to Pocok.Modularity.</summary>
/// <remarks>Initializes a modularity defaults configurator.</remarks>
public sealed class ModularityDefaultsConfigurator(
    Action<ModularityDefaultsOptions>? configureDefaults = null,
    Action<ModuleLoadOptions>? configureLoader = null) : IApplicationConfigurator
{
    private readonly Action<ModularityDefaultsOptions>? _configureDefaults = configureDefaults;
    private readonly Action<ModuleLoadOptions>? _configureLoader = configureLoader;

    /// <inheritdoc />
    public void Configure(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(ModularityDefaultsOptions))) return;

        ModularityDefaultsOptions defaults = builder.Configuration
            .GetSection(ModularityDefaultsOptions.DefaultSectionName)
            .Get<ModularityDefaultsOptions>() ?? new ModularityDefaultsOptions();
        _configureDefaults?.Invoke(defaults);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaults.PluginDirectory);

        var pluginDirectory = Path.IsPathFullyQualified(defaults.PluginDirectory)
            ? defaults.PluginDirectory
            : Path.Combine(builder.Environment.ContentRootPath, defaults.PluginDirectory);

        builder.Services.AddSingleton(defaults);
        builder.Services.AddPocokModules(builder.Configuration, options =>
        {
            options.AddDirectory(pluginDirectory);
            options.SearchRecursively = defaults.SearchRecursively;
            options.IgnoreMissingDirectories = defaults.IgnoreMissingDirectory;
            options.ThrowOnOptionalFailure = defaults.ThrowOnOptionalFailure;
            foreach (var assemblyName in defaults.SharedAssemblyNames.Order(StringComparer.Ordinal))
                options.ShareAssembly(assemblyName);

            _configureLoader?.Invoke(options);
        });
    }
}
