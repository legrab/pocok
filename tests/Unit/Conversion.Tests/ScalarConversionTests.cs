// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;

namespace Pocok.Conversion.Tests;

public sealed class ScalarConversionTests
{
    private readonly IValueConverter _converter = new ValueConverter();

    [Test]
    public void NullIsPreservedForReferenceAndNullableTargets()
    {
        _converter.Convert<string?>(null).Value.ShouldBeNull();
        _converter.Convert<int?>(null).Value.ShouldBeNull();
    }

    [Test]
    public void StrictNullFailsForNonNullableTarget()
    {
        var result = _converter.Convert<int>(null);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(ConversionErrorCodes.NullNotAllowed);
    }

    [Test]
    public void RejectPolicyRejectsNullForNullableTarget()
    {
        var context = new ConversionContext(CultureInfo.InvariantCulture, nulls: NullPolicy.Reject);

        _converter.Convert<string?>(null, context).Error!.Code.ShouldBe(ConversionErrorCodes.NullNotAllowed);
    }

    [Test]
    public void UseDefaultPolicyReturnsValueAndReferenceDefaults()
    {
        var context = new ConversionContext(CultureInfo.InvariantCulture, nulls: NullPolicy.UseDefault);

        _converter.Convert<int>(null, context).Value.ShouldBe(0);
        _converter.Convert<int?>(null, context).Value.ShouldBeNull();
        _converter.Convert<string?>(null, context).Value.ShouldBeNull();
    }

    [TestCase("true", true)]
    [TestCase("FALSE", false)]
    public void BooleanTextIsParsed(string source, bool expected) =>
        _converter.Convert<bool>(source).Value.ShouldBe(expected);

    [Test]
    public void InvalidBooleanTextFailsSafely() =>
        _converter.Convert<bool>("enabled").Error!.Code.ShouldBe(ConversionErrorCodes.InvalidFormat);

    [Test]
    public void NumericBooleansAreDisabledByDefault() =>
        _converter.Convert<bool>(1).Error!.Code.ShouldBe(ConversionErrorCodes.Unsupported);

    [TestCase(0, false)]
    [TestCase(1, true)]
    public void ZeroOrOneBooleanPolicyIsExplicit(int source, bool expected)
    {
        var context = new ConversionContext(
            CultureInfo.InvariantCulture,
            numericBooleans: NumericBooleanPolicy.ZeroOrOne);

        _converter.Convert<bool>(source, context).Value.ShouldBe(expected);
    }

    [Test]
    public void ZeroOrOneBooleanPolicyRejectsOtherValues()
    {
        var context = new ConversionContext(
            CultureInfo.InvariantCulture,
            numericBooleans: NumericBooleanPolicy.ZeroOrOne);

        _converter.Convert<bool>(-1, context).Error!.Code.ShouldBe(ConversionErrorCodes.InvalidFormat);
    }

    [TestCase(-2, true)]
    [TestCase(0, false)]
    [TestCase(0.25, true)]
    public void NonZeroBooleanPolicyAcceptsFiniteNumbers(double source, bool expected)
    {
        var context = new ConversionContext(
            CultureInfo.InvariantCulture,
            numericBooleans: NumericBooleanPolicy.NonZeroIsTrue);

        _converter.Convert<bool>(source, context).Value.ShouldBe(expected);
    }

    [Test]
    public void GuidRoundTripsThroughCanonicalText()
    {
        var value = Guid.Parse("88d6ff94-8f21-4137-b122-83967ae278e9");

        var text = _converter.Convert<string>(value).Value;

        text.ShouldBe("88d6ff94-8f21-4137-b122-83967ae278e9");
        _converter.Convert<Guid>(text).Value.ShouldBe(value);
    }

    [Test]
    public void CharacterRequiresExactlyOneCharacter()
    {
        _converter.Convert<char>("x").Value.ShouldBe('x');
        _converter.Convert<char>("xy").Error!.Code.ShouldBe(ConversionErrorCodes.InvalidFormat);
    }

    [Test]
    public void NumericStringFormattingUsesSuppliedCulture()
    {
        var context = new ConversionContext(CultureInfo.GetCultureInfo("de-DE"));

        _converter.Convert<string>(12.5m, context).Value.ShouldBe("12,5");
    }

    [Test]
    public void UnsupportedObjectConversionDoesNotUseSerializerFallback()
    {
        var result = _converter.Convert<SampleRecord>(new { Value = 42 });

        result.Error!.Code.ShouldBe(ConversionErrorCodes.Unsupported);
    }

    [Test]
    public void NonGenericConversionMatchesGenericConversion()
    {
        var result = _converter.Convert("42", typeof(int));

        result.Value.ShouldBe(42);
    }

    [Test]
    public void InvalidTargetTypeIsAnArgumentError()
    {
        Should.Throw<ArgumentNullException>(() => _converter.Convert("x", null!));
        Should.Throw<ArgumentException>(() => _converter.Convert("x", typeof(List<>)));
        Should.Throw<ArgumentException>(() => _converter.Convert("x", typeof(Span<int>)));
    }

    private sealed record SampleRecord(int Value);
}
