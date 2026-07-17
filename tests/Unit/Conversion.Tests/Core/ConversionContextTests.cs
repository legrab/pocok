// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;

namespace Pocok.Conversion.Tests.Core;

public sealed class ConversionContextTests
{
    [Test]
    public void StrictContextUsesInvariantFailClosedPolicies()
    {
        ConversionContext context = ConversionContext.Strict;

        context.Culture.ShouldBe(CultureInfo.InvariantCulture);
        context.Culture.IsReadOnly.ShouldBeTrue();
        context.Overflow.ShouldBe(OverflowPolicy.Fail);
        context.Nulls.ShouldBe(NullPolicy.Preserve);
        context.Enums.ShouldBe(EnumPolicy.DefinedValuesAndFlags);
        context.NumericLoss.ShouldBe(NumericLossPolicy.Reject);
        context.NumericBooleans.ShouldBe(NumericBooleanPolicy.Reject);
        context.TemporalText.ShouldBe(TemporalTextPolicy.RoundTrip);
    }

    [Test]
    public void ContextClonesAndFreezesCulture()
    {
        var sourceCulture = (CultureInfo)CultureInfo.GetCultureInfo("de-DE").Clone();
        var context = new ConversionContext(sourceCulture);

        sourceCulture.NumberFormat.NumberDecimalSeparator = "!";

        context.Culture.NumberFormat.NumberDecimalSeparator.ShouldBe(",");
        context.Culture.IsReadOnly.ShouldBeTrue();
    }

    [Test]
    public void ContextRejectsNullCulture()
    {
        Should.Throw<ArgumentNullException>(() => new ConversionContext(null!));
    }

    [Test]
    public void ContextRejectsUndefinedPolicies()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new ConversionContext(
            CultureInfo.InvariantCulture,
            (OverflowPolicy)99));
    }
}
