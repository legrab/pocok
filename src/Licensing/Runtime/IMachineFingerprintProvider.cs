// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Licensing.Runtime;

/// <summary>Provides a stable privacy-preserving machine identifier.</summary>
public interface IMachineFingerprintProvider
{
    /// <summary>Gets the current machine fingerprint.</summary>
    /// <returns>A lowercase SHA-256 hexadecimal fingerprint.</returns>
    public string GetFingerprint();
}
