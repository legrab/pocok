// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Signals;

/// <summary>
/// Configures bounded buffering, reconnect, and optional staleness behavior.
/// </summary>
public sealed record SignalRuntimeOptions
{
    /// <summary>
    /// Initializes runtime options.
    /// </summary>
    public SignalRuntimeOptions(
        int subscriberCapacity = 64,
        TimeSpan? reconnectDelay = null,
        TimeSpan? staleAfter = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(subscriberCapacity);

        TimeSpan effectiveReconnectDelay = reconnectDelay ?? TimeSpan.FromSeconds(1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            effectiveReconnectDelay,
            TimeSpan.Zero,
            nameof(reconnectDelay));

        if (staleAfter is { } effectiveStaleAfter)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
                effectiveStaleAfter,
                TimeSpan.Zero,
                nameof(staleAfter));
        }

        SubscriberCapacity = subscriberCapacity;
        ReconnectDelay = effectiveReconnectDelay;
        StaleAfter = staleAfter;
    }

    /// <summary>
    /// Gets the maximum pending samples retained for each connection.
    /// </summary>
    public int SubscriberCapacity { get; }

    /// <summary>
    /// Gets the delay before resubscribing after source completion or failure.
    /// </summary>
    public TimeSpan ReconnectDelay { get; }

    /// <summary>
    /// Gets the optional quiet period after which a good sample becomes stale.
    /// </summary>
    public TimeSpan? StaleAfter { get; }
}
