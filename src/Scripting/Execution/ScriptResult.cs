// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors


namespace Pocok.Scripting.Execution;

/// <summary>Represents either a script result or an expected script failure.</summary>
public sealed class ScriptResult<T>
{
    internal ScriptResult(T? value, ScriptFailure? failure) { Value = value; Failure = failure; }
    /// <summary>Gets whether execution succeeded.</summary>
    public bool IsSuccess => Failure is null;
    /// <summary>Gets the result value.</summary>
    public T? Value { get; }
    /// <summary>Gets the expected failure.</summary>
    public ScriptFailure? Failure { get; }
}

/// <summary>Creates script results.</summary>
public static class ScriptResult
{
    /// <summary>Creates a successful result.</summary>
    public static ScriptResult<T> Success<T>(T? value = default) => new(value, null);
    /// <summary>Creates a failed result.</summary>
    public static ScriptResult<T> Failed<T>(ScriptFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return new(default, failure);
    }
}
