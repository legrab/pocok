// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Readiness;

/// <summary>
///     Reports a structured lifecycle failure to readiness waiters.
/// </summary>
/// <remarks>Initializes an exception for the supplied structured failure.</remarks>
public sealed class ReadinessFailedException(ReadinessFailure failure)
    : Exception(GetMessage(failure), failure.Exception)
{
    /// <summary>Gets the structured lifecycle failure.</summary>
    public ReadinessFailure Failure { get; } = failure;

    private static string GetMessage(ReadinessFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return failure.Message;
    }
}
