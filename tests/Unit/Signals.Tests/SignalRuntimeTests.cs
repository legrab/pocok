// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Pocok.Signals.Tests;

public sealed class SignalRuntimeTests
{
    private static readonly SignalAddress Address = new(new SourceId("test"), "value");
    private static readonly DateTimeOffset Start = new(2026, 7, 15, 9, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task ConnectionsShareSubscriptionAndReplayLatestSample()
    {
        var source = new TestSignalSource();
        await using var runtime = RuntimeFor(source);
        await using var first = (await runtime.ConnectAsync<int>(Address)).Value!;
        await using var firstSamples = first.Samples().GetAsyncEnumerator();
        await TestAsync.UntilAsync(() => source.ActiveSubscriptions == 1);

        source.Emit("42");
        var initial = await TestAsync.NextAsync(firstSamples);

        await using var second = (await runtime.ConnectAsync<int>(Address)).Value!;
        await using var secondSamples = second.Samples().GetAsyncEnumerator();
        var replay = await TestAsync.NextAsync(secondSamples);

        replay.ShouldBe(initial);
        initial.Value.ShouldBe(42);
        source.SubscriptionCount.ShouldBe(1);
    }

    [Test]
    public async Task BoundedConnectionDropsOldestPendingSamples()
    {
        var source = new TestSignalSource();
        await using var runtime = RuntimeFor(source, new SignalRuntimeOptions(subscriberCapacity: 2));
        await using var connection = (await runtime.ConnectAsync<int>(Address)).Value!;
        await TestAsync.UntilAsync(() => source.ActiveSubscriptions == 1);

        for (var value = 1; value <= 5; value++)
        {
            source.Emit(value);
        }

        await TestAsync.UntilAsync(() => connection.DroppedSampleCount == 3);
        await using var samples = connection.Samples().GetAsyncEnumerator();
        (await TestAsync.NextAsync(samples)).Value.ShouldBe(4);
        (await TestAsync.NextAsync(samples)).Value.ShouldBe(5);
    }

    [Test]
    public async Task DuplicateGoodSampleRefreshesStalenessDeadline()
    {
        var source = new TestSignalSource();
        var time = new ManualTimeProvider(Start);
        await using var runtime = RuntimeFor(
            source,
            new SignalRuntimeOptions(
                reconnectDelay: TimeSpan.FromMinutes(1),
                staleAfter: TimeSpan.FromMinutes(5)),
            time);
        await using var connection = (await runtime.ConnectAsync<int>(Address)).Value!;
        await using var samples = connection.Samples().GetAsyncEnumerator();
        await TestAsync.UntilAsync(() => source.ActiveSubscriptions == 1);

        source.Emit(7);
        var good = await TestAsync.NextAsync(samples);
        time.Advance(TimeSpan.FromMinutes(4));
        source.Emit(7);
        await TestAsync.UntilAsync(() => source.DeliveredSampleCount >= 2);
        time.Advance(TimeSpan.FromMinutes(4));
        time.Advance(TimeSpan.FromMinutes(1));
        var stale = await TestAsync.NextAsync(samples);

        good.Quality.ShouldBe(SignalQuality.Good);
        stale.Quality.ShouldBe(SignalQuality.Stale);
        stale.Value.ShouldBe(7);
        stale.Sequence.ShouldBe(2);
        stale.ObservedAt.ShouldBe(Start.AddMinutes(9));
    }

    [Test]
    public async Task ConfirmedWriteConvertsAndPublishesEvidence()
    {
        var source = new TestSignalSource
        {
            WriteHandler = (_, _) => SignalResult.Success(new SignalWriteResult(
                SignalWriteConsistency.ReadAfterWrite,
                new SignalSample<object?>("42", true, TestTimes.Source, TestTimes.Observed, SignalQuality.Good, 1)))
        };
        await using var runtime = RuntimeFor(source);
        await using var connection = (await runtime.ConnectAsync<int>(Address)).Value!;
        await using var samples = connection.Samples().GetAsyncEnumerator();
        await TestAsync.UntilAsync(() => source.ActiveSubscriptions == 1);

        var written = await connection.WriteAsync(42, SignalWriteConsistency.ReadAfterWrite);
        var published = await TestAsync.NextAsync(samples);

        written.IsSuccess.ShouldBeTrue();
        written.Value!.Consistency.ShouldBe(SignalWriteConsistency.ReadAfterWrite);
        written.Value.Sample!.Value.ShouldBe(42);
        published.ShouldBe(written.Value.Sample);
    }

    [Test]
    public async Task PointReadConvertsAndPublishesSample()
    {
        var source = new TestSignalSource(
            capabilities: SignalSourceCapabilities.Read | SignalSourceCapabilities.Subscribe)
        {
            ReadHandler = _ => SignalResult.Success(new SignalSample<object?>(
                "42", true, TestTimes.Source, TestTimes.Observed, SignalQuality.Good, 1))
        };
        await using var runtime = RuntimeFor(source);
        await using var connection = (await runtime.ConnectAsync<int>(Address)).Value!;
        await using var samples = connection.Samples().GetAsyncEnumerator();
        await TestAsync.UntilAsync(() => source.ActiveSubscriptions == 1);

        var read = await connection.ReadAsync();
        var published = await TestAsync.NextAsync(samples);

        read.IsSuccess.ShouldBeTrue();
        read.Value!.Value.ShouldBe(42);
        published.ShouldBe(read.Value);
    }

    [Test]
    public async Task PointReadRequiresReadCapabilityAndInterface()
    {
        var source = new TestSignalSource();
        await using var runtime = RuntimeFor(source);
        await using var connection = (await runtime.ConnectAsync<int>(Address)).Value!;

        var read = await connection.ReadAsync();

        read.Failure!.Code.ShouldBe(SignalRuntimeErrorCodes.ReadUnsupported);
    }

    [Test]
    public async Task PointReadFailureIsStructured()
    {
        var source = new TestSignalSource(capabilities: SignalSourceCapabilities.Read | SignalSourceCapabilities.Subscribe)
        {
            ReadHandler = _ => throw new InvalidOperationException("Synthetic reader failure.")
        };
        await using var runtime = RuntimeFor(source);
        await using var connection = (await runtime.ConnectAsync<int>(Address)).Value!;

        var read = await connection.ReadAsync();

        read.Failure!.Code.ShouldBe(SignalRuntimeErrorCodes.ReadFailed);
    }

    [Test]
    public async Task PointReadConversionFailureIsStructured()
    {
        var source = new TestSignalSource(
            capabilities: SignalSourceCapabilities.Read | SignalSourceCapabilities.Subscribe)
        {
            ReadHandler = _ => SignalResult.Success(new SignalSample<object?>(
                "not-a-number", true, TestTimes.Source, TestTimes.Observed, SignalQuality.Good, 1))
        };
        await using var runtime = RuntimeFor(source);
        await using var connection = (await runtime.ConnectAsync<int>(Address)).Value!;

        var read = await connection.ReadAsync();

        read.IsSuccess.ShouldBeFalse();
        read.Failure!.Code.ShouldBe("conversion.invalid-format");
    }

    [Test]
    public async Task SourceFailurePublishesFailureAndReconnects()
    {
        var source = new FailingThenStreamingSource();
        var time = new ManualTimeProvider(Start);
        await using var runtime = new SignalRuntime(
            (_, _) => ValueTask.FromResult(SignalResult.Success<ISignalSource>(source)),
            new SignalRuntimeOptions(reconnectDelay: TimeSpan.FromMinutes(1)),
            time);
        await using var connection = (await runtime.ConnectAsync<int>(Address)).Value!;
        await using var samples = connection.Samples().GetAsyncEnumerator();

        var failure = await TestAsync.NextAsync(samples);
        failure.Quality.ShouldBe(SignalQuality.Failed);
        failure.Failure!.Code.ShouldBe(SignalRuntimeErrorCodes.SubscriptionFailed);

        time.Advance(TimeSpan.FromMinutes(1));
        await TestAsync.UntilAsync(() => source.SubscriptionCount == 2);
        source.Emit(9);
        var recovered = await TestAsync.NextAsync(samples);
        recovered.Value.ShouldBe(9);
        recovered.Sequence.ShouldBe(2);
    }

    [Test]
    public async Task ConversionFailureIsLocalToTypedConnection()
    {
        var source = new TestSignalSource();
        await using var runtime = RuntimeFor(source);
        await using var numbers = (await runtime.ConnectAsync<int>(Address)).Value!;
        await using var text = (await runtime.ConnectAsync<string>(Address)).Value!;
        await using var numberSamples = numbers.Samples().GetAsyncEnumerator();
        await using var textSamples = text.Samples().GetAsyncEnumerator();
        await TestAsync.UntilAsync(() => source.ActiveSubscriptions == 1);

        source.Emit("not-a-number");
        var number = await TestAsync.NextAsync(numberSamples);
        var textValue = await TestAsync.NextAsync(textSamples);

        number.Quality.ShouldBe(SignalQuality.Failed);
        number.Failure!.Code.ShouldBe("conversion.invalid-format");
        textValue.Quality.ShouldBe(SignalQuality.Good);
        textValue.Value.ShouldBe("not-a-number");
    }

    [Test]
    public async Task WeakerWriteEvidenceIsRejected()
    {
        var source = new TestSignalSource
        {
            WriteHandler = (_, _) => SignalResult.Success(
                new SignalWriteResult(SignalWriteConsistency.Acknowledged, null))
        };
        await using var runtime = RuntimeFor(source);
        await using var connection = (await runtime.ConnectAsync<int>(Address)).Value!;

        var result = await connection.WriteAsync(1, SignalWriteConsistency.Confirmed);

        result.IsSuccess.ShouldBeFalse();
        result.Failure!.Code.ShouldBe(SignalRuntimeErrorCodes.WriteConsistencyNotMet);
    }

    [Test]
    public async Task WriteRequiresBothCapabilityAndWriterInterface()
    {
        var source = new TestSignalSource(capabilities: SignalSourceCapabilities.Subscribe);
        await using var runtime = RuntimeFor(source);
        await using var connection = (await runtime.ConnectAsync<int>(Address)).Value!;

        var result = await connection.WriteAsync(1, SignalWriteConsistency.Acknowledged);

        result.Failure!.Code.ShouldBe(SignalRuntimeErrorCodes.WriteUnsupported);
    }

    [Test]
    public async Task ConnectionAllowsOneSampleEnumeration()
    {
        var source = new TestSignalSource();
        await using var runtime = RuntimeFor(source);
        await using var connection = (await runtime.ConnectAsync<int>(Address)).Value!;
        await using var first = connection.Samples().GetAsyncEnumerator();
        await TestAsync.UntilAsync(() => source.ActiveSubscriptions == 1);
        source.Emit(1);
        await TestAsync.NextAsync(first);

        await using var second = connection.Samples().GetAsyncEnumerator();
        await Should.ThrowAsync<InvalidOperationException>(async () => await second.MoveNextAsync());
    }

    [Test]
    public async Task DisposingLastConnectionStopsSharedSubscription()
    {
        var source = new TestSignalSource();
        await using var runtime = RuntimeFor(source);
        var connection = (await runtime.ConnectAsync<int>(Address)).Value!;
        await TestAsync.UntilAsync(() => source.ActiveSubscriptions == 1);

        await connection.DisposeAsync();

        await TestAsync.UntilAsync(() => source.ActiveSubscriptions == 0);
        source.SubscriptionCount.ShouldBe(1);
    }

    private static SignalRuntime RuntimeFor(
        TestSignalSource source,
        SignalRuntimeOptions? options = null,
        TimeProvider? timeProvider = null) =>
        new(
            (_, _) => ValueTask.FromResult(SignalResult.Success<ISignalSource>(source)),
            options,
            timeProvider);

    private sealed class FailingThenStreamingSource : ISignalSubscriber
    {
        private readonly Channel<SignalSample<object?>> _samples = Channel.CreateUnbounded<SignalSample<object?>>();
        private int _subscriptionCount;

        public SourceId Id { get; } = new("test");
        public SignalSourceCapabilities Capabilities => SignalSourceCapabilities.Subscribe;
        internal int SubscriptionCount => Volatile.Read(ref _subscriptionCount);

        public async IAsyncEnumerable<SignalSample<object?>> SubscribeAsync(
            SignalAddress address,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _subscriptionCount) == 1)
            {
                await Task.Yield();
                throw new InvalidOperationException("Synthetic source failure.");
            }

            await foreach (var sample in _samples.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return sample;
            }
        }

