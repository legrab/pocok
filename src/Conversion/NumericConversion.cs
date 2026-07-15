// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Pocok.Primitives;

namespace Pocok.Conversion;

internal static class NumericConversion
{
    private const NumberStyles NumericTextStyles = NumberStyles.Float | NumberStyles.AllowThousands;

    internal static Result<object?> Convert(object value, Type targetType, ConversionContext context)
    {
        if (targetType == typeof(float))
        {
            return ConvertToFloat(value, context);
        }

        if (targetType == typeof(double))
        {
            return ConvertToDouble(value, context);
        }

        var decimalResult = ReadDecimal(value, targetType, context);
        if (decimalResult.IsFailure)
        {
            return Result<object?>.Failure(decimalResult.Error!);
        }

        var decimalValue = decimalResult.Value;
        if (targetType == typeof(decimal))
        {
            return Result<object?>.Success(decimalValue);
        }

        return ConvertDecimalToIntegral(decimalValue, targetType, context);
    }

    internal static bool TryReadFiniteDouble(object value, out double number)
    {
        try
        {
            var source = NormalizeNumericSource(value);

            number = System.Convert.ToDouble(source, CultureInfo.InvariantCulture);
            return double.IsFinite(number);
        }
        catch (Exception exception) when (exception is InvalidCastException or FormatException or OverflowException)
        {
            number = default;
            return false;
        }
    }

    private static Result<decimal> ReadDecimal(object value, Type targetType, ConversionContext context)
    {
        if (value is bool boolean)
        {
            if (context.NumericBooleans == NumericBooleanPolicy.Reject)
            {
                return Result<decimal>.Failure(new Error(
                    ConversionErrorCodes.Unsupported,
                    "Boolean-to-numeric conversion is disabled by the selected policy."));
            }

            return Result<decimal>.Success(boolean ? decimal.One : decimal.Zero);
        }

        if (value is string text)
        {
            if (decimal.TryParse(text, NumericTextStyles, context.Culture, out var parsed))
            {
                return Result<decimal>.Success(parsed);
            }

            if (double.TryParse(text, NumericTextStyles, context.Culture, out var floating) &&
                !double.IsNaN(floating) &&
                (double.IsInfinity(floating) || Math.Abs(floating) > (double)decimal.MaxValue))
            {
                return SaturateDecimalOrFail(floating, targetType, context);
            }

            return Result<decimal>.Failure(ConversionFailures.InvalidFormat(targetType).Error!);
        }

        var source = NormalizeNumericSource(value);

        if (source is float single)
        {
            if (!float.IsFinite(single))
            {
                return float.IsNaN(single)
                    ? Result<decimal>.Failure(ConversionFailures.Overflow(targetType).Error!)
                    : SaturateDecimalOrFail(single, targetType, context);
            }

            try
            {
                return Result<decimal>.Success((decimal)single);
            }
            catch (OverflowException)
            {
                return SaturateDecimalOrFail(single, targetType, context);
            }
        }

        if (source is double floatingPoint)
        {
            if (!double.IsFinite(floatingPoint))
            {
                return double.IsNaN(floatingPoint)
                    ? Result<decimal>.Failure(ConversionFailures.Overflow(targetType).Error!)
                    : SaturateDecimalOrFail(floatingPoint, targetType, context);
            }

            try
            {
                return Result<decimal>.Success((decimal)floatingPoint);
            }
            catch (OverflowException)
            {
                return SaturateDecimalOrFail(floatingPoint, targetType, context);
            }
        }

        try
        {
            return Result<decimal>.Success(System.Convert.ToDecimal(source, context.Culture));
        }
        catch (OverflowException)
        {
            return Result<decimal>.Failure(ConversionFailures.Overflow(targetType).Error!);
        }
        catch (Exception exception) when (exception is InvalidCastException or FormatException)
        {
            return Result<decimal>.Failure(ConversionFailures.Unsupported(value.GetType(), targetType).Error!);
        }
    }

    private static Result<object?> ConvertDecimalToIntegral(
        decimal value,
        Type targetType,
        ConversionContext context)
    {
        var integralValue = decimal.Truncate(value);
        if (integralValue != value)
        {
            if (context.NumericLoss == NumericLossPolicy.Reject)
            {
                return ConversionFailures.Lossy(targetType);
            }

            integralValue = decimal.Round(value, 0, MidpointRounding.AwayFromZero);
        }

        var (minimum, maximum) = GetIntegralBounds(targetType);
        if (integralValue < minimum || integralValue > maximum)
        {
            if (context.Overflow == OverflowPolicy.Fail)
            {
                return ConversionFailures.Overflow(targetType);
            }

            integralValue = decimal.Clamp(integralValue, minimum, maximum);
        }

        object converted = targetType == typeof(byte) ? decimal.ToByte(integralValue) :
            targetType == typeof(sbyte) ? decimal.ToSByte(integralValue) :
            targetType == typeof(short) ? decimal.ToInt16(integralValue) :
            targetType == typeof(ushort) ? decimal.ToUInt16(integralValue) :
            targetType == typeof(int) ? decimal.ToInt32(integralValue) :
            targetType == typeof(uint) ? decimal.ToUInt32(integralValue) :
            targetType == typeof(long) ? decimal.ToInt64(integralValue) :
            targetType == typeof(ulong) ? decimal.ToUInt64(integralValue) :
            throw new ArgumentException("Unsupported integral target type.", nameof(targetType));

        return Result<object?>.Success(converted);
    }

