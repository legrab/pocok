// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Runtime.CompilerServices;
using Pocok.Conversion;
using Pocok.Signals.Operations;
using Pocok.Signals.Sources;
using Pocok.Signals.Writing;

namespace Pocok.Signals.Runtime;

/// <summary>
///     Represents one typed, single-reader lease on a shared source subscription.
/// </summary>
public sealed class SignalConnection<T> : IAsyncDisposable
{
    private readonly SignalSubscriberBuffer _buffer;
    private readonly IValueConverter _converter;

    private readonly TaskCompletionSource _disposeCompletion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly SharedSignalEntry _entry;
    private int _disposed;
    private int _enumerationStarted;

    internal SignalConnection(
        SharedSignalEntry entry,
        SignalSubscriberBuffer buffer,
        IValueConverter converter)
    {
        _entry = entry;
        _buffer = buffer;
        _converter = converter;
    }

    /// <summary>
    ///     Gets the connected signal address.
    /// </summary>
    public SignalAddress Address => _entry.Address;

    /// <summary>
    ///     Gets the number of oldest pending samples discarded by this connection.
    /// </summary>
    public long DroppedSampleCount => _buffer.DroppedSampleCount;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            await _disposeCompletion.Task.ConfigureAwait(false);
            return;
        }

        try
        {
            await _entry.ReleaseAsync(_buffer).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _disposeCompletion.TrySetException(exception);
            throw;
        }

        _disposeCompletion.TrySetResult();
    }

    /// <summary>
    ///     Reads one point-in-time sample through the connected source and publishes it to this runtime entry.
    /// </summary>
    public async ValueTask<SignalResult<SignalSample<T>>> ReadAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

        SignalResult<SignalSample<object?>> read = await _entry.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (!read.IsSuccess) return SignalResult.Failed<SignalSample<T>>(read.Failure!);

        SignalSample<T> converted = ConvertSample(read.Value!);
        return converted.Quality == SignalQuality.Failed && read.Value!.Quality != SignalQuality.Failed
            ? SignalResult.Failed<SignalSample<T>>(converted.Failure!)
            : SignalResult.Success(converted);
    }

    /// <summary>
    ///     Reads the typed sample stream. A connection permits one enumeration.
    /// </summary>
    public async IAsyncEnumerable<SignalSample<T>> Samples(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (Interlocked.Exchange(ref _enumerationStarted, 1) != 0)
            throw new InvalidOperationException("A signal connection permits one sample enumeration.");

        await foreach (SignalSample<object?> sample in _buffer.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            yield return ConvertSample(sample);
    }

    /// <summary>
    ///     Writes a value through the connected source and returns typed consistency evidence.
    /// </summary>
    public async ValueTask<SignalResult<SignalWriteResult<T>>> WriteAsync(
        T? value,
        SignalWriteConsistency consistency,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (!Enum.IsDefined(consistency)) throw new ArgumentOutOfRangeException(nameof(consistency));

        SignalResult<SignalWriteResult> written =
            await _entry.WriteAsync(value, consistency, cancellationToken).ConfigureAwait(false);
        if (!written.IsSuccess) return SignalResult.Failed<SignalWriteResult<T>>(written.Failure!);

        SignalWriteResult raw = written.Value!;
        if (raw.Sample is null) return SignalResult.Success(new SignalWriteResult<T>(raw.Consistency, null));

        SignalSample<T> converted = ConvertSample(raw.Sample);
        if (converted.Quality == SignalQuality.Failed && raw.Sample.Quality != SignalQuality.Failed)
            return SignalResult.Failed<SignalWriteResult<T>>(converted.Failure!);

        return SignalResult.Success(new SignalWriteResult<T>(raw.Consistency, converted));
    }

    private SignalSample<T> ConvertSample(SignalSample<object?> sample)
    {
        if (!sample.HasValue)
            return new SignalSample<T>(
                default,
                false,
                sample.SourceTimestamp,
                sample.ObservedAt,
                sample.Quality,
                sample.Sequence,
                sample.Failure);

        ConversionResult<T> converted;
        try
        {
            converted = _converter.Convert<T>(sample.Value);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            converted = ConversionResult<T>.Failure(new ConversionFailure(
                SignalRuntimeErrorCodes.ConversionFailed,
                "The signal value converter failed.",
                exception: exception));
        }

        return converted.IsSuccess
            ? new SignalSample<T>(
                converted.Value,
                true,
                sample.SourceTimestamp,
                sample.ObservedAt,
                sample.Quality,
                sample.Sequence,
                sample.Failure)
            : new SignalSample<T>(
                default,
                false,
                sample.SourceTimestamp,
                sample.ObservedAt,
                SignalQuality.Failed,
                sample.Sequence,
                new SignalFailure(
                    converted.Error!.Code,
                    converted.Error!.Message,
                    converted.Error.Exception));
    }
}
