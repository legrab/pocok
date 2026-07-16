// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Pocok.Licensing;

/// <summary>Registers Pocok licensing services.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers licensing from an options delegate.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The options delegate.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddPocokLicensing(
        this IServiceCollection services,
        Action<LicenseOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        var options = new LicenseOptions();
        configure(options);
        return services.AddPocokLicensing(options);
    }

    /// <summary>Registers licensing from an already resolved options instance.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The resolved options.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddPocokLicensing(
        this IServiceCollection services,
        LicenseOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        LicenseOptionsValidator.Validate(options);
        if (services.Any(descriptor => descriptor.ServiceType == typeof(LicensingRegistrationMarker)))
            throw new InvalidOperationException(
                "Pocok licensing has already been registered in this service collection.");

        services.TryAddSingleton<TimeProvider>(_ => TimeProvider.System);
        services.AddSingleton(Options.Create(options));
        services.TryAddSingleton<ILicenseClock, SystemLicenseClock>();
        services.TryAddSingleton<IMachineFingerprintProvider, DefaultMachineFingerprintProvider>();
        services.TryAddSingleton<ILicenseService, LicenseService>();
        services.AddSingleton<LicensingRegistrationMarker>();
        return services;
    }

    private sealed class LicensingRegistrationMarker;
}
