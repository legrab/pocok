// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Licensing.Validation;

/// <summary>Identifies the outcome of license loading or validation.</summary>
public enum LicenseValidationCode
{
    /// <summary>The license and requested entitlement are valid.</summary>
    Valid,

    /// <summary>No license content was available.</summary>
    Missing,

    /// <summary>The license content or signed claims are malformed.</summary>
    Malformed,

    /// <summary>The envelope format or algorithm is unsupported.</summary>
    UnsupportedFormat,

    /// <summary>The license references an unknown signing key.</summary>
    UntrustedSigningKey,

    /// <summary>The digital signature is invalid.</summary>
    InvalidSignature,

    /// <summary>The license validity window has not started.</summary>
    NotYetValid,

    /// <summary>The license validity window has ended.</summary>
    Expired,

    /// <summary>The licensed maximum process runtime was exceeded.</summary>
    RuntimeExceeded,

    /// <summary>The current machine is not covered.</summary>
    MachineMismatch,

    /// <summary>An encrypted license was supplied without its decryption secret.</summary>
    DecryptionSecretRequired,

    /// <summary>A required pre-shared key was not supplied.</summary>
    PresharedKeyRequired,

    /// <summary>The supplied pre-shared key is invalid.</summary>
    PresharedKeyMismatch,

    /// <summary>The requested module is not licensed.</summary>
    ModuleMissing
}
