// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Primitives;

namespace Pocok.Conversion;

internal static class ConversionFailures
{
    internal static Result<object?> Null(Type targetType) => Failure(
        ConversionErrorCodes.NullNotAllowed,
        $"Null is not allowed for target type {DisplayName(targetType)}.");

    internal static Result<object?> InvalidFormat(Type targetType) => Failure(
        ConversionErrorCodes.InvalidFormat,
        $"The source value has an invalid format for target type {DisplayName(targetType)}.");

    internal static Result<object?> Overflow(Type targetType) => Failure(
        ConversionErrorCodes.Overflow,
        $"The source value is outside the range of target type {DisplayName(targetType)}.");

    internal static Result<object?> Lossy(Type targetType) => Failure(
        ConversionErrorCodes.Lossy,
        $"The source value cannot be converted to {DisplayName(targetType)} without numeric loss.");

    internal static Result<object?> InvalidEnum(Type targetType) => Failure(
        ConversionErrorCodes.InvalidEnum,
        $"The source value is not valid for enum type {DisplayName(targetType)}.");

    internal static Result<object?> Unsupported(Type sourceType, Type targetType) => Failure(
        ConversionErrorCodes.Unsupported,
        $"Conversion from {DisplayName(sourceType)} to {DisplayName(targetType)} is not supported.");

    internal static Result<object?> Collection(string message, Exception? exception = null) =>
        Failure(ConversionErrorCodes.CollectionItem, message, exception);

    private static Result<object?> Failure(string code, string message, Exception? exception = null) =>
        Result<object?>.Failure(exception is null
            ? new Error(code, message)
            : Error.FromException(code, message, exception));

    private static string DisplayName(Type type) => type.FullName ?? type.Name;
}
