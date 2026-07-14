// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Diagnostics.CodeAnalysis;

namespace Pocok.Primitives;

/// <summary>
/// Represents either a successful value or an expected operational failure.
/// </summary>
/// <typeparam name="T">The successful value type.</typeparam>
public sealed record Result<T>
{
    private readonly T? _value;

    private Result(bool isSuccess, T? value, Error? error)
    {
        IsSuccess = isSuccess;
        _value = value;
        Error = error;
    }

    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the failure information, or null when the operation succeeded.
    /// </summary>
    public Error? Error { get; }

    /// <summary>
    /// Gets the successful value, including a legitimate null, and throws when the result failed.
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result has no value.");

    /// <summary>
    /// Creates a successful result. Null is valid when <typeparamref name="T"/> permits it.
    /// </summary>
    public static Result<T> Success(T value) => new(true, value, null);

    /// <summary>
    /// Creates a failed result with no partial value.
    /// </summary>
    public static Result<T> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<T>(false, default, error);
    }

    /// <summary>
    /// Attempts to get the successful value without throwing.
    /// </summary>
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = _value;
        return IsSuccess;
    }

    /// <summary>
    /// Produces a value by explicitly handling both result states.
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> success, Func<Error, TResult> failure)
    {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return IsSuccess ? success(_value!) : failure(Error!);
    }

    /// <summary>
    /// Transforms a successful value while preserving a failure unchanged.
    /// </summary>
    public Result<TNext> Map<TNext>(Func<T, TNext> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return IsSuccess ? Result<TNext>.Success(selector(_value!)) : Result<TNext>.Failure(Error!);
    }

    /// <summary>
    /// Chains a successful value into another result while preserving a failure unchanged.
    /// </summary>
    public Result<TNext> Bind<TNext>(Func<T, Result<TNext>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (IsFailure)
        {
            return Result<TNext>.Failure(Error!);
        }

        return selector(_value!) ?? throw new InvalidOperationException("A result selector returned null.");
    }

    /// <summary>
    /// Discards a successful value while preserving the result state.
    /// </summary>
    public Result ToResult() => IsSuccess ? Result.Success() : Result.Failure(Error!);
}
