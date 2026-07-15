// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Diagnostics.CodeAnalysis;

namespace Pocok.Conversion;

/// <summary>
///     Represents either a converted value or an expected conversion failure.
/// </summary>
public sealed record ConversionResult<T>
{
    private readonly T? _value;

    private ConversionResult(bool isSuccess, T? value, ConversionFailure? error)
    {
        IsSuccess = isSuccess;
        _value = value;
        Error = error;
    }

    /// <summary>Gets whether conversion succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets whether conversion failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Gets the failure, or null on success.</summary>
    public ConversionFailure? Error { get; }

    /// <summary>Gets the converted value and throws when conversion failed.</summary>
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("A failed conversion has no value.");

    /// <summary>Creates a successful result.</summary>
    public static ConversionResult<T> Success(T value)
    {
        return new ConversionResult<T>(true, value, null);
    }

    /// <summary>Creates a failed result.</summary>
    public static ConversionResult<T> Failure(ConversionFailure error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new ConversionResult<T>(false, default, error);
    }

    /// <summary>Attempts to obtain the converted value without throwing.</summary>
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = _value;
        return IsSuccess;
    }
}
