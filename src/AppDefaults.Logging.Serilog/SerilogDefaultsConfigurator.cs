// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Pocok.AppDefaults.Logging.Serilog;

/// <summary>Configures Serilog from the standard <c>Serilog</c> section and the host service collection.</summary>
/// <remarks>Initializes a Serilog application-default configurator.</remarks>
public sealed class SerilogDefaultsConfigurator(
    Action<SerilogDefaultsOptions>? configureOptions = null,
    Action<LoggerConfiguration>? configureLogger = null) : IApplicationConfigurator
{
    private readonly Action<LoggerConfiguration>? _configureLogger = configureLogger;
    private readonly Action<SerilogDefaultsOptions>? _configureOptions = configureOptions;

    /// <inheritdoc />
    public void Configure(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(SerilogDefaultsMarker))) return;

        SerilogDefaultsOptions options = builder.Configuration
            .GetSection(SerilogDefaultsOptions.DefaultSectionName)
            .Get<SerilogDefaultsOptions>() ?? new SerilogDefaultsOptions();
        _configureOptions?.Invoke(options);

        builder.Services.AddSingleton<SerilogDefaultsMarker>();
        builder.Services.AddSingleton(options);
        builder.Services.AddSerilog(
            (_, loggerConfiguration) =>
            {
                loggerConfiguration.ReadFrom.Configuration(builder.Configuration);
                if (options.EnrichFromLogContext) loggerConfiguration.Enrich.FromLogContext();

                _configureLogger?.Invoke(loggerConfiguration);
            },
            options.PreserveStaticLogger,
            options.WriteToProviders);
    }

    private sealed class SerilogDefaultsMarker;
}
