// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Signals.Operations;

/// <summary>Describes an expected signal-source or signal-operation failure.</summary>
public sealed record SignalFailure
{
    /// <summary>Creates a structured signal failure.</summary>
    public SignalFailure(string code, string message, Exception? exception = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Code = code;
        Message = message;
        Exception = exception;
    }

    /// <summary>Gets the stable failure code.</summary>
    public string Code { get; }

    /// <summary>Gets the safe failure message.</summary>
    public string Message { get; }

    /// <summary>Gets the underlying exception when available to the host.</summary>
    public Exception? Exception { get; }
}
