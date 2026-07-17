// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Licensing.Validation;

namespace Pocok.Licensing;

/// <summary>Represents a blocking license requirement failure.</summary>
public sealed class LicenseException : InvalidOperationException
{
    /// <summary>Initializes an exception from a failed validation result.</summary>
    /// <param name="result">The failed result.</param>
    public LicenseException(LicenseValidationResult result)
        : base((result ?? throw new ArgumentNullException(nameof(result))).Message)
    {
        Result = result;
    }

    /// <summary>Gets the validation result that caused the exception.</summary>
    public LicenseValidationResult Result { get; }
}
