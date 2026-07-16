// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Signals;

/// <summary>Reads point-in-time raw signal samples.</summary>
public interface ISignalReader : ISignalSource
{
    /// <summary>Reads one sample without applying a typed conversion policy.</summary>
    public ValueTask<SignalResult<SignalSample<object?>>> ReadAsync(
        SignalAddress address,
        CancellationToken cancellationToken = default);
}
