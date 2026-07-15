// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Hosting;

namespace Pocok.AppDefaults;

/// <summary>Composes application configurators in an explicit and deterministic order.</summary>
public static class ApplicationConfiguratorExtensions
{
    /// <summary>Applies configurators in the order supplied and returns the same builder.</summary>
    public static IHostApplicationBuilder ConfigureWith(
        this IHostApplicationBuilder builder,
        params IApplicationConfigurator[] configurators)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configurators);

        foreach (IApplicationConfigurator configurator in configurators)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            configurator.Configure(builder);
        }

        return builder;
    }

    /// <summary>Applies configurators in enumeration order and returns the same builder.</summary>
    public static IHostApplicationBuilder ConfigureWith(
        this IHostApplicationBuilder builder,
        IEnumerable<IApplicationConfigurator> configurators)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configurators);

        foreach (IApplicationConfigurator configurator in configurators)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            configurator.Configure(builder);
        }

        return builder;
    }
}
