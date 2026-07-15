// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors


using System.Diagnostics.CodeAnalysis;

namespace Pocok.Conversion;

/// <summary>
///     Converts runtime values according to an explicit immutable policy context.
/// </summary>
public interface IValueConverter
{
    /// <summary>
    ///     Converts a value to <typeparamref name="TTarget" />.
    /// </summary>
    [RequiresUnreferencedCode(ConversionTrimming.IncompatibleMessage)]
    public ConversionResult<TTarget> Convert<TTarget>(object? value, ConversionContext? context = null);

    /// <summary>
    ///     Converts a value to a type selected at runtime.
    /// </summary>
    [RequiresUnreferencedCode(ConversionTrimming.IncompatibleMessage)]
    public ConversionResult<object?> Convert(object? value, Type targetType, ConversionContext? context = null);
}
