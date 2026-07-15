// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Hosting;
using Serilog;

namespace Pocok.AppDefaults.Logging.Serilog;

/// <summary>Provides composition-root syntax for Serilog defaults.</summary>
public static class SerilogDefaultsExtensions
{
    /// <summary>Applies Serilog hosting integration once and returns the same builder.</summary>
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
