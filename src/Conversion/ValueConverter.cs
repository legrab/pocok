// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Pocok.Primitives;

namespace Pocok.Conversion;

/// <summary>
/// Provides stateless, serializer-free value conversion with explicit policies.
/// </summary>
/// <remarks>
/// Instances contain no mutable state and are safe for concurrent use.
/// </remarks>
public sealed class ValueConverter : IValueConverter
{
    /// <inheritdoc />
    public Result<TTarget> Convert<TTarget>(object? value, ConversionContext? context = null)
    {
        var result = Convert(value, typeof(TTarget), context);
        if (result.IsFailure)
        {
            return Result<TTarget>.Failure(result.Error!);
        }

        if (result.Value is null)
        {
            return Result<TTarget>.Success(default!);
        }

        if (result.Value is TTarget typedValue)
        {
            return Result<TTarget>.Success(typedValue);
        }

        throw new InvalidOperationException("The conversion engine returned a value incompatible with its target type.");
    }

    /// <inheritdoc />
    public Result<object?> Convert(object? value, Type targetType, ConversionContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(targetType);
        if (!TypeShape.IsValidTarget(targetType))
        {
            throw new ArgumentException("The target must be a closed, boxable, non-pointer value or reference type.",
                nameof(targetType));
        }

        var effectiveContext = context ?? ConversionContext.Strict;
        if (value is null)
        {
            return ConvertNull(targetType, effectiveContext.Nulls);
        }

        var effectiveTargetType = TypeShape.UnwrapNullable(targetType);
        if (effectiveTargetType.IsInstanceOfType(value) || effectiveTargetType == typeof(object))
        {
            return Result<object?>.Success(value);
        }

        if (effectiveTargetType == typeof(string))
        {
            return ConvertToString(value, effectiveContext);
        }

        if (effectiveTargetType == typeof(char))
        {
            return ConvertToCharacter(value);
        }

        if (effectiveTargetType == typeof(bool))
        {
            return ConvertToBoolean(value, effectiveContext);
        }

        if (TypeShape.IsNumeric(effectiveTargetType))
        {
            if (!TypeShape.IsNumeric(value.GetType()) && value is not string and not char and not bool &&
                !value.GetType().IsEnum)
            {
                return ConversionFailures.Unsupported(value.GetType(), effectiveTargetType);
            }

            return NumericConversion.Convert(value, effectiveTargetType, effectiveContext);
        }

        if (effectiveTargetType.IsEnum)
        {
            return EnumConversion.Convert(value, effectiveTargetType, effectiveContext);
        }

        if (effectiveTargetType == typeof(Guid))
        {
            return value is string text && Guid.TryParse(text, out var guid)
                ? Result<object?>.Success(guid)
                : ConversionFailures.InvalidFormat(effectiveTargetType);
        }

        if (TypeShape.IsTemporal(effectiveTargetType))
        {
            return TemporalConversion.Convert(value, effectiveTargetType, effectiveContext);
        }

        if (CollectionConversion.IsPairOrCollectionTarget(effectiveTargetType))
        {
            return CollectionConversion.Convert(value, effectiveTargetType, effectiveContext, Convert);
        }

        return ConversionFailures.Unsupported(value.GetType(), effectiveTargetType);
    }

    private static Result<object?> ConvertNull(Type targetType, NullPolicy nullPolicy)
    {
        if (nullPolicy == NullPolicy.Reject)
        {
            return ConversionFailures.Null(targetType);
        }

        if (nullPolicy == NullPolicy.Preserve)
        {
            return TypeShape.PermitsNull(targetType)
                ? Result<object?>.Success(null)
                : ConversionFailures.Null(targetType);
        }

        return Result<object?>.Success(targetType.IsValueType ? Activator.CreateInstance(targetType) : null);
    }

    private static Result<object?> ConvertToCharacter(object value) =>
        value is string { Length: 1 } text
            ? Result<object?>.Success(text[0])
            : ConversionFailures.InvalidFormat(typeof(char));

    private static Result<object?> ConvertToBoolean(object value, ConversionContext context)
    {
        if (value is string text)
        {
            if (bool.TryParse(text, out var parsedBoolean))
            {
                return Result<object?>.Success(parsedBoolean);
            }

            if (context.NumericBooleans == NumericBooleanPolicy.Reject ||
                !double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, context.Culture,
                    out var parsedNumber) || !double.IsFinite(parsedNumber))
            {
                return ConversionFailures.InvalidFormat(typeof(bool));
            }

            return ConvertNumericToBoolean(parsedNumber, context.NumericBooleans);
        }

        if (context.NumericBooleans == NumericBooleanPolicy.Reject ||
            (!TypeShape.IsNumeric(value.GetType()) && !value.GetType().IsEnum && value is not char))
        {
            return ConversionFailures.Unsupported(value.GetType(), typeof(bool));
        }

        return NumericConversion.TryReadFiniteDouble(value, out var number)
            ? ConvertNumericToBoolean(number, context.NumericBooleans)
            : ConversionFailures.InvalidFormat(typeof(bool));
    }

    private static Result<object?> ConvertNumericToBoolean(double number, NumericBooleanPolicy policy)
    {
        if (policy == NumericBooleanPolicy.ZeroOrOne && number is not 0 and not 1)
        {
            return ConversionFailures.InvalidFormat(typeof(bool));
        }

        return Result<object?>.Success(number != 0);
    }

    private static Result<object?> ConvertToString(object value, ConversionContext context)
    {
        if (TypeShape.IsTemporal(value.GetType()))
        {
            if (value is DateTime { Kind: DateTimeKind.Local })
            {
                return ConversionFailures.Unsupported(typeof(DateTime), typeof(string));
            }

            return Result<object?>.Success(TemporalConversion.Format(value));
        }

        if (value is Guid guid)
        {
            return Result<object?>.Success(guid.ToString("D", CultureInfo.InvariantCulture));
        }

        if (value is char character)
        {
            return Result<object?>.Success(character.ToString());
        }

        if (value is bool boolean)
        {
            return Result<object?>.Success(boolean ? bool.TrueString : bool.FalseString);
        }

        if (value.GetType().IsEnum)
        {
            return Result<object?>.Success(value.ToString());
        }

        if (TypeShape.IsNumeric(value.GetType()) && value is IFormattable formattable)
        {
            return Result<object?>.Success(formattable.ToString(null, context.Culture));
        }

        return ConversionFailures.Unsupported(value.GetType(), typeof(string));
    }
}
