// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Readiness;

/// <summary>
///     Captures one atomic observation of readiness state, cycle sequence, and failure.
/// </summary>
public readonly record struct ReadinessSnapshot(
    ReadinessState State,
    long Sequence,
    ReadinessFailure? Failure)
{
    /// <summary>Gets whether the observed cycle is ready.</summary>
    public bool IsReady => State == ReadinessState.Ready;
}
