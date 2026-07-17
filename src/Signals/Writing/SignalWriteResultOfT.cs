// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Signals.Runtime;

namespace Pocok.Signals.Writing;

/// <summary>
///     Describes typed evidence returned by a successful runtime write.
/// </summary>
public sealed record SignalWriteResult<T>
{
    /// <summary>
    ///     Initializes validated typed write evidence.
    /// </summary>
    public SignalWriteResult(SignalWriteConsistency consistency, SignalSample<T>? sample)
    {
        if (!Enum.IsDefined(consistency)) throw new ArgumentOutOfRangeException(nameof(consistency));

        if (consistency is not SignalWriteConsistency.Acknowledged && sample is null)
            throw new ArgumentException("Confirmed writes require the resulting sample.", nameof(sample));

        Consistency = consistency;
        Sample = sample;
    }

    /// <summary>
    ///     Gets the consistency evidence achieved by the source.
    /// </summary>
    public SignalWriteConsistency Consistency { get; }

    /// <summary>
    ///     Gets the typed resulting sample, or null for acknowledgement-only evidence.
    /// </summary>
    public SignalSample<T>? Sample { get; }
}
