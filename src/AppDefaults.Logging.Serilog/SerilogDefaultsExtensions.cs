// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Hosting;
using Serilog;

namespace Pocok.AppDefaults.Logging.Serilog;

/// <summary>Provides composition-root syntax for Serilog defaults.</summary>
public static class SerilogDefaultsExtensions
{
    /// <summary>Applies Serilog hosting integration and returns the same builder.</summary>
    /// <exception cref="InvalidOperationException">The defaults were already applied to this builder.</exception>
    public static IHostApplicationBuilder AddPocokSerilogDefaults(
        this IHostApplicationBuilder builder,
        Action<SerilogDefaultsOptions>? configureOptions = null,
        Action<LoggerConfiguration>? configureLogger = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        new SerilogDefaultsConfigurator(configureOptions, configureLogger).Configure(builder);
        return builder;
    }
}
