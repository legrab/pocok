// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Conversion;

/// <summary>
/// Stable error codes returned by conversion operations.
/// </summary>
public static class ConversionErrorCodes
{
    /// <summary>Null is not accepted by the selected target and policy.</summary>
    public const string NullNotAllowed = "conversion.null-not-allowed";

    /// <summary>The source value has an invalid textual or structural format.</summary>
    public const string InvalidFormat = "conversion.invalid-format";

    /// <summary>The value is outside the target type's finite range.</summary>
    public const string Overflow = "conversion.overflow";

    /// <summary>The conversion would lose information under the selected policy.</summary>
    public const string Lossy = "conversion.lossy";

    /// <summary>The value is not valid for the requested enum policy.</summary>
    public const string InvalidEnum = "conversion.invalid-enum";

    /// <summary>The source and target type combination is not supported.</summary>
    public const string Unsupported = "conversion.unsupported";

    /// <summary>A collection item, key, or value could not be converted.</summary>
    public const string CollectionItem = "conversion.collection-item";
}
