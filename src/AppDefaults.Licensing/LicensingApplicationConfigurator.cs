// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pocok.AppDefaults;
using Pocok.Licensing;

namespace Pocok.AppDefaults.Licensing;

/// <summary>Applies configuration-driven startup and periodic license enforcement.</summary>
public sealed class LicensingApplicationConfigurator : IApplicationConfigurator
{
    private readonly Action<LicenseOptions>? _configure;
    private readonly string _sectionName;

    /// <summary>Initializes a configurator for a configuration section.</summary>
    /// <param name="sectionName">The configuration section name.</param>
    /// <param name="configure">An optional final options override.</param>
    public LicensingApplicationConfigurator(
        string sectionName = LicenseOptions.DefaultSectionName,
        Action<LicenseOptions>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);
        _sectionName = sectionName;
        _configure = configure;
    }

    /// <inheritdoc />
    public void Configure(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(LicensingDefaultsMarker)))
            throw new InvalidOperationException("Pocok licensing defaults have already been applied to this builder.");

        LicenseOptions options = builder.Configuration.GetSection(_sectionName).Get<LicenseOptions>() ??
                                 new LicenseOptions();
        _configure?.Invoke(options);
        ResolvePaths(options, builder.Environment.ContentRootPath);

        builder.Services.AddPocokLicensing(options);
        builder.Services.AddHostedService<LicenseEnforcementHostedService>();
        builder.Services.AddSingleton<LicensingDefaultsMarker>();
    }

    private static void ResolvePaths(LicenseOptions options, string contentRoot)
    {
        ArgumentNullException.ThrowIfNull(options.TrustedPublicKeyFiles);
        if (string.IsNullOrWhiteSpace(options.LicenseText))
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(options.LicensePath);
            if (!Path.IsPathFullyQualified(options.LicensePath))
                options.LicensePath = Path.Combine(contentRoot, options.LicensePath);
        }

        foreach (string keyId in options.TrustedPublicKeyFiles.Keys.ToArray())
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(keyId);
            string path = options.TrustedPublicKeyFiles[keyId];
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            if (!Path.IsPathFullyQualified(path))
                options.TrustedPublicKeyFiles[keyId] = Path.Combine(contentRoot, path);
        }
    }

    private sealed class LicensingDefaultsMarker;
}
