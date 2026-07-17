// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Conversion;

namespace Pocok.Signals;

internal sealed class SharedSignalEntry
{
    private readonly Lock _sync = new();
    private readonly ISignalSource _source;
    private readonly SharedSignalSubscription _subscription;
    private readonly IValueConverter _converter;
    private readonly Action<SharedSignalEntry> _onEmpty;
    private readonly TaskCompletionSource _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _leaseCount;
    private bool _closing;

    internal SharedSignalEntry(
        SignalAddress address,
        ISignalSource source,
        SignalRuntimeOptions options,
        TimeProvider timeProvider,
        IValueConverter converter,
        Action<SharedSignalEntry> onEmpty,
        CancellationToken runtimeCancellation)
    {
        Address = address;
        _source = source;
        _converter = converter;
        _onEmpty = onEmpty;
        _subscription = new SharedSignalSubscription(
            address,
            (ISignalSubscriber)source,
            options,
            timeProvider,
            runtimeCancellation);
    }

    internal SignalAddress Address { get; }

    internal bool TryAcquire<T>(out SignalConnection<T>? connection)
    {
        SignalSubscriberBuffer? buffer = null;
        var start = false;
        lock (_sync)
        {
            if (!_closing)
            {
                buffer = new SignalSubscriberBuffer(_subscription.Options.SubscriberCapacity);
                _subscription.AddSubscriber(buffer);
                _leaseCount++;
                start = _leaseCount == 1;
            }
        }

        if (buffer is null)
        {
            connection = null;
            return false;
        }

        if (start)
        {
            _subscription.Start();
        }

        connection = new SignalConnection<T>(this, buffer, _converter);
        return true;
    }

    internal async ValueTask<SignalResult<SignalWriteResult>> WriteAsync(
        object? value,
        SignalWriteConsistency consistency,
        CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_closing, this);
        }

        if (!_source.Capabilities.HasFlag(SignalSourceCapabilities.Write) || _source is not ISignalWriter writer)
        {
            return SignalResult.Failed<SignalWriteResult>(new SignalFailure(
                SignalRuntimeErrorCodes.WriteUnsupported,
                "The signal source does not support writes."));
        }

        SignalResult<SignalWriteResult> result;
        try
        {
            result = await writer.WriteAsync(Address, value, consistency, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return SignalResult.Failed<SignalWriteResult>(new SignalFailure(
                SignalRuntimeErrorCodes.WriteFailed,
                "The signal source write failed.",
                exception));
        }

        if (!result.IsSuccess)
        {
            return result;
        }

        SignalWriteResult evidence = result.Value!;
        if (evidence.Consistency < consistency)
        {
            return SignalResult.Failed<SignalWriteResult>(new SignalFailure(
                SignalRuntimeErrorCodes.WriteConsistencyNotMet,
                "The signal source returned weaker write evidence than requested."));
        }

        if (evidence.Sample is null)
        {
            return result;
        }

        SignalSample<object?>? normalized = _subscription.Publish(evidence.Sample);
        return normalized is null
            ? SignalResult.Failed<SignalWriteResult>(new SignalFailure(
                SignalRuntimeErrorCodes.WriteFailed,
                "The signal subscription closed before write evidence could be published."))
            : SignalResult.Success(new SignalWriteResult(evidence.Consistency, normalized));
    }

    internal async ValueTask<SignalResult<SignalSample<object?>>> ReadAsync(
        CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_closing, this);
        }

        if (!_source.Capabilities.HasFlag(SignalSourceCapabilities.Read) || _source is not ISignalReader reader)
        {
            return SignalResult.Failed<SignalSample<object?>>(new SignalFailure(
                SignalRuntimeErrorCodes.ReadUnsupported,
                "The signal source does not support point-in-time reads."));
        }

        SignalResult<SignalSample<object?>> result;
        try
        {
            result = await reader.ReadAsync(Address, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return SignalResult.Failed<SignalSample<object?>>(new SignalFailure(
                SignalRuntimeErrorCodes.ReadFailed,
                "The signal source read failed.",
                exception));
        }

        if (!result.IsSuccess)
        {
            return result;
        }

        SignalSample<object?>? sample = result.Value;
        if (sample is null)
        {
            return SignalResult.Failed<SignalSample<object?>>(new SignalFailure(
                SignalRuntimeErrorCodes.ReadFailed,
                "The signal source returned no sample."));
        }

        SignalSample<object?>? normalized = _subscription.Publish(sample);
        return normalized is null
            ? SignalResult.Failed<SignalSample<object?>>(new SignalFailure(
                SignalRuntimeErrorCodes.ReadFailed,
                "The signal subscription closed before read evidence could be published."))
            : SignalResult.Success(normalized);
    }

    internal async ValueTask ReleaseAsync(SignalSubscriberBuffer buffer)
    {
        var close = false;
        lock (_sync)
        {
            _subscription.RemoveSubscriber(buffer);
            if (_leaseCount > 0)
            {
                _leaseCount--;
            }

            if (_leaseCount == 0 && !_closing)
            {
                _closing = true;
                close = true;
            }
        }

        if (close)
        {
            await CloseSubscriptionAsync().ConfigureAwait(false);
        }
    }

    internal async ValueTask CloseIfUnusedAsync()
    {
        var close = false;
        lock (_sync)
        {
            if (_leaseCount == 0 && !_closing)
            {
                _closing = true;
                close = true;
            }
        }

        if (close)
        {
            await CloseSubscriptionAsync().ConfigureAwait(false);
        }
    }

    internal async ValueTask CloseAsync()
    {
        var close = false;
        lock (_sync)
        {
            if (!_closing)
            {
                _closing = true;
                close = true;
            }
        }

        if (close)
        {
            await CloseSubscriptionAsync().ConfigureAwait(false);
        }
        else
        {
            await _closed.Task.ConfigureAwait(false);
        }
    }

    internal Task WaitForCloseAsync(CancellationToken cancellationToken) =>
        _closed.Task.WaitAsync(cancellationToken);

    private async ValueTask CloseSubscriptionAsync()
    {
        try
        {
            await _subscription.StopAsync().ConfigureAwait(false);
            _closed.TrySetResult();
        }
        catch (Exception exception)
        {
            _closed.TrySetException(exception);
            throw;
        }
        finally
        {
            _onEmpty(this);
        }
    }
}
