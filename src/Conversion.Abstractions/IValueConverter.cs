// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Primitives;

namespace Pocok.Conversion;

/// <summary>
/// Converts runtime values according to an explicit immutable policy context.
/// </summary>
public interface IValueConverter
{
    /// <summary>
    /// Converts a value to <typeparamref name="TTarget"/>.
    /// </summary>
    public Result<TTarget> Convert<TTarget>(object? value, ConversionContext? context = null);

    /// <summary>
    /// Converts a value to a type selected at runtime.
    /// </summary>
    public Result<object?> Convert(object? value, Type targetType, ConversionContext? context = null);
}
