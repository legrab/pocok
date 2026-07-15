// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;

namespace Pocok.Conversion.Tests;

public sealed class NumericConversionTests
{
    private readonly IValueConverter _converter = new ValueConverter();

    private static IEnumerable<TestCaseData> NumericTargets()
    {
        yield return new TestCaseData("12", typeof(byte), (byte)12);
        yield return new TestCaseData("-12", typeof(sbyte), (sbyte)-12);
        yield return new TestCaseData("-1200", typeof(short), (short)-1200);
        yield return new TestCaseData("1200", typeof(ushort), (ushort)1200);
        yield return new TestCaseData("-120000", typeof(int), -120000);
        yield return new TestCaseData("120000", typeof(uint), 120000U);
        yield return new TestCaseData("-12000000000", typeof(long), -12000000000L);
        yield return new TestCaseData("12000000000", typeof(ulong), 12000000000UL);
        yield return new TestCaseData("12.5", typeof(float), 12.5F);
        yield return new TestCaseData("12.5", typeof(double), 12.5D);
        yield return new TestCaseData("12.5", typeof(decimal), 12.5M);
    }

    [TestCaseSource(nameof(NumericTargets))]
    public void NumericTextConvertsAcrossSupportedTargets(string source, Type targetType, object expected)
    {
        ConversionResult<object?> result = _converter.Convert(source, targetType);

        result.Value.ShouldBe(expected);
    }

    [Test]
    public void SuppliedCultureControlsNumericParsing()
    {
        var context = new ConversionContext(CultureInfo.GetCultureInfo("de-DE"));

        _converter.Convert<decimal>("1.234,5", context).Value.ShouldBe(1234.5m);
        _converter.Convert<decimal>("1,234.5", context).IsFailure.ShouldBeTrue();
    }

    [TestCase(1.1)]
    [TestCase(-1.1)]
    [TestCase(0.5)]
    public void StrictIntegralConversionRejectsFractionalLoss(double source)
    {
        ConversionResult<int> result = _converter.Convert<int>(source);

        result.Error!.Code.ShouldBe(ConversionErrorCodes.Lossy);
    }

    [TestCase(1.4, 1)]
    [TestCase(1.5, 2)]
    [TestCase(-1.5, -2)]
    public void RoundingPolicyUsesMidpointAwayFromZero(double source, int expected)
    {
        var context = new ConversionContext(
            CultureInfo.InvariantCulture,
            numericLoss: NumericLossPolicy.RoundToNearest);

        _converter.Convert<int>(source, context).Value.ShouldBe(expected);
    }

    [Test]
    public void CheckedOverflowFails()
    {
        _converter.Convert<byte>(300).Error!.Code.ShouldBe(ConversionErrorCodes.Overflow);
        _converter.Convert<uint>(-1).Error!.Code.ShouldBe(ConversionErrorCodes.Overflow);
        _converter.Convert<long>(ulong.MaxValue).Error!.Code.ShouldBe(ConversionErrorCodes.Overflow);
    }

    [Test]
    public void SaturatingOverflowClampsBothBoundaries()
    {
        var context = new ConversionContext(CultureInfo.InvariantCulture, OverflowPolicy.Saturate);

        _converter.Convert<byte>(300, context).Value.ShouldBe(byte.MaxValue);
        _converter.Convert<byte>(-10, context).Value.ShouldBe(byte.MinValue);
        _converter.Convert<int>(long.MaxValue, context).Value.ShouldBe(int.MaxValue);
        _converter.Convert<int>(long.MinValue, context).Value.ShouldBe(int.MinValue);
    }

    [Test]
    public void FiniteFloatOverflowFollowsPolicy()
    {
        _converter.Convert<float>(double.MaxValue).Error!.Code.ShouldBe(ConversionErrorCodes.Overflow);

        var context = new ConversionContext(CultureInfo.InvariantCulture, OverflowPolicy.Saturate);
        _converter.Convert<float>(double.MaxValue, context).Value.ShouldBe(float.MaxValue);
        _converter.Convert<float>(double.MinValue, context).Value.ShouldBe(float.MinValue);
    }

    [Test]
    public void FloatingTargetsPreserveNanAndInfinity()
    {
        float.IsNaN(_converter.Convert<float>(double.NaN).Value).ShouldBeTrue();
        _converter.Convert<double>(float.PositiveInfinity).Value.ShouldBe(double.PositiveInfinity);
        _converter.Convert<float>(double.NegativeInfinity).Value.ShouldBe(float.NegativeInfinity);
    }

    [Test]
    public void NonFiniteValuesCannotBecomeIntegralOrDecimal()
    {
        _converter.Convert<int>(double.NaN).Error!.Code.ShouldBe(ConversionErrorCodes.Overflow);
        _converter.Convert<long>(double.PositiveInfinity).Error!.Code.ShouldBe(ConversionErrorCodes.Overflow);
        _converter.Convert<decimal>(double.NegativeInfinity).Error!.Code.ShouldBe(ConversionErrorCodes.Overflow);
    }

    [Test]
    public void BooleanToNumericConversionRequiresExplicitPolicy()
    {
        _converter.Convert<int>(true).Error!.Code.ShouldBe(ConversionErrorCodes.Unsupported);

        var context = new ConversionContext(
            CultureInfo.InvariantCulture,
            numericBooleans: NumericBooleanPolicy.ZeroOrOne);

        _converter.Convert<int>(true, context).Value.ShouldBe(1);
        _converter.Convert<decimal>(false, context).Value.ShouldBe(decimal.Zero);
    }

    [Test]
    public void CharacterUsesItsNumericCodePoint()
    {
        _converter.Convert<int>('A').Value.ShouldBe(65);
    }

    [Test]
    public void InvalidNumericTextHasStableFormatFailure()
    {
        _converter.Convert<int>("twelve").Error!.Code.ShouldBe(ConversionErrorCodes.InvalidFormat);
    }

    [Test]
    public void HugeNumericTextIsReportedAsOverflow()
    {
        _converter.Convert<int>("1e100").Error!.Code.ShouldBe(ConversionErrorCodes.Overflow);
    }

    [Test]
    public void HugeNumericTextCanSaturate()
    {
        var context = new ConversionContext(CultureInfo.InvariantCulture, OverflowPolicy.Saturate);

        _converter.Convert<int>("1e100", context).Value.ShouldBe(int.MaxValue);
        _converter.Convert<int>("-1e100", context).Value.ShouldBe(int.MinValue);
        _converter.Convert<double>("1e1000", context).Value.ShouldBe(double.MaxValue);
        _converter.Convert<float>("-1e1000", context).Value.ShouldBe(float.MinValue);
    }

    [Test]
    public void ExplicitInfinityRemainsAvailableOnlyToFloatingTargets()
    {
        _converter.Convert<double>("Infinity").Value.ShouldBe(double.PositiveInfinity);
        _converter.Convert<int>("Infinity").Error!.Code.ShouldBe(ConversionErrorCodes.Overflow);
    }
}
