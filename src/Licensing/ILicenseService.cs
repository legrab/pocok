// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Licensing;

/// <summary>Loads, validates, and enforces the current application license.</summary>
public interface ILicenseService
{
    /// <summary>Gets the most recent loading or validation result.</summary>
    public LicenseValidationResult Current { get; }

    /// <summary>Reloads the configured license source and validates it.</summary>
    /// <param name="requiredModule">An optional module that must be licensed.</param>
    /// <param name="cancellationToken">Cancels license and public-key file reads.</param>
    /// <returns>The validation result.</returns>
    public ValueTask<LicenseValidationResult> RefreshAsync(
        string? requiredModule = null,
        CancellationToken cancellationToken = default);

    /// <summary>Validates the already loaded license against current runtime facts.</summary>
    /// <param name="requiredModule">An optional module that must be licensed.</param>
    /// <returns>The validation result. Before the first refresh, the result is <see cref="LicenseValidationCode.Missing" />.</returns>
    public LicenseValidationResult Validate(string? requiredModule = null);

    /// <summary>Determines whether a moduleIdentifier is currently licensed in the already loaded license.</summary>
    /// <param name="moduleIdentifier">The moduleIdentifier identifier.</param>
    /// <returns>True when the loaded license and moduleIdentifier are valid.</returns>
    public bool HasModule(string moduleIdentifier);

    /// <summary>Throws when a moduleIdentifier is not currently licensed in the already loaded license.</summary>
    /// <param name="moduleIdentifier">The moduleIdentifier identifier.</param>
    /// <exception cref="LicenseException">The license has not been loaded or the moduleIdentifier is invalid.</exception>
    public void Demand(string moduleIdentifier);
}
