// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Hosting;
using Pocok.Licensing;

namespace Pocok.AppDefaults.Licensing;

/// <summary>Provides composition-root syntax for licensing defaults.</summary>
public static class LicensingDefaultsExtensions
{
    /// <summary>Applies startup and periodic license enforcement and returns the same builder.</summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">An optional options override applied after configuration binding.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="InvalidOperationException">The defaults were already applied to this builder.</exception>
    public static IHostApplicationBuilder AddPocokLicensingDefaults(
        this IHostApplicationBuilder builder,
        Action<LicenseOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        new LicensingApplicationConfigurator(configure: configure).Configure(builder);
        return builder;
    }
}
