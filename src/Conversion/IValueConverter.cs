// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors


namespace Pocok.Conversion;

/// <summary>
///     Converts runtime values according to an explicit immutable policy context.
/// </summary>
public interface IValueConverter
{
    /// <summary>
    ///     Converts a value to <typeparamref name="TTarget" />.
    /// </summary>
    public ConversionResult<TTarget> Convert<TTarget>(object? value, ConversionContext? context = null);

    /// <summary>
    ///     Converts a value to a type selected at runtime.
    /// </summary>
    public ConversionResult<object?> Convert(object? value, Type targetType, ConversionContext? context = null);
}
