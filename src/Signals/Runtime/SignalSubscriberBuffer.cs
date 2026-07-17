// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Pocok.Signals.Runtime;

internal sealed class SignalSubscriberBuffer(int capacity)
{
    private readonly Channel<bool> _available = Channel.CreateUnbounded<bool>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });

    private readonly Queue<SignalSample<object?>> _samples = [];
    private readonly Lock _sync = new();
    private bool _completed;
    private long _droppedSampleCount;

    internal long DroppedSampleCount => Interlocked.Read(ref _droppedSampleCount);

    internal void Enqueue(SignalSample<object?> sample)
    {
        var signal = false;
        lock (_sync)
        {
            if (_completed) return;

            signal = _samples.Count == 0;
            if (_samples.Count == capacity)
            {
                _samples.Dequeue();
                Interlocked.Increment(ref _droppedSampleCount);
            }

            _samples.Enqueue(sample);
        }

        if (signal) _available.Writer.TryWrite(true);
    }

    internal void Complete()
    {
        var signal = false;
        lock (_sync)
        {
            if (_completed) return;

            _completed = true;
            signal = _samples.Count == 0;
        }

        if (signal) _available.Writer.TryComplete();
    }

    internal async IAsyncEnumerable<SignalSample<object?>> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SignalSample<object?>? sample = null;
            var completed = false;
            lock (_sync)
            {
                if (_samples.Count > 0)
                    sample = _samples.Dequeue();
                else
                    completed = _completed;
            }

            if (sample is not null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return sample;
                continue;
            }

            if (completed) yield break;

            await _available.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false);
            while (_available.Reader.TryRead(out _))
            {
            }
        }
    }
}
