// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Primitives;

namespace Pocok.Hosting;

/// <summary>
/// Exposes observable readiness independently from lifecycle scheduling.
/// </summary>
public interface IReadinessSignal
{
    /// <summary>
    /// Gets the current lifecycle state.
    /// </summary>
    public ReadinessState State { get; }

    /// <summary>
    /// Gets the current failure, or null when the lifecycle has not failed.
    /// </summary>
    public Error? Failure { get; }

    /// <summary>
    /// Waits for the current or next startup attempt to become ready.
    /// </summary>
    public Task WaitUntilReadyAsync(CancellationToken cancellationToken = default);
}
