// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Signals;

/// <summary>Writes raw values and returns source-confirmed evidence when available.</summary>
public interface ISignalWriter : ISignalSource
{
    /// <summary>Writes one value using explicit consistency semantics.</summary>
    public ValueTask<SignalResult<SignalWriteResult>> WriteAsync(
        SignalAddress address,
        object? value,
        SignalWriteConsistency consistency,
        CancellationToken cancellationToken = default);
}