        internal void Emit(object value) =>
            _samples.Writer.TryWrite(new SignalSample<object?>(
                value, true, TestTimes.Source, TestTimes.Observed, SignalQuality.Good, 1));
    }
}

internal sealed class TestSignalSource : ISignalSubscriber, ISignalReader, ISignalWriter
{
    private readonly Channel<SignalSample<object?>> _samples = Channel.CreateUnbounded<SignalSample<object?>>();
    private long _sourceSequence;
    private int _activeSubscriptions;
    private int _deliveredSampleCount;
    private int _subscriptionCount;

    internal TestSignalSource(
        SourceId? id = null,
        SignalSourceCapabilities capabilities = SignalSourceCapabilities.Subscribe | SignalSourceCapabilities.Write)
    {
        Id = id ?? new SourceId("test");
        Capabilities = capabilities;
    }

    public SourceId Id { get; }
    public SignalSourceCapabilities Capabilities { get; }
    internal int ActiveSubscriptions => Volatile.Read(ref _activeSubscriptions);
    internal int DeliveredSampleCount => Volatile.Read(ref _deliveredSampleCount);
    internal int SubscriptionCount => Volatile.Read(ref _subscriptionCount);
    internal Func<object?, SignalWriteConsistency, SignalResult<SignalWriteResult>>? WriteHandler { get; init; }
    internal Func<SignalAddress, SignalResult<SignalSample<object?>>>? ReadHandler { get; init; }

