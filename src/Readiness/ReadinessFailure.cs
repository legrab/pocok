// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Readiness;

/// <summary>
///     Describes an expected lifecycle failure without coupling readiness to a generic result package.
/// </summary>
public sealed record ReadinessFailure
{
    /// <summary>Initializes a readiness failure.</summary>
    public ReadinessFailure(string code, string message, Exception? exception = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        if (exception is OperationCanceledException)
            throw new ArgumentException("Cancellation must propagate instead of becoming a readiness failure.",
                nameof(exception));

        Code = code;
        Message = message;
        Exception = exception;
    }

    /// <summary>Gets the stable machine-readable code.</summary>
    public string Code { get; }

    /// <summary>Gets the safe caller-facing message.</summary>
    public string Message { get; }

    /// <summary>Gets optional diagnostic context.</summary>
    public Exception? Exception { get; }

    /// <summary>Creates a failure with diagnostic exception context.</summary>
    public static ReadinessFailure FromException(string code, string message, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new ReadinessFailure(code, message, exception);
    }
}
