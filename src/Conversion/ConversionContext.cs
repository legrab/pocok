// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;

namespace Pocok.Conversion;

/// <summary>
///     Supplies all policies that can change a conversion result.
/// </summary>
public sealed record ConversionContext
{
    /// <summary>
    ///     Initializes a conversion context.
    /// </summary>
    public ConversionContext(
        CultureInfo culture,
        OverflowPolicy overflow = OverflowPolicy.Fail,
        NullPolicy nulls = NullPolicy.Preserve,
        EnumPolicy enums = EnumPolicy.DefinedValuesAndFlags,
        NumericLossPolicy numericLoss = NumericLossPolicy.Reject,
        NumericBooleanPolicy numericBooleans = NumericBooleanPolicy.Reject,
        TemporalTextPolicy temporalText = TemporalTextPolicy.RoundTrip,
        int maximumDepth = 32,
        int maximumCollectionItems = 10_000)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ValidatePolicy(overflow, nameof(overflow));
        ValidatePolicy(nulls, nameof(nulls));
        ValidatePolicy(enums, nameof(enums));
        ValidatePolicy(numericLoss, nameof(numericLoss));
        ValidatePolicy(numericBooleans, nameof(numericBooleans));
        ValidatePolicy(temporalText, nameof(temporalText));
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumDepth, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumCollectionItems, 1);

        Culture = CultureInfo.ReadOnly((CultureInfo)culture.Clone());
        Overflow = overflow;
        Nulls = nulls;
        Enums = enums;
        NumericLoss = numericLoss;
        NumericBooleans = numericBooleans;
        TemporalText = temporalText;
        MaximumDepth = maximumDepth;
        MaximumCollectionItems = maximumCollectionItems;
    }

    /// <summary>
    ///     Gets the strict, invariant default context.
    /// </summary>
    public static ConversionContext Strict { get; } = new(CultureInfo.InvariantCulture);

    /// <summary>
    ///     Gets the culture used for parsing, formatting, and case-insensitive textual conversion.
    /// </summary>
    public CultureInfo Culture { get; }

    /// <summary>
    ///     Gets the numeric overflow policy.
    /// </summary>
    public OverflowPolicy Overflow { get; }

    /// <summary>
    ///     Gets the null-input policy.
    /// </summary>
    public NullPolicy Nulls { get; }

    /// <summary>
    ///     Gets the enum validation policy.
    /// </summary>
    public EnumPolicy Enums { get; }

    /// <summary>
    ///     Gets the policy for fractional numeric values converted to integral targets.
    /// </summary>
    public NumericLossPolicy NumericLoss { get; }

    /// <summary>
    ///     Gets the policy for conversions between booleans and numeric values.
    /// </summary>
    public NumericBooleanPolicy NumericBooleans { get; }

    /// <summary>
    ///     Gets the policy for parsing temporal text.
    /// </summary>
    public TemporalTextPolicy TemporalText { get; }

    /// <summary>Gets the maximum nested conversion depth.</summary>
    public int MaximumDepth { get; }

    /// <summary>Gets the maximum number of collection items processed by one conversion.</summary>
    public int MaximumCollectionItems { get; }

    private static void ValidatePolicy<TPolicy>(TPolicy policy, string parameterName)
        where TPolicy : struct, Enum
    {
        if (!Enum.IsDefined(policy))
            throw new ArgumentOutOfRangeException(parameterName, policy, "The policy value is not defined.");
    }
}
