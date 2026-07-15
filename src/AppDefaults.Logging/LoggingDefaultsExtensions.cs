// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Hosting;

namespace Pocok.AppDefaults.Logging;

/// <summary>Provides composition-root syntax for the logging defaults.</summary>
public static class LoggingDefaultsExtensions
{
    /// <summary>Applies the logging defaults once and returns the same builder.</summary>
    public static IHostApplicationBuilder AddPocokLoggingDefaults(
        this IHostApplicationBuilder builder,
        Action<LoggingDefaultsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        new LoggingDefaultsConfigurator(configure).Configure(builder);
        return builder;
    }
}
