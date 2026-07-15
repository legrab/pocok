// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Pocok.Conversion;

/// <summary>
///     Provides immutable, serializer-free value conversion with explicit policies.
/// </summary>
public sealed class ValueConverter : IValueConverter
{
    private readonly ConversionStrategyPrecedence _precedence;
    private readonly IConversionStrategy[] _strategies;

    /// <summary>Creates an immutable converter with explicitly supplied custom strategies.</summary>
    public ValueConverter(
        IEnumerable<IConversionStrategy>? additionalStrategies = null,
        ConversionStrategyPrecedence precedence = ConversionStrategyPrecedence.AfterBuiltIns)
    {
        if (!Enum.IsDefined(precedence))
            throw new ArgumentOutOfRangeException(nameof(precedence), precedence,
                "The precedence value is not defined.");

        _strategies = additionalStrategies?.ToArray() ?? [];
        if (_strategies.Any(static strategy => strategy is null))
            throw new ArgumentException("A custom conversion strategy cannot be null.", nameof(additionalStrategies));

        _precedence = precedence;
    }

    /// <summary>Gets the default converter containing only built-in strategies.</summary>
    public static ValueConverter Default { get; } = new();

    /// <inheritdoc />
    [RequiresUnreferencedCode(ConversionTrimming.IncompatibleMessage)]
    public ConversionResult<TTarget> Convert<TTarget>(object? value, ConversionContext? context = null)
    {
        ConversionResult<object?> result = Convert(value, typeof(TTarget), context);
        if (result.IsFailure) return ConversionResult<TTarget>.Failure(result.Error!);

        if (result.Value is null) return ConversionResult<TTarget>.Success(default!);

        if (result.Value is TTarget typedValue) return ConversionResult<TTarget>.Success(typedValue);

        throw new InvalidOperationException(
            "The conversion engine returned a value incompatible with its target type.");
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode(ConversionTrimming.IncompatibleMessage)]
    public ConversionResult<object?> Convert(object? value, Type targetType, ConversionContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(targetType);
        if (!TypeShape.IsValidTarget(targetType))
            throw new ArgumentException("The target must be a closed, boxable, non-pointer value or reference type.",
                nameof(targetType));

        var session = new ConversionSession(this, context ?? ConversionContext.Strict);
        return session.Convert(value, targetType);
    }

    [RequiresUnreferencedCode(ConversionTrimming.IncompatibleMessage)]
    internal ConversionResult<object?> ConvertCore(
        object? value,
        Type targetType,
        ConversionSession session,
        string path,
        int depth)
    {
        if (_precedence == ConversionStrategyPrecedence.BeforeBuiltIns)
        {
            ConversionResult<object?>? custom = TryCustomStrategies(value, targetType, session, path, depth);
            if (custom is not null) return custom;
        }

        ConversionResult<object?> builtIn = ConvertBuiltIn(value, targetType, session, path, depth);
        if (builtIn.IsSuccess || builtIn.Error!.Code != ConversionErrorCodes.Unsupported ||
            _precedence == ConversionStrategyPrecedence.BeforeBuiltIns)
            return builtIn;

        return TryCustomStrategies(value, targetType, session, path, depth) ?? builtIn;
    }

    [RequiresUnreferencedCode(ConversionTrimming.IncompatibleMessage)]
    private ConversionResult<object?>? TryCustomStrategies(
        object? value,
        Type targetType,
        ConversionSession session,
        string path,
        int depth)
    {
        if (_strategies.Length == 0) return null;

        var strategyContext = new ConversionStrategyContext(
            session.Context,
            path,
            (nestedValue, nestedType, segment) =>
                session.ConvertNested(nestedValue, nestedType, path + segment, depth));

        foreach (IConversionStrategy strategy in _strategies)
        {
            ConversionStrategyResult result = strategy.TryConvert(value, targetType, strategyContext)
                                              ?? throw new InvalidOperationException(
                                                  $"Conversion strategy {strategy.GetType().FullName} returned null.");

            if (result.Status == ConversionStrategyStatus.NotApplicable) continue;

            if (result.Status == ConversionStrategyStatus.Failed)
            {
                ConversionFailure failure = result.Failure
                                            ?? throw new InvalidOperationException(
                                                "A failed custom strategy did not supply a failure.");
                return ConversionResult<object?>.Failure(failure.Path == "$" ? failure.AtPath(path) : failure);
            }

            if (result.Failure is not null)
                throw new InvalidOperationException("A successful custom strategy supplied a failure.");

            Type effectiveTarget = TypeShape.UnwrapNullable(targetType);
            if (result.Value is null)
                return TypeShape.PermitsNull(targetType)
                    ? ConversionResult<object?>.Success(null)
                    : ConversionFailures.Null(targetType, path);

            if (!effectiveTarget.IsInstanceOfType(result.Value) && effectiveTarget != typeof(object))
                throw new InvalidOperationException(
                    $"Conversion strategy {strategy.GetType().FullName} returned {result.Value.GetType().FullName} for target {targetType.FullName}.");

            return ConversionResult<object?>.Success(result.Value);
        }

        return null;
    }

