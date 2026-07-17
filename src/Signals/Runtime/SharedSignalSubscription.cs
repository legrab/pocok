// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors


using Pocok.Signals.Operations;
using Pocok.Signals.Sources;

namespace Pocok.Signals.Runtime;

internal sealed class SharedSignalSubscription
{
    private readonly SignalAddress _address;
    private readonly ISignalSubscriber _source;
    private readonly CancellationTokenSource _stop;
    private readonly HashSet<SignalSubscriberBuffer> _subscribers = [];
    private readonly Lock _sync = new();
    private readonly TimeProvider _timeProvider;
    private SignalSample<object?>? _lastSample;
    private Task? _runTask;
    private long _sequence;
    private long _staleGeneration;
    private ITimer? _staleTimer;
    private bool _stopping;

    internal SharedSignalSubscription(
        SignalAddress address,
        ISignalSubscriber source,
        SignalRuntimeOptions options,
        TimeProvider timeProvider,
        CancellationToken runtimeCancellation)
    {
        _address = address;
        _source = source;
        Options = options;
        _timeProvider = timeProvider;
        _stop = CancellationTokenSource.CreateLinkedTokenSource(runtimeCancellation);
    }

    internal SignalRuntimeOptions Options { get; }

    internal void AddSubscriber(SignalSubscriberBuffer subscriber)
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_stopping, this);

            _subscribers.Add(subscriber);
            if (_lastSample is not null) subscriber.Enqueue(_lastSample);
        }
    }

    internal void RemoveSubscriber(SignalSubscriberBuffer subscriber)
    {
        lock (_sync)
        {
            _subscribers.Remove(subscriber);
            subscriber.Complete();
        }
    }

    internal void Start()
    {
        lock (_sync)
        {
            if (_runTask is null && !_stopping) _runTask = Task.Run(RunAsync);
        }
    }

    internal SignalSample<object?>? Publish(SignalSample<object?> sample)
    {
        lock (_sync)
        {
            return PublishLocked(sample);
        }
    }

    internal async ValueTask StopAsync()
    {
        Task? runTask;
        var stop = false;
        lock (_sync)
        {
            if (!_stopping)
            {
                _stopping = true;
                stop = true;
                _staleGeneration++;
                _staleTimer?.Dispose();
                _staleTimer = null;

                foreach (SignalSubscriberBuffer subscriber in _subscribers) subscriber.Complete();

                _subscribers.Clear();
            }

            runTask = _runTask;
        }

        if (stop) await _stop.CancelAsync().ConfigureAwait(false);

        if (runTask is not null)
            try
            {
                await runTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested)
            {
            }

        if (stop) _stop.Dispose();
    }

    private async Task RunAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            var completed = false;
            try
            {
                await foreach (SignalSample<object?> sample in _source.SubscribeAsync(_address, _stop.Token)
                                   .WithCancellation(_stop.Token)
                                   .ConfigureAwait(false))
                    Publish(sample);

                completed = true;
            }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested)
            {
                break;
            }
            catch (OperationCanceledException)
            {
                PublishFailure(new SignalFailure(
                    SignalRuntimeErrorCodes.SubscriptionFailed,
                    "The underlying signal subscription was canceled unexpectedly."));
            }
            catch (Exception exception)
            {
                PublishFailure(new SignalFailure(
                    SignalRuntimeErrorCodes.SubscriptionFailed,
                    "The underlying signal subscription failed.",
                    exception));
            }

            if (completed) PublishDisconnected();

            try
            {
                await Task.Delay(Options.ReconnectDelay, _timeProvider, _stop.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private SignalSample<object?>? PublishLocked(SignalSample<object?> sample)
    {
        if (_stopping) return null;

        if (_lastSample is not null && HasSamePayload(_lastSample, sample))
        {
            ScheduleStalenessLocked(sample.Quality);
            return _lastSample;
        }

        var normalized = new SignalSample<object?>(
            sample.Value,
            sample.HasValue,
            sample.SourceTimestamp,
            _timeProvider.GetUtcNow(),
            sample.Quality,
            checked(++_sequence),
            sample.Failure);

        _lastSample = normalized;
        foreach (SignalSubscriberBuffer subscriber in _subscribers) subscriber.Enqueue(normalized);

        ScheduleStalenessLocked(normalized.Quality);
        return normalized;
    }

    private void PublishDisconnected()
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        Publish(new SignalSample<object?>(
            null,
            false,
            null,
            now,
            SignalQuality.Disconnected,
            1));
    }

    private void PublishFailure(SignalFailure failure)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        Publish(new SignalSample<object?>(
            null,
            false,
            null,
            now,
            SignalQuality.Failed,
            1,
            failure));
    }

    private void ScheduleStalenessLocked(SignalQuality quality)
    {
        _staleGeneration++;
        _staleTimer?.Dispose();
        _staleTimer = null;

        if (quality != SignalQuality.Good || Options.StaleAfter is not { } staleAfter) return;

        var generation = _staleGeneration;
        _staleTimer = _timeProvider.CreateTimer(
            _ => MarkStale(generation),
            null,
            staleAfter,
            Timeout.InfiniteTimeSpan);
    }

    private void MarkStale(long generation)
    {
        lock (_sync)
        {
            if (_stopping || generation != _staleGeneration ||
                _lastSample is not { Quality: SignalQuality.Good } last) return;

            _staleTimer?.Dispose();
            _staleTimer = null;
            _staleGeneration++;

            var stale = new SignalSample<object?>(
                last.Value,
                true,
                last.SourceTimestamp,
                _timeProvider.GetUtcNow(),
                SignalQuality.Stale,
                checked(++_sequence));
            _lastSample = stale;

            foreach (SignalSubscriberBuffer subscriber in _subscribers) subscriber.Enqueue(stale);
        }
    }

    private static bool HasSamePayload(SignalSample<object?> left, SignalSample<object?> right)
    {
        return left.HasValue == right.HasValue &&
               Equals(left.Value, right.Value) &&
               left.SourceTimestamp == right.SourceTimestamp &&
               left.Quality == right.Quality &&
               Equals(left.Failure, right.Failure);
    }
}
