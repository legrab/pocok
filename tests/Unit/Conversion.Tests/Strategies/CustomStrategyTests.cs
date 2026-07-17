// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Conversion.Strategies;

namespace Pocok.Conversion.Tests.Strategies;

public sealed class CustomStrategyTests
{
    [Test]
    public void AfterBuiltInsDoesNotReplaceSupportedBuiltInConversion()
    {
        var converter = new ValueConverter([new ConstantIntegerStrategy(99)]);

        converter.Convert<int>("42").Value.ShouldBe(42);
    }

    [Test]
    public void BeforeBuiltInsCanExplicitlyReplaceBuiltInConversion()
    {
        var converter = new ValueConverter(
            [new ConstantIntegerStrategy(99)],
            ConversionStrategyPrecedence.BeforeBuiltIns);

        converter.Convert<int>("42").Value.ShouldBe(99);
    }

    [Test]
    public void StrategiesRunInCallerOrder()
    {
        var converter = new ValueConverter([
            new NotApplicableStrategy(),
            new ConstantIntegerStrategy(7),
            new ConstantIntegerStrategy(8)
        ], ConversionStrategyPrecedence.BeforeBuiltIns);

        converter.Convert<int>(new object()).Value.ShouldBe(7);
    }

    [Test]
    public void CustomFailureReceivesCurrentPath()
    {
        var converter = new ValueConverter([new FailingStringStrategy()]);
        ConversionResult<string[]> result = converter.Convert<string[]>(new[] { new object() });

        result.Error!.Path.ShouldBe("$[0]");
        result.Error.Code.ShouldBe("custom.failed");
    }

    private sealed class ConstantIntegerStrategy(int value) : IConversionStrategy
    {
        public ConversionStrategyResult TryConvert(object? source, Type targetType, ConversionStrategyContext context)
        {
            return targetType == typeof(int)
                ? ConversionStrategyResult.Success(value)
                : ConversionStrategyResult.NotApplicable();
        }
    }

    private sealed class NotApplicableStrategy : IConversionStrategy
    {
        public ConversionStrategyResult TryConvert(object? value, Type targetType, ConversionStrategyContext context)
        {
            return ConversionStrategyResult.NotApplicable();
        }
    }

    private sealed class FailingStringStrategy : IConversionStrategy
    {
        public ConversionStrategyResult TryConvert(object? value, Type targetType, ConversionStrategyContext context)
        {
            return targetType == typeof(string)
                ? ConversionStrategyResult.Failed(new ConversionFailure("custom.failed", "Custom conversion failed."))
                : ConversionStrategyResult.NotApplicable();
        }
    }
}