    private static Result<object?> ConvertToFloat(object value, ConversionContext context)
    {
        if (!TryReadDouble(value, typeof(float), context, out var source, out var failure))
        {
            return failure!;
        }

        if (double.IsFinite(source) && (source > float.MaxValue || source < float.MinValue))
        {
            return context.Overflow == OverflowPolicy.Fail
                ? ConversionFailures.Overflow(typeof(float))
                : Result<object?>.Success(source > 0 ? float.MaxValue : float.MinValue);
        }

        return Result<object?>.Success((float)source);
    }

    private static Result<object?> ConvertToDouble(object value, ConversionContext context)
    {
        if (!TryReadDouble(value, typeof(double), context, out var source, out var failure))
        {
            return failure!;
        }

        return Result<object?>.Success(source);
    }

    private static bool TryReadDouble(
        object value,
        Type targetType,
        ConversionContext context,
        out double number,
        out Result<object?>? failure)
    {
        if (value is bool boolean)
        {
            if (context.NumericBooleans == NumericBooleanPolicy.Reject)
            {
                number = default;
                failure = Result<object?>.Failure(new Error(
                    ConversionErrorCodes.Unsupported,
                    "Boolean-to-numeric conversion is disabled by the selected policy."));
                return false;
            }

            number = boolean ? 1 : 0;
            failure = null;
            return true;
        }

        if (value is string text)
        {
            if (double.TryParse(text, NumericTextStyles, context.Culture, out number))
            {
                if (double.IsInfinity(number) && !IsExplicitInfinity(text, context.Culture))
                {
                    if (context.Overflow == OverflowPolicy.Fail)
                    {
                        failure = ConversionFailures.Overflow(targetType);
                        return false;
                    }

                    number = number > 0
                        ? targetType == typeof(float) ? float.MaxValue : double.MaxValue
                        : targetType == typeof(float) ? float.MinValue : double.MinValue;
                }

                failure = null;
                return true;
            }

            number = default;
            failure = ConversionFailures.InvalidFormat(targetType);
            return false;
        }

        var source = NormalizeNumericSource(value);

        try
        {
            number = System.Convert.ToDouble(source, context.Culture);
            failure = null;
            return true;
        }
        catch (OverflowException)
        {
            number = default;
            failure = ConversionFailures.Overflow(targetType);
            return false;
        }
        catch (Exception exception) when (exception is InvalidCastException or FormatException)
        {
            number = default;
            failure = ConversionFailures.Unsupported(value.GetType(), targetType);
            return false;
        }
    }

    private static Result<decimal> SaturateDecimalOrFail(
        double value,
        Type targetType,
        ConversionContext context)
    {
        if (context.Overflow == OverflowPolicy.Fail)
        {
            return Result<decimal>.Failure(ConversionFailures.Overflow(targetType).Error!);
        }

        return Result<decimal>.Success(value < 0 ? decimal.MinValue : decimal.MaxValue);
    }

    private static bool IsExplicitInfinity(string text, CultureInfo culture)
    {
        var trimmed = text.Trim();
        return string.Equals(trimmed, culture.NumberFormat.PositiveInfinitySymbol, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(trimmed, culture.NumberFormat.NegativeInfinitySymbol, StringComparison.OrdinalIgnoreCase);
    }

    private static (decimal Minimum, decimal Maximum) GetIntegralBounds(Type targetType) =>
        targetType == typeof(byte) ? (byte.MinValue, byte.MaxValue) :
        targetType == typeof(sbyte) ? (sbyte.MinValue, sbyte.MaxValue) :
        targetType == typeof(short) ? (short.MinValue, short.MaxValue) :
        targetType == typeof(ushort) ? (ushort.MinValue, ushort.MaxValue) :
        targetType == typeof(int) ? (int.MinValue, int.MaxValue) :
        targetType == typeof(uint) ? (uint.MinValue, uint.MaxValue) :
        targetType == typeof(long) ? (long.MinValue, long.MaxValue) :
        targetType == typeof(ulong) ? (ulong.MinValue, ulong.MaxValue) :
        throw new ArgumentException("Unsupported integral target type.", nameof(targetType));

    private static object NormalizeNumericSource(object value)
    {
        if (value is char character)
        {
            return (ushort)character;
        }

        return value.GetType().IsEnum
            ? System.Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()), CultureInfo.InvariantCulture)
            : value;
    }
}
