// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Conversion;

/// <summary>
/// Specifies how numeric overflow is handled.
/// </summary>
public enum OverflowPolicy
{
    /// <summary>Return a failed result.</summary>
    Fail,

    /// <summary>Clamp to the nearest finite target boundary.</summary>
    Saturate
}

/// <summary>
/// Specifies how null input is handled.
/// </summary>
public enum NullPolicy
{
    /// <summary>Preserve null when the target permits it and fail otherwise.</summary>
    Preserve,

    /// <summary>Return the target type's default value.</summary>
    UseDefault,

    /// <summary>Reject null even when the target permits it.</summary>
    Reject
}

/// <summary>
/// Specifies which parsed enum values are accepted.
/// </summary>
public enum EnumPolicy
{
    /// <summary>Accept only a value declared as an enum member.</summary>
    DefinedValuesOnly,

    /// <summary>Also accept flags combinations composed only of declared flag bits.</summary>
    DefinedValuesAndFlags
}

/// <summary>
/// Specifies how fractional values are converted to integral targets.
/// </summary>
public enum NumericLossPolicy
{
    /// <summary>Reject a conversion that would discard a fractional component.</summary>
    Reject,

    /// <summary>Round to the nearest integer with midpoint values rounded away from zero.</summary>
    RoundToNearest
}

/// <summary>
/// Specifies whether booleans and numeric values can be interchanged.
/// </summary>
public enum NumericBooleanPolicy
{
    /// <summary>Reject numeric-to-boolean and boolean-to-numeric conversions.</summary>
    Reject,

    /// <summary>Accept only zero and one, mapped to false and true.</summary>
    ZeroOrOne,

    /// <summary>Map zero to false and every other finite value to true.</summary>
    NonZeroIsTrue
}

/// <summary>
/// Specifies which textual temporal formats are accepted.
/// </summary>
public enum TemporalTextPolicy
{
    /// <summary>Accept only round-trip, invariant formats.</summary>
    RoundTrip,

    /// <summary>Use the context culture's standard temporal parser.</summary>
    CultureAware
}
