// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Licensing.Validation;

/// <summary>Supplies runtime facts used to evaluate signed license claims.</summary>
public sealed record LicenseValidationContext
{
    /// <summary>Gets the current UTC time.</summary>
    public DateTimeOffset UtcNow { get; init; } = TimeProvider.System.GetUtcNow();

    /// <summary>Gets the elapsed runtime of the current application process.</summary>
    public TimeSpan ProcessRuntime { get; init; }

    /// <summary>Gets the current machine fingerprint.</summary>
    public string? MachineFingerprint { get; init; }

    /// <summary>Gets the supplied high-entropy pre-shared key.</summary>
    public string? PresharedKey { get; init; }

    /// <summary>Gets the module that must be available, or null for license-wide validation.</summary>
    public string? RequiredModule { get; init; }
}
