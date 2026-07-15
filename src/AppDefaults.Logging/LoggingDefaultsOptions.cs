// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Logging;

namespace Pocok.AppDefaults.Logging;

/// <summary>Controls conservative defaults applied to Microsoft.Extensions.Logging.</summary>
public sealed class LoggingDefaultsOptions
{
    /// <summary>Gets the default configuration section.</summary>
    public const string DefaultSectionName = "Pocok:Logging";

    /// <summary>Gets or sets whether existing providers are removed. The default is false.</summary>
    public bool ClearProviders { get; set; }

    /// <summary>Gets or sets whether the simple console formatter is registered.</summary>
    public bool AddSimpleConsole { get; set; } = true;

    /// <summary>Gets or sets whether scopes are included in simple-console output.</summary>
    public bool IncludeScopes { get; set; } = true;

    /// <summary>Gets or sets whether simple-console output uses one line per event.</summary>
    public bool SingleLine { get; set; } = true;

    /// <summary>Gets or sets whether simple-console timestamps are UTC.</summary>
    public bool UseUtcTimestamp { get; set; } = true;

    /// <summary>Gets or sets the simple-console timestamp format.</summary>
    public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff 'UTC' ";

    /// <summary>Gets or sets an optional global minimum level.</summary>
    public LogLevel? MinimumLevel { get; set; }

    /// <summary>Gets category-specific minimum levels applied in ordinal category order.</summary>
    public IDictionary<string, LogLevel> CategoryMinimumLevels { get; } =
        new Dictionary<string, LogLevel>(StringComparer.Ordinal);

    /// <summary>Gets or sets activity fields attached to logging scopes.</summary>
    public ActivityTrackingOptions ActivityTrackingOptions { get; set; } =
        ActivityTrackingOptions.SpanId |
        ActivityTrackingOptions.TraceId |
        ActivityTrackingOptions.ParentId |
        ActivityTrackingOptions.Baggage |
        ActivityTrackingOptions.Tags;
}
