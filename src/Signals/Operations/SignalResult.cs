// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Signals.Operations;

/// <summary>Represents either signal operation evidence or an expected failure.</summary>
public sealed class SignalResult<T>
{
    internal SignalResult(T? value, SignalFailure? failure)
    {
        Value = value;
        Failure = failure;
    }

    /// <summary>Gets whether the operation succeeded.</summary>
    public bool IsSuccess => Failure is null;

    /// <summary>Gets the operation value when successful.</summary>
    public T? Value { get; }

    /// <summary>Gets the expected failure when unsuccessful.</summary>
    public SignalFailure? Failure { get; }
}

/// <summary>Creates signal operation results without static members on a generic type.</summary>
public static class SignalResult
{
    /// <summary>Creates a successful result.</summary>
    public static SignalResult<T> Success<T>(T? value = default)
    {
        return new SignalResult<T>(value, null);
    }

    /// <summary>Creates a failed result.</summary>
    public static SignalResult<T> Failed<T>(SignalFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return new SignalResult<T>(default, failure);
    }
}
