// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Conversion;

/// <summary>
///     Describes an expected conversion failure using a stable code, safe message, and optional value path.
/// </summary>
public sealed record ConversionFailure
{
    /// <summary>
    ///     Initializes a conversion failure.
    /// </summary>
    public ConversionFailure(string code, string message, string path = "$", Exception? exception = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (exception is OperationCanceledException)
            throw new ArgumentException("Cancellation must propagate instead of becoming a conversion failure.",
                nameof(exception));

        Code = code;
        Message = message;
        Path = path;
        Exception = exception;
    }

    /// <summary>Gets the stable machine-readable failure code.</summary>
    public string Code { get; }

    /// <summary>Gets a safe caller-facing message.</summary>
    public string Message { get; }

    /// <summary>Gets the source value path, rooted at <c>$</c>.</summary>
    public string Path { get; init; }

    /// <summary>Gets optional diagnostic context.</summary>
    public Exception? Exception { get; }

    /// <summary>Creates a failure with diagnostic exception context.</summary>
    public static ConversionFailure FromException(string code, string message, Exception exception, string path = "$")
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new ConversionFailure(code, message, path, exception);
    }

    /// <summary>Returns a copy associated with a more specific value path.</summary>
    public ConversionFailure AtPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return this with { Path = path };
    }
}
