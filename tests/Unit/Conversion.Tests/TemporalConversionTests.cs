// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;

namespace Pocok.Conversion.Tests;

public sealed class TemporalConversionTests
{
    private readonly IValueConverter _converter = new ValueConverter();

    private static IEnumerable<TestCaseData> RoundTripValues()
    {
        yield return new TestCaseData(
            new DateTime(2026, 7, 14, 12, 34, 56, DateTimeKind.Utc), typeof(DateTime));
        yield return new TestCaseData(
            new DateTimeOffset(2026, 7, 14, 12, 34, 56, TimeSpan.FromHours(2)), typeof(DateTimeOffset));
        yield return new TestCaseData(new DateOnly(2026, 7, 14), typeof(DateOnly));
        yield return new TestCaseData(new TimeOnly(12, 34, 56, 789), typeof(TimeOnly));
        yield return new TestCaseData(TimeSpan.FromHours(27.5), typeof(TimeSpan));
    }

    [TestCaseSource(nameof(RoundTripValues))]
    public void TemporalValuesRoundTripThroughInvariantText(object value, Type targetType)
    {
        var text = _converter.Convert<string>(value).Value;
        var converted = _converter.Convert(text, targetType);

        converted.Value.ShouldBe(value);
    }

    [Test]
    public void StrictTemporalPolicyRejectsCultureSpecificText()
    {
        _converter.Convert<DateOnly>("14.07.2026").Error!.Code.ShouldBe(ConversionErrorCodes.InvalidFormat);
    }

    [Test]
    public void CultureAwareTemporalPolicyUsesSuppliedCulture()
    {
        var context = new ConversionContext(
            CultureInfo.GetCultureInfo("de-DE"),
            temporalText: TemporalTextPolicy.CultureAware);

        _converter.Convert<DateOnly>("14.07.2026", context).Value.ShouldBe(new DateOnly(2026, 7, 14));
        _converter.Convert<TimeOnly>("13:45", context).Value.ShouldBe(new TimeOnly(13, 45));
    }

    [Test]
    public void DateTimeOffsetConvertsToUtcDateTimeDeterministically()
    {
        var source = new DateTimeOffset(2026, 7, 14, 12, 0, 0, TimeSpan.FromHours(2));

        _converter.Convert<DateTime>(source).Value.ShouldBe(
            new DateTime(2026, 7, 14, 10, 0, 0, DateTimeKind.Utc));
    }

    [Test]
    public void OnlyUtcDateTimeConvertsToDateTimeOffset()
    {
        var utc = new DateTime(2026, 7, 14, 10, 0, 0, DateTimeKind.Utc);
        var unspecified = DateTime.SpecifyKind(utc, DateTimeKind.Unspecified);

        _converter.Convert<DateTimeOffset>(utc).Value.ShouldBe(new DateTimeOffset(utc));
        _converter.Convert<DateTimeOffset>(unspecified).Error!.Code.ShouldBe(ConversionErrorCodes.Unsupported);
    }

    [Test]
    public void LocalDateTimeTextAndFormattingAreRejected()
    {
        var offsetText = new DateTimeOffset(2026, 7, 14, 12, 0, 0, TimeSpan.FromHours(2))
            .ToString("O", CultureInfo.InvariantCulture);
        var localValue = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Local);

        _converter.Convert<DateTime>(offsetText).Error!.Code.ShouldBe(ConversionErrorCodes.InvalidFormat);
        _converter.Convert<string>(localValue).Error!.Code.ShouldBe(ConversionErrorCodes.Unsupported);
    }

    [Test]
    public void DateAndTimeProjectionsAreExplicit()
    {
        var dateTime = new DateTime(2026, 7, 14, 12, 34, 56, DateTimeKind.Utc);

        _converter.Convert<DateOnly>(dateTime).Value.ShouldBe(new DateOnly(2026, 7, 14));
        _converter.Convert<TimeOnly>(dateTime).Value.ShouldBe(new TimeOnly(12, 34, 56));
        _converter.Convert<TimeSpan>(new TimeOnly(12, 34, 56)).Value.ShouldBe(new TimeSpan(12, 34, 56));
    }

    [Test]
    public void TemporalFormattingDoesNotChangeWithContextCulture()
    {
        var context = new ConversionContext(CultureInfo.GetCultureInfo("de-DE"));
        var date = new DateOnly(2026, 7, 14);

        _converter.Convert<string>(date, context).Value.ShouldBe("2026-07-14");
    }
}
