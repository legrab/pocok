// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Primitives;

namespace Pocok.Hosting;

/// <summary>
/// Reports a structured lifecycle failure to readiness waiters.
/// </summary>
public sealed class ReadinessFailedException : Exception
{
    /// <summary>
    /// Initializes an exception for the supplied structured failure.
    /// </summary>
    public ReadinessFailedException(Error error)
        : base(GetMessage(error), error.Exception)
    {
        Error = error;
    }

    /// <summary>
    /// Gets the structured lifecycle failure.
    /// </summary>
    public Error Error { get; }

    private static string GetMessage(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return error.Message;
    }
}
