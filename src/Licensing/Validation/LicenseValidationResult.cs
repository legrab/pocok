// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Licensing.Documents;

namespace Pocok.Licensing.Validation;

/// <summary>Represents one expected license loading or validation outcome.</summary>
/// <param name="IsValid">Whether validation succeeded.</param>
/// <param name="Code">The stable outcome code.</param>
/// <param name="Message">A human-readable diagnostic without secrets.</param>
/// <param name="License">The verified license payload when available.</param>
/// <param name="Module">The requested module when applicable.</param>
public sealed record LicenseValidationResult(
    bool IsValid,
    LicenseValidationCode Code,
    string Message,
    LicenseDocument? License = null,
    string? Module = null)
{
    internal static LicenseValidationResult Success(LicenseDocument license, string? module = null)
    {
        return new LicenseValidationResult(true, LicenseValidationCode.Valid, "The license is valid.", license, module);
    }

    internal static LicenseValidationResult Failure(
        LicenseValidationCode code,
        string message,
        LicenseDocument? license = null,
        string? module = null)
    {
        return new LicenseValidationResult(false, code, message, license, module);
    }
}
