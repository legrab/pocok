// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Licensing.Documents;

namespace Pocok.Licensing.Validation;

internal static class LicenseClaimsValidator
{
    internal const int MaximumPayloadBytes = 256 * 1024;
    internal const int MaximumLicenseTextCharacters = 1024 * 1024;

    internal static string? FindError(LicenseDocument license)
    {
        if (!IsIdentifier(license.LicenseId, 256))
            return "licenseId must contain 1 to 256 non-control characters without surrounding whitespace.";
        if (license.Customer is { } customer && (customer.Length > 512 || customer.Any(char.IsControl)))
            return "customer must not exceed 512 characters or contain control characters.";
        if (license.IssuedAtUtc == default)
            return "issuedAtUtc is required.";
        if (license.ValidFromUtc is { } from && license.ValidUntilUtc is { } until && from >= until)
            return "validFromUtc must be earlier than validUntilUtc.";
        if (license.MaximumProcessRuntime is { } runtime && runtime <= TimeSpan.Zero)
            return "maximumProcessRuntime must be positive.";
        if (license.Modules is null || license.MachineFingerprints is null || license.Metadata is null)
            return "modules, machineFingerprints, and metadata cannot be null.";
        if (license.Modules.Count > 256)
            return "A license cannot contain more than 256 modules.";
        if (license.MachineFingerprints.Count > 256)
            return "A license cannot contain more than 256 machine fingerprints.";
        if (license.Metadata.Count > 128)
            return "A license cannot contain more than 128 metadata entries.";

        if (license.Modules.Any(module => !IsIdentifier(module, 256)))
            return "Module identifiers must contain 1 to 256 non-control characters without surrounding whitespace.";
        if (license.Modules.Distinct(StringComparer.OrdinalIgnoreCase).Count() != license.Modules.Count)
            return "Module identifiers must be unique ignoring case.";
        if (license.MachineFingerprints.Any(fingerprint => fingerprint is null || !IsLowerOrUpperHex(fingerprint, 64)))
            return "Machine fingerprints must be 64-character SHA-256 hexadecimal values.";
        if (license.MachineFingerprints.Distinct(StringComparer.OrdinalIgnoreCase).Count() !=
            license.MachineFingerprints.Count)
            return "Machine fingerprints must be unique ignoring case.";
        if (license.PresharedKeyHash is not null && !IsLowerOrUpperHex(license.PresharedKeyHash, 64))
            return "presharedKeyHash must be a 64-character SHA-256 hexadecimal value.";
        if (license.Metadata.Any(pair =>
                !IsIdentifier(pair.Key, 128) || pair.Value is null || pair.Value.Length > 2048 ||
                pair.Value.Any(char.IsControl)))
            return
                "Metadata keys must contain 1 to 128 non-control characters; values must not exceed 2048 characters or contain control characters.";

        return null;
    }

    internal static bool IsIdentifier(string? value, int maximumLength)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Length <= maximumLength &&
               value == value.Trim() &&
               !value.Any(char.IsControl);
    }

    private static bool IsLowerOrUpperHex(string value, int expectedLength)
    {
        if (value.Length != expectedLength) return false;
        foreach (var character in value)
            if (!char.IsAsciiHexDigit(character))
                return false;
        return true;
    }
}
