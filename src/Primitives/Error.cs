// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Primitives;

/// <summary>
/// Describes an expected operational failure using a stable code and a safe message.
/// </summary>
public sealed record Error
{
    /// <summary>
    /// Initializes a new error.
    /// </summary>
    /// <param name="code">A stable, machine-readable error code.</param>
    /// <param name="message">A safe message suitable for callers.</param>
    /// <param name="exception">Optional diagnostic context that must not represent cancellation.</param>
    public Error(string code, string message, Exception? exception = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (exception is OperationCanceledException)
        {
            throw new ArgumentException("Cancellation must propagate instead of becoming an error.", nameof(exception));
        }

        Code = code;
        Message = message;
        Exception = exception;
    }

    /// <summary>
    /// Gets the stable, machine-readable error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the safe message suitable for callers.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets optional diagnostic context. Callers should not display or serialize it by default.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Creates an error with attached exception diagnostics and an independently supplied safe message.
    /// </summary>
    public static Error FromException(string code, string message, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new Error(code, message, exception);
    }
}
