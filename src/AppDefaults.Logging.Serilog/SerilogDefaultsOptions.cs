// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.AppDefaults.Logging.Serilog;

/// <summary>Controls the small policy layer around Serilog hosting integration.</summary>
public sealed class SerilogDefaultsOptions
{
    /// <summary>Gets the policy configuration section.</summary>
    public const string DefaultSectionName = "Pocok:Logging:Serilog";

    /// <summary>Gets or sets whether the existing static Serilog logger is preserved.</summary>
    public bool PreserveStaticLogger { get; set; }

    /// <summary>Gets or sets whether Serilog forwards events to Microsoft logging providers.</summary>
    public bool WriteToProviders { get; set; }

    /// <summary>Gets or sets whether <c>LogContext</c> enrichment is added after configuration.</summary>
    public bool EnrichFromLogContext { get; set; } = true;
}
