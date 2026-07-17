// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Signals.Runtime;

namespace Pocok.Signals.Writing;

/// <summary>Describes evidence returned by a successful write operation.</summary>
public sealed record SignalWriteResult
{
    /// <summary>Creates a validated write result.</summary>
    public SignalWriteResult(SignalWriteConsistency consistency, SignalSample<object?>? sample)
    {
        if (!Enum.IsDefined(consistency))
            throw new ArgumentOutOfRangeException(nameof(consistency));
        if (consistency is not SignalWriteConsistency.Acknowledged && sample is null)
            throw new ArgumentException("Confirmed writes require the resulting sample.", nameof(sample));
        Consistency = consistency;
        Sample = sample;
    }

    /// <summary>Gets the consistency evidence achieved by the source.</summary>
    public SignalWriteConsistency Consistency { get; }

    /// <summary>Gets the resulting sample, or null for acknowledgement-only evidence.</summary>
    public SignalSample<object?>? Sample { get; }
}