    [RequiresUnreferencedCode(ConversionTrimming.IncompatibleMessage)]
    private static ConversionResult<object?> ConvertBuiltIn(
        object? value,
        Type targetType,
        ConversionSession session,
        string path,
        int depth)
    {
        ConversionContext context = session.Context;
        if (value is null) return ConvertNull(targetType, context.Nulls, path);

        Type effectiveTargetType = TypeShape.UnwrapNullable(targetType);
        if (effectiveTargetType.IsInstanceOfType(value) || effectiveTargetType == typeof(object))
            return ConversionResult<object?>.Success(value);

        if (effectiveTargetType == typeof(string)) return ConvertToString(value, context);

        if (effectiveTargetType == typeof(char)) return ConvertToCharacter(value);

        if (effectiveTargetType == typeof(bool)) return ConvertToBoolean(value, context);

        if (TypeShape.IsNumeric(effectiveTargetType))
        {
            if (!TypeShape.IsNumeric(value.GetType()) && value is not string and not char and not bool &&
                !value.GetType().IsEnum)
                return ConversionFailures.Unsupported(value.GetType(), effectiveTargetType, path);

            return NumericConversion.Convert(value, effectiveTargetType, context);
        }

        if (effectiveTargetType.IsEnum) return EnumConversion.Convert(value, effectiveTargetType, context);

        if (effectiveTargetType == typeof(Guid))
            return value is string text && Guid.TryParse(text, out Guid guid)
                ? ConversionResult<object?>.Success(guid)
                : ConversionFailures.InvalidFormat(effectiveTargetType, path);

        if (TypeShape.IsTemporal(effectiveTargetType))
            return TemporalConversion.Convert(value, effectiveTargetType, context);

        if (CollectionConversion.IsPairOrCollectionTarget(effectiveTargetType))
            return CollectionConversion.Convert(value, effectiveTargetType, session, path, depth);

        return ConversionFailures.Unsupported(value.GetType(), effectiveTargetType, path);
    }

    [RequiresUnreferencedCode(ConversionTrimming.IncompatibleMessage)]
    private static ConversionResult<object?> ConvertNull(Type targetType, NullPolicy nullPolicy, string path)
    {
        if (nullPolicy == NullPolicy.Reject) return ConversionFailures.Null(targetType, path);

        if (nullPolicy == NullPolicy.Preserve)
            return TypeShape.PermitsNull(targetType)
                ? ConversionResult<object?>.Success(null)
                : ConversionFailures.Null(targetType, path);

        return ConversionResult<object?>.Success(targetType.IsValueType ? Activator.CreateInstance(targetType) : null);
    }

    private static ConversionResult<object?> ConvertToCharacter(object value)
    {
        return value is string { Length: 1 } text
            ? ConversionResult<object?>.Success(text[0])
            : ConversionFailures.InvalidFormat(typeof(char));
    }

    private static ConversionResult<object?> ConvertToBoolean(object value, ConversionContext context)
    {
        if (value is string text)
        {
            if (bool.TryParse(text, out var parsedBoolean)) return ConversionResult<object?>.Success(parsedBoolean);

            if (context.NumericBooleans == NumericBooleanPolicy.Reject ||
                !double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, context.Culture,
                    out var parsedNumber) ||
                !double.IsFinite(parsedNumber))
                return ConversionFailures.InvalidFormat(typeof(bool));

            return ConvertNumericToBoolean(parsedNumber, context.NumericBooleans);
        }

        if (context.NumericBooleans == NumericBooleanPolicy.Reject ||
            (!TypeShape.IsNumeric(value.GetType()) && !value.GetType().IsEnum && value is not char))
            return ConversionFailures.Unsupported(value.GetType(), typeof(bool));

        return NumericConversion.TryReadFiniteDouble(value, out var number)
            ? ConvertNumericToBoolean(number, context.NumericBooleans)
            : ConversionFailures.InvalidFormat(typeof(bool));
    }

    private static ConversionResult<object?> ConvertNumericToBoolean(double number, NumericBooleanPolicy policy)
    {
        if (policy == NumericBooleanPolicy.ZeroOrOne && number is not 0 and not 1)
            return ConversionFailures.InvalidFormat(typeof(bool));

        return ConversionResult<object?>.Success(number != 0);
    }

    private static ConversionResult<object?> ConvertToString(object value, ConversionContext context)
    {
        if (TypeShape.IsTemporal(value.GetType()))
        {
            if (value is DateTime { Kind: DateTimeKind.Local })
                return ConversionFailures.Unsupported(typeof(DateTime), typeof(string));

            return ConversionResult<object?>.Success(TemporalConversion.Format(value));
        }

        if (value is Guid guid)
            return ConversionResult<object?>.Success(guid.ToString("D", CultureInfo.InvariantCulture));

        if (value is char character) return ConversionResult<object?>.Success(character.ToString());

        if (value is bool boolean)
            return ConversionResult<object?>.Success(boolean ? bool.TrueString : bool.FalseString);

        if (value.GetType().IsEnum) return ConversionResult<object?>.Success(value.ToString());

        if (TypeShape.IsNumeric(value.GetType()) && value is IFormattable formattable)
            return ConversionResult<object?>.Success(formattable.ToString(null, context.Culture));

        return ConversionFailures.Unsupported(value.GetType(), typeof(string));
    }
}
