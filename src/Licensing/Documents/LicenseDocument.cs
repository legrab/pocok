// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Licensing.Documents;

/// <summary>Describes the signed entitlements and constraints contained in a license.</summary>
public sealed record LicenseDocument
{
    /// <summary>Gets the issuer-defined license identifier.</summary>
    public string LicenseId { get; init; } = string.Empty;

    /// <summary>Gets the optional customer or installation label.</summary>
    public string? Customer { get; init; }

    /// <summary>Gets the UTC time at which the license was issued.</summary>
    public DateTimeOffset IssuedAtUtc { get; init; }

    /// <summary>Gets the optional inclusive UTC start of the validity window.</summary>
    public DateTimeOffset? ValidFromUtc { get; init; }

    /// <summary>Gets the optional exclusive UTC end of the validity window.</summary>
    public DateTimeOffset? ValidUntilUtc { get; init; }

    /// <summary>Gets the optional maximum runtime of one application process.</summary>
    public TimeSpan? MaximumProcessRuntime { get; init; }

    /// <summary>Gets whether every module is licensed.</summary>
    public bool AllModules { get; init; }

    /// <summary>Gets explicitly licensed module identifiers.</summary>
    public IReadOnlyList<string> Modules { get; init; } = [];

    /// <summary>Gets accepted privacy-preserving machine fingerprints.</summary>
    public IReadOnlyList<string> MachineFingerprints { get; init; } = [];

    /// <summary>Gets the optional versioned digest of a high-entropy pre-shared key.</summary>
    public string? PresharedKeyHash { get; init; }

    /// <summary>Gets issuer-defined non-secret metadata.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
}
