// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Security.Cryptography;

namespace Pocok.Licensing;

/// <summary>Evaluates verified license claims against current runtime facts.</summary>
public static class LicenseValidator
{
    /// <summary>Validates one verified license against the supplied context.</summary>
    /// <param name="license">The verified license claims.</param>
    /// <param name="context">Current runtime facts.</param>
    /// <returns>A stable validation result.</returns>
    public static LicenseValidationResult Validate(LicenseDocument license, LicenseValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(license);
        ArgumentNullException.ThrowIfNull(context);

        if (LicenseClaimsValidator.FindError(license) is { } claimError)
            return LicenseValidationResult.Failure(
                LicenseValidationCode.Malformed,
                $"The signed license claims are invalid: {claimError}",
                license);
        if (context.ProcessRuntime < TimeSpan.Zero)
            return LicenseValidationResult.Failure(
                LicenseValidationCode.Malformed,
                "Process runtime cannot be negative.",
                license);
        if (license.ValidFromUtc is { } from && context.UtcNow < from)
            return LicenseValidationResult.Failure(
                LicenseValidationCode.NotYetValid,
                $"License '{license.LicenseId}' is not valid before {from:O}.",
                license);
        if (license.ValidUntilUtc is { } until && context.UtcNow >= until)
            return LicenseValidationResult.Failure(
                LicenseValidationCode.Expired,
                $"License '{license.LicenseId}' expired at {until:O}.",
                license);
        if (license.MaximumProcessRuntime is { } runtime && context.ProcessRuntime > runtime)
            return LicenseValidationResult.Failure(
                LicenseValidationCode.RuntimeExceeded,
                $"License '{license.LicenseId}' allows a maximum process runtime of {runtime}.",
                license);
        if (license.MachineFingerprints.Count > 0 &&
            !license.MachineFingerprints.Contains(context.MachineFingerprint ?? string.Empty,
                StringComparer.OrdinalIgnoreCase))
            return LicenseValidationResult.Failure(
                LicenseValidationCode.MachineMismatch,
                "The license does not cover this machine.",
                license);

        if (license.PresharedKeyHash is not null)
        {
            if (string.IsNullOrWhiteSpace(context.PresharedKey))
                return LicenseValidationResult.Failure(
                    LicenseValidationCode.PresharedKeyRequired,
                    "A pre-shared key is required.",
                    license);

            var expected = Convert.FromHexString(license.PresharedKeyHash);
            var actual = Convert.FromHexString(
                LicenseCryptography.CreatePresharedKeyHash(context.PresharedKey, license.LicenseId));
            try
            {
                if (!CryptographicOperations.FixedTimeEquals(expected, actual))
                    return LicenseValidationResult.Failure(
                        LicenseValidationCode.PresharedKeyMismatch,
                        "The pre-shared key is invalid.",
                        license);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(expected);
                CryptographicOperations.ZeroMemory(actual);
            }
        }

        var requiredModule = context.RequiredModule;
        if (requiredModule is not null && !LicenseClaimsValidator.IsIdentifier(requiredModule, 256))
            return LicenseValidationResult.Failure(
                LicenseValidationCode.Malformed,
                "The required module identifier is invalid.",
                license,
                requiredModule);
        if (requiredModule is not null &&
            !license.AllModules &&
            !license.Modules.Contains(requiredModule, StringComparer.OrdinalIgnoreCase))
            return LicenseValidationResult.Failure(
                LicenseValidationCode.ModuleMissing,
                $"Module '{requiredModule}' is not licensed.",
                license,
                requiredModule);

        return LicenseValidationResult.Success(license, requiredModule);
    }
}
