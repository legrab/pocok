// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Conversion;

internal static class ConversionFailures
{
    internal static ConversionResult<object?> Null(Type targetType, string path = "$")
    {
        return Failure(
            ConversionErrorCodes.NullNotAllowed,
            $"Null is not allowed for target type {DisplayName(targetType)}.", path);
    }

    internal static ConversionResult<object?> InvalidFormat(Type targetType, string path = "$")
    {
        return Failure(
            ConversionErrorCodes.InvalidFormat,
            $"The source value has an invalid format for target type {DisplayName(targetType)}.", path);
    }

    internal static ConversionResult<object?> Overflow(Type targetType, string path = "$")
    {
        return Failure(
            ConversionErrorCodes.Overflow,
            $"The source value is outside the range of target type {DisplayName(targetType)}.", path);
    }

    internal static ConversionResult<object?> Lossy(Type targetType, string path = "$")
    {
        return Failure(
            ConversionErrorCodes.Lossy,
            $"The source value cannot be converted to {DisplayName(targetType)} without numeric loss.", path);
    }

    internal static ConversionResult<object?> InvalidEnum(Type targetType, string path = "$")
    {
        return Failure(
            ConversionErrorCodes.InvalidEnum,
            $"The source value is not valid for enum type {DisplayName(targetType)}.", path);
    }

    internal static ConversionResult<object?> Unsupported(Type sourceType, Type targetType, string path = "$")
    {
        return Failure(
            ConversionErrorCodes.Unsupported,
            $"Conversion from {DisplayName(sourceType)} to {DisplayName(targetType)} is not supported.", path);
    }

    internal static ConversionResult<object?> Collection(string message, Exception? exception = null, string path = "$")
    {
        return Failure(ConversionErrorCodes.CollectionItem, message, path, exception);
    }

    internal static ConversionResult<object?> DuplicateKey(string path)
    {
        return Failure(
            ConversionErrorCodes.DuplicateKey,
            "A converted dictionary key duplicates an earlier key.", path);
    }

    internal static ConversionResult<object?> ResourceLimit(string message, string path)
    {
        return Failure(ConversionErrorCodes.ResourceLimit, message, path);
    }

    private static ConversionResult<object?> Failure(
        string code,
        string message,
        string path,
        Exception? exception = null)
    {
        return ConversionResult<object?>.Failure(exception is null
            ? new ConversionFailure(code, message, path)
            : ConversionFailure.FromException(code, message, exception, path));
    }

    private static string DisplayName(Type type)
    {
        return type.FullName ?? type.Name;
    }
}
