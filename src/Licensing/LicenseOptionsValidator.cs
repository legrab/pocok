// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Licensing;

internal static class LicenseOptionsValidator
{
    internal static void Validate(LicenseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.LicenseText))
            ArgumentException.ThrowIfNullOrWhiteSpace(options.LicensePath);
        if (options.RevalidationInterval <= TimeSpan.Zero || options.RevalidationInterval > TimeSpan.FromDays(1))
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "RevalidationInterval must be greater than zero and no longer than one day.");
        if (options.BlockingExitCode is < 1 or > 255)
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "BlockingExitCode must be between 1 and 255 for portable process exit semantics.");
        if (!Enum.IsDefined(options.FailureBehavior))
            throw new ArgumentOutOfRangeException(nameof(options));

        ArgumentNullException.ThrowIfNull(options.TrustedPublicKeys);
        ArgumentNullException.ThrowIfNull(options.TrustedPublicKeyFiles);
        ArgumentNullException.ThrowIfNull(options.RequiredModules);
        ValidateMap(options.TrustedPublicKeys, "trusted public keys");
        ValidateMap(options.TrustedPublicKeyFiles, "trusted public key files");
        if (options.RequiredModules.Any(module => !LicenseClaimsValidator.IsIdentifier(module, 256)))
            throw new ArgumentException(
                "Required module identifiers must contain 1 to 256 non-control characters without surrounding whitespace.",
                nameof(options));
        if (options.RequiredModules.Distinct(StringComparer.OrdinalIgnoreCase).Count() !=
            options.RequiredModules.Length)
            throw new ArgumentException("Required module identifiers must be unique ignoring case.", nameof(options));
    }

    private static void ValidateMap(IReadOnlyDictionary<string, string> values, string description)
    {
        ArgumentNullException.ThrowIfNull(values);
        foreach (var (key, value) in values)
            if (!LicenseClaimsValidator.IsIdentifier(key, 256) || string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(
                    $"Configured {description} must use non-empty identifiers of at most 256 non-control characters without surrounding whitespace and non-empty values.");
    }
}