    public async IAsyncEnumerable<SignalSample<object?>> SubscribeAsync(
        SignalAddress address,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _subscriptionCount);
        Interlocked.Increment(ref _activeSubscriptions);
        try
        {
            await foreach (var sample in _samples.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return sample;
                Interlocked.Increment(ref _deliveredSampleCount);
            }
        }
        finally
        {
            Interlocked.Decrement(ref _activeSubscriptions);
        }
    }

    public ValueTask<SignalResult<SignalWriteResult>> WriteAsync(
        SignalAddress address,
        object? value,
        SignalWriteConsistency consistency,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = WriteHandler?.Invoke(value, consistency) ??
                     SignalResult.Success(new SignalWriteResult(SignalWriteConsistency.Acknowledged, null));
        return ValueTask.FromResult(result);
    }

    public ValueTask<SignalResult<SignalSample<object?>>> ReadAsync(
        SignalAddress address,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = ReadHandler?.Invoke(address) ??
                     SignalResult.Failed<SignalSample<object?>>(new SignalFailure(
                         "test.read-unconfigured",
                         "The synthetic reader is not configured."));
        return ValueTask.FromResult(result);
    }

    internal void Emit(object? value, SignalQuality quality = SignalQuality.Good, bool hasValue = true) =>
        _samples.Writer.TryWrite(new SignalSample<object?>(
            value,
            hasValue,
            TestTimes.Source,
            TestTimes.Observed,
            quality,
            Interlocked.Increment(ref _sourceSequence)));

    internal void Complete() => _samples.Writer.TryComplete();
}

