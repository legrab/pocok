// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Diagnostics.CodeAnalysis;

namespace Pocok.Conversion;

/// <summary>
///     Gives a custom strategy immutable policy access and a bounded continuation for nested values.
/// </summary>
public sealed class ConversionStrategyContext
{
    private readonly Func<object?, Type, string, ConversionResult<object?>> _continuation;

    internal ConversionStrategyContext(
        ConversionContext policies,
        string path,
        Func<object?, Type, string, ConversionResult<object?>> continuation)
    {
        Policies = policies;
        Path = path;
        _continuation = continuation;
    }

    /// <summary>Gets the active immutable conversion policies.</summary>
    public ConversionContext Policies { get; }

    /// <summary>Gets the path of the value supplied to the strategy.</summary>
    public string Path { get; }

    /// <summary>
    ///     Converts a nested value while preserving converter limits and strategy ordering.
    /// </summary>
    /// <param name="value">The nested source value.</param>
    /// <param name="targetType">The nested target type.</param>
    /// <param name="pathSegment">A caller-owned path segment such as <c>.value</c> or <c>[0]</c>.</param>
    [RequiresUnreferencedCode(ConversionTrimming.IncompatibleMessage)]
    public ConversionResult<object?> ConvertNested(object? value, Type targetType, string pathSegment)
    {
        ArgumentNullException.ThrowIfNull(targetType);
        ArgumentException.ThrowIfNullOrWhiteSpace(pathSegment);
        if (pathSegment[0] is not '.' and not '[')
            throw new ArgumentException("A nested path segment must start with '.' or '['.", nameof(pathSegment));

        return _continuation(value, targetType, pathSegment);
    }
}
