// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using System.Text;

namespace Pocok.Conversion.Tests;

public sealed class RobustnessTests
{
    [TestCase("en-US", "1,234.5", 1234.5)]
    [TestCase("de-DE", "1.234,5", 1234.5)]
    [TestCase("hu-HU", "1 234,5", 1234.5)]
    public void NumericTextUsesOnlyExplicitCulture(string cultureName, string text, double expected)
    {
        var context = new ConversionContext(CultureInfo.GetCultureInfo(cultureName));
        Math.Abs(ValueConverter.Default.Convert<double>(text, context).Value - expected).ShouldBeLessThan(0.0001);
    }

    [Test]
    public void IntegerBoundariesRemainExact()
    {
        ValueConverter.Default.Convert<long>(long.MinValue.ToString(CultureInfo.InvariantCulture)).Value
            .ShouldBe(long.MinValue);
        ValueConverter.Default.Convert<long>(long.MaxValue.ToString(CultureInfo.InvariantCulture)).Value
            .ShouldBe(long.MaxValue);
        ValueConverter.Default.Convert<ulong>(ulong.MaxValue.ToString(CultureInfo.InvariantCulture)).Value
            .ShouldBe(ulong.MaxValue);
    }

    [Test]
    public void BoundedMalformedTextFuzzNeverThrows()
    {
        var random = new Random(14327);
        ValueConverter converter = ValueConverter.Default;
        for (var iteration = 0; iteration < 2_000; iteration++)
        {
            var length = random.Next(0, 128);
            var builder = new StringBuilder(length);
            for (var index = 0; index < length; index++) builder.Append((char)random.Next(0x20, 0x7f));

            var text = builder.ToString();
            _ = converter.Convert<decimal>(text);
            _ = converter.Convert<DateTimeOffset>(text);
            _ = converter.Convert<Guid>(text);
        }
    }
}
