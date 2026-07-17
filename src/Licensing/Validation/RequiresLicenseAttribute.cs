// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Licensing.Validation;

/// <summary>Declares a module requirement that an application-owned pipeline can enforce.</summary>
/// <remarks>
///     The attribute is metadata only. Call <see cref="LicenseGuard.DemandFor" /> from an interceptor, endpoint
///     filter, or command pipeline.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequiresLicenseAttribute : Attribute
{
    /// <summary>Initializes a module requirement.</summary>
    /// <param name="module">The required module identifier.</param>
    public RequiresLicenseAttribute(string module)
    {
        if (!LicenseClaimsValidator.IsIdentifier(module, 256))
            throw new ArgumentException(
                "The module identifier must contain 1 to 256 non-control characters without surrounding whitespace.",
                nameof(module));
        Module = module;
    }

    /// <summary>Gets the required module identifier.</summary>
    public string Module { get; }
}
