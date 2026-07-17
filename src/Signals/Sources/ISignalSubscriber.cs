// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Signals.Runtime;

namespace Pocok.Signals.Sources;

/// <summary>Streams ordered raw samples for one signal address.</summary>
public interface ISignalSubscriber : ISignalSource
{
    /// <summary>Subscribes until cancellation, completion, or owner disposal.</summary>
    public IAsyncEnumerable<SignalSample<object?>> SubscribeAsync(
        SignalAddress address,
        CancellationToken cancellationToken = default);
}
