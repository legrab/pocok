// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Readiness;

/// <summary>
///     Identifies one startup attempt and prevents stale completions from affecting a later attempt.
/// </summary>
public sealed class ReadinessCycle
{
    internal ReadinessCycle(ReadinessSource owner, long sequence)
    {
        Owner = owner;
        Sequence = sequence;
    }

    internal ReadinessSource Owner { get; }

    /// <summary>
    ///     Gets the monotonically increasing startup sequence within the source.
    /// </summary>
    public long Sequence { get; }
}
