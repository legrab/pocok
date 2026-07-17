// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Diagnostics.CodeAnalysis;

namespace Pocok.Conversion.Internal;

internal sealed class ConversionSession
{
    private readonly ValueConverter _converter;
    private int _remainingItems;

    internal ConversionSession(ValueConverter converter, ConversionContext context)
    {
        _converter = converter;
        Context = context;
        _remainingItems = context.MaximumCollectionItems;
    }

    internal ConversionContext Context { get; }

    [RequiresUnreferencedCode(ConversionTrimming.IncompatibleMessage)]
    internal ConversionResult<object?> Convert(object? value, Type targetType, string path = "$", int depth = 0)
    {
        if (depth > Context.MaximumDepth)
            return ConversionFailures.ResourceLimit(
                $"Conversion exceeded the maximum depth of {Context.MaximumDepth}.", path);

        ConversionResult<object?> result = _converter.ConvertCore(value, targetType, this, path, depth);
        if (result.IsFailure && result.Error!.Path == "$")
            return ConversionResult<object?>.Failure(result.Error.AtPath(path));

        return result;
    }

    [RequiresUnreferencedCode(ConversionTrimming.IncompatibleMessage)]
    internal ConversionResult<object?> ConvertNested(object? value, Type targetType, string path, int depth)
    {
        return Convert(value, targetType, path, depth + 1);
    }

    internal ConversionResult<object?> ConsumeItem(string path)
    {
        if (_remainingItems <= 0)
            return ConversionFailures.ResourceLimit(
                $"Conversion exceeded the maximum collection item budget of {Context.MaximumCollectionItems}.", path);

        _remainingItems--;
        return ConversionResult<object?>.Success(null);
    }
}
