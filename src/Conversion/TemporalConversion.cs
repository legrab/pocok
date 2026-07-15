// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Pocok.Primitives;

namespace Pocok.Conversion;

internal static class TemporalConversion
{
    internal static Result<object?> Convert(object value, Type targetType, ConversionContext context)
    {
        if (value is string text)
        {
            return ParseText(text, targetType, context);
        }

        if (targetType == typeof(DateTime))
        {
            return value switch
            {
                DateTimeOffset dateTimeOffset => Result<object?>.Success(dateTimeOffset.UtcDateTime),
                DateOnly date => Result<object?>.Success(date.ToDateTime(TimeOnly.MinValue)),
                _ => ConversionFailures.Unsupported(value.GetType(), targetType)
            };
        }

        if (targetType == typeof(DateTimeOffset))
        {
            return value is DateTime dateTime && dateTime.Kind == DateTimeKind.Utc
                ? Result<object?>.Success(new DateTimeOffset(dateTime))
                : ConversionFailures.Unsupported(value.GetType(), targetType);
        }

        if (targetType == typeof(DateOnly))
        {
            return value switch
            {
                DateTime dateTime => Result<object?>.Success(DateOnly.FromDateTime(dateTime)),
                DateTimeOffset dateTimeOffset => Result<object?>.Success(DateOnly.FromDateTime(dateTimeOffset.UtcDateTime)),
                _ => ConversionFailures.Unsupported(value.GetType(), targetType)
            };
        }

        if (targetType == typeof(TimeOnly))
        {
            return value switch
            {
                DateTime dateTime => Result<object?>.Success(TimeOnly.FromDateTime(dateTime)),
                DateTimeOffset dateTimeOffset => Result<object?>.Success(TimeOnly.FromDateTime(dateTimeOffset.UtcDateTime)),
                TimeSpan timeSpan when timeSpan >= TimeSpan.Zero && timeSpan < TimeSpan.FromDays(1) =>
                    Result<object?>.Success(TimeOnly.FromTimeSpan(timeSpan)),
                _ => ConversionFailures.Unsupported(value.GetType(), targetType)
            };
        }

        if (targetType == typeof(TimeSpan))
        {
            return value is TimeOnly timeOnly
                ? Result<object?>.Success(timeOnly.ToTimeSpan())
                : ConversionFailures.Unsupported(value.GetType(), targetType);
        }

        throw new ArgumentException("Unsupported temporal target type.", nameof(targetType));
    }

    internal static string Format(object value) => value switch
    {
        DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
        DateOnly dateOnly => dateOnly.ToString("O", CultureInfo.InvariantCulture),
        TimeOnly timeOnly => timeOnly.ToString("O", CultureInfo.InvariantCulture),
        TimeSpan timeSpan => timeSpan.ToString("c", CultureInfo.InvariantCulture),
        _ => throw new ArgumentException("Value is not a supported temporal type.", nameof(value))
    };

    private static Result<object?> ParseText(string text, Type targetType, ConversionContext context)
    {
        if (context.TemporalText == TemporalTextPolicy.RoundTrip)
        {
            return ParseRoundTrip(text, targetType);
        }

        return ParseCultureAware(text, targetType, context.Culture);
    }

    private static Result<object?> ParseRoundTrip(string text, Type targetType)
    {
        if (targetType == typeof(DateTime) &&
            DateTime.TryParseExact(text, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind,
                out var dateTime) &&
            dateTime.Kind != DateTimeKind.Local)
        {
            return Result<object?>.Success(dateTime);
        }

        if (targetType == typeof(DateTimeOffset) &&
            DateTimeOffset.TryParseExact(text, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind,
                out var dateTimeOffset))
        {
            return Result<object?>.Success(dateTimeOffset);
        }

        if (targetType == typeof(DateOnly) &&
            DateOnly.TryParseExact(text, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
        {
            return Result<object?>.Success(dateOnly);
        }

        if (targetType == typeof(TimeOnly) &&
            TimeOnly.TryParseExact(text, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeOnly))
        {
            return Result<object?>.Success(timeOnly);
        }

        if (targetType == typeof(TimeSpan) &&
            TimeSpan.TryParseExact(text, "c", CultureInfo.InvariantCulture, out var timeSpan))
        {
            return Result<object?>.Success(timeSpan);
        }

        return ConversionFailures.InvalidFormat(targetType);
    }

    private static Result<object?> ParseCultureAware(string text, Type targetType, CultureInfo culture)
    {
        if (targetType == typeof(DateTime) &&
            DateTime.TryParse(text, culture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind,
                out var dateTime) &&
            dateTime.Kind != DateTimeKind.Local)
        {
            return Result<object?>.Success(dateTime);
        }

        if (targetType == typeof(DateTimeOffset) &&
            DateTimeOffset.TryParse(text, culture, DateTimeStyles.AllowWhiteSpaces, out var dateTimeOffset))
        {
            return Result<object?>.Success(dateTimeOffset);
        }

        if (targetType == typeof(DateOnly) && DateOnly.TryParse(text, culture, DateTimeStyles.AllowWhiteSpaces,
                out var dateOnly))
        {
            return Result<object?>.Success(dateOnly);
        }

        if (targetType == typeof(TimeOnly) && TimeOnly.TryParse(text, culture, DateTimeStyles.AllowWhiteSpaces,
                out var timeOnly))
        {
            return Result<object?>.Success(timeOnly);
        }

        if (targetType == typeof(TimeSpan) && TimeSpan.TryParse(text, culture, out var timeSpan))
        {
            return Result<object?>.Success(timeSpan);
        }

        return ConversionFailures.InvalidFormat(targetType);
    }
}
