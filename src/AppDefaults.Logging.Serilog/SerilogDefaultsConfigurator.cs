// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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
        if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(SerilogDefaultsMarker)))
            throw new InvalidOperationException("Pocok Serilog defaults have already been applied to this builder.");

        SerilogDefaultsOptions options = builder.Configuration
            .GetSection(SerilogDefaultsOptions.DefaultSectionName)
            .Get<SerilogDefaultsOptions>() ?? new SerilogDefaultsOptions();
        _configureOptions?.Invoke(options);

        builder.Services.AddSingleton<IOptions<SerilogDefaultsOptions>>(Options.Create(options));
        builder.Services.AddSerilog(
            (_, loggerConfiguration) =>
            {
                loggerConfiguration.ReadFrom.Configuration(builder.Configuration);
                if (options.EnrichFromLogContext) loggerConfiguration.Enrich.FromLogContext();

                _configureLogger?.Invoke(loggerConfiguration);
            },
            options.PreserveStaticLogger,
            options.WriteToProviders);
        builder.Services.AddSingleton<SerilogDefaultsMarker>();
    }

    private sealed class SerilogDefaultsMarker;
}