internal sealed class ManualTimeProvider(DateTimeOffset initialTime) : TimeProvider
{
    private readonly Lock _sync = new();
    private readonly HashSet<ManualTimer> _timers = [];
    private DateTimeOffset _utcNow = initialTime;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    public override DateTimeOffset GetUtcNow() { lock (_sync) return _utcNow; }
    public override long GetTimestamp() { lock (_sync) return _utcNow.UtcTicks; }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state, dueTime, period);
        lock (_sync) _timers.Add(timer);
        return timer;
    }

    internal void Advance(TimeSpan duration)
    {
        var callbacks = new List<(TimerCallback Callback, object? State)>();
        lock (_sync)
        {
            _utcNow += duration;
            foreach (var timer in _timers)
            {
                if (timer.TryTakeCallback(_utcNow, out var callback)) callbacks.Add(callback);
            }
        }

        foreach (var (callback, state) in callbacks) callback(state);
    }

    private void Remove(ManualTimer timer) { lock (_sync) _timers.Remove(timer); }

    private sealed class ManualTimer : ITimer
    {
        private readonly ManualTimeProvider _provider;
        private readonly TimerCallback _callback;
        private readonly object? _state;
        private DateTimeOffset? _dueAt;
        private readonly TimeSpan _period;
        private bool _disposed;

        internal ManualTimer(ManualTimeProvider provider, TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            _provider = provider;
            _callback = callback;
            _state = state;
            _period = period;
            _dueAt = dueTime == Timeout.InfiniteTimeSpan ? null : provider.GetUtcNow() + dueTime;
        }

        public bool Change(TimeSpan dueTime, TimeSpan period) => throw new NotSupportedException();

        public void Dispose()
        {
            lock (_provider._sync)
            {
                if (_disposed) return;
                _disposed = true;
                _dueAt = null;
            }

            _provider.Remove(this);
        }

        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }

        internal bool TryTakeCallback(DateTimeOffset now, out (TimerCallback Callback, object? State) callback)
        {
            if (_disposed || _dueAt is null || _dueAt > now)
            {
                callback = default;
                return false;
            }

            callback = (_callback, _state);
            _dueAt = _period == Timeout.InfiniteTimeSpan ? null : now + _period;
            return true;
        }
    }
}

internal static class TestTimes
{
    internal static readonly DateTimeOffset Observed = new(2026, 7, 15, 8, 0, 0, TimeSpan.Zero);
    internal static readonly DateTimeOffset Source = Observed.AddSeconds(-1);
}

internal static class TestAsync
{
    internal static async Task UntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition()) await Task.Delay(10, timeout.Token);
    }

    internal static async Task<SignalSample<T>> NextAsync<T>(IAsyncEnumerator<SignalSample<T>> enumerator)
    {
        (await enumerator.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5))).ShouldBeTrue();
        return enumerator.Current;
    }
}
