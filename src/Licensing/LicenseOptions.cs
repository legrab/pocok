// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Licensing;

/// <summary>Controls license loading, runtime validation, and host enforcement.</summary>
public sealed class LicenseOptions
{
    /// <summary>Gets the conventional configuration section.</summary>
    public const string DefaultSectionName = "Pocok:Licensing";

    /// <summary>Gets or sets the license file path used when <see cref="LicenseText" /> is empty.</summary>
    public string LicensePath { get; set; } = "license.pocok";

    /// <summary>Gets or sets inline license content. Inline content takes precedence over <see cref="LicensePath" />.</summary>
    public string? LicenseText { get; set; }

    /// <summary>Gets or sets the secret used only to unwrap an optionally encrypted license envelope.</summary>
    public string? DecryptionSecret { get; set; }

    /// <summary>Gets or sets the high-entropy pre-shared key required by the license.</summary>
    public string? PresharedKey { get; set; }

    /// <summary>Gets trusted signing public keys indexed by license key identifier.</summary>
    public Dictionary<string, string> TrustedPublicKeys { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Gets trusted signing public-key file paths indexed by license key identifier.</summary>
    public Dictionary<string, string> TrustedPublicKeyFiles { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Gets or sets modules that must be present during host enforcement.</summary>
    public string[] RequiredModules { get; set; } = [];

    /// <summary>Gets or sets whether host enforcement warns or blocks when validation fails.</summary>
    public LicenseFailureBehavior FailureBehavior { get; set; } = LicenseFailureBehavior.Block;

    /// <summary>Gets or sets the interval at which a running host reloads and revalidates its license.</summary>
    public TimeSpan RevalidationInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the portable process exit code, from 1 through 255, assigned when blocking enforcement stops a running
    /// host.
    /// </summary>
    public int BlockingExitCode { get; set; } = 78;
}
