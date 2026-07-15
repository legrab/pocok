// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Pocok.AppDefaults.Logging;

/// <summary>Applies one configuration-driven Microsoft.Extensions.Logging baseline.</summary>
public sealed class LoggingDefaultsConfigurator : IApplicationConfigurator
{
    private readonly Action<LoggingDefaultsOptions>? _configure;
    private readonly string _sectionName;

    /// <summary>Initializes a configurator for the default <c>Pocok:Logging</c> section.</summary>
    public LoggingDefaultsConfigurator(Action<LoggingDefaultsOptions>? configure = null)
        : this(LoggingDefaultsOptions.DefaultSectionName, configure)
    {
    }

    /// <summary>Initializes a configurator for an explicit configuration section.</summary>
    public LoggingDefaultsConfigurator(
        string sectionName,
        Action<LoggingDefaultsOptions>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);
        _sectionName = sectionName;
        _configure = configure;
    }

    /// <inheritdoc />
    public void Configure(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(LoggingDefaultsMarker))) return;

        LoggingDefaultsOptions options = builder.Configuration.GetSection(_sectionName).Get<LoggingDefaultsOptions>() ??
                                         new LoggingDefaultsOptions();
        _configure?.Invoke(options);
        Validate(options);

        builder.Services.AddSingleton<LoggingDefaultsMarker>();
        builder.Services.AddSingleton(options);

        if (options.ClearProviders) builder.Logging.ClearProviders();

        builder.Logging.Configure(logging => logging.ActivityTrackingOptions = options.ActivityTrackingOptions);

        if (options.MinimumLevel is { } minimumLevel) builder.Logging.SetMinimumLevel(minimumLevel);

        foreach ((var category, LogLevel level) in options.CategoryMinimumLevels.OrderBy(pair => pair.Key,
                     StringComparer.Ordinal)) builder.Logging.AddFilter(category, level);

        if (options.AddSimpleConsole)
            builder.Logging.AddSimpleConsole(console =>
            {
                console.IncludeScopes = options.IncludeScopes;
                console.SingleLine = options.SingleLine;
                console.UseUtcTimestamp = options.UseUtcTimestamp;
                console.TimestampFormat = options.TimestampFormat;
            });
    }

    private static void Validate(LoggingDefaultsOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.TimestampFormat);
        foreach (var category in options.CategoryMinimumLevels.Keys)
            ArgumentException.ThrowIfNullOrWhiteSpace(category);
    }

    private sealed class LoggingDefaultsMarker;
}
