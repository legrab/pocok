// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.BackgroundWork.Tests;

internal sealed class ManualTimeProvider(DateTimeOffset initialTime) : TimeProvider
{
    private readonly object _gate = new();
    private readonly HashSet<ManualTimer> _timers = [];
    private DateTimeOffset _utcNow = initialTime;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate)
        {
            return _utcNow;
        }
    }

    public override long GetTimestamp()
    {
        lock (_gate)
        {
            return _utcNow.UtcTicks;
        }
    }

    internal int ScheduledTimerCount
    {
        get
        {
            lock (_gate)
            {
                return _timers.Count;
            }
        }
    }

    public override ITimer CreateTimer(
        TimerCallback callback,
        object? state,
        TimeSpan dueTime,
        TimeSpan period)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var timer = new ManualTimer(this, callback, state, dueTime, period);
        lock (_gate)
        {
            _timers.Add(timer);
        }

        return timer;
    }

    internal void Advance(TimeSpan duration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);

        var callbacks = new List<(TimerCallback Callback, object? State)>();
        lock (_gate)
        {
            _utcNow += duration;
            foreach (ManualTimer timer in _timers)
            {
                if (timer.TryTakeCallback(_utcNow, out (TimerCallback Callback, object? State) callback))
                {
                    callbacks.Add(callback);
                }
            }
        }

        foreach ((TimerCallback callback, var state) in callbacks)
        {
            callback(state);
        }
    }

    private void Remove(ManualTimer timer)
    {
        lock (_gate)
        {
            _timers.Remove(timer);
        }
    }

    private sealed class ManualTimer : ITimer
    {
        private readonly ManualTimeProvider _provider;
        private readonly TimerCallback _callback;
        private readonly object? _state;
        private DateTimeOffset? _dueAt;
        private TimeSpan _period;
        private bool _disposed;

        internal ManualTimer(
            ManualTimeProvider provider,
            TimerCallback callback,
            object? state,
            TimeSpan dueTime,
            TimeSpan period)
        {
            _provider = provider;
            _callback = callback;
            _state = state;
            _period = period;
            _dueAt = dueTime == Timeout.InfiniteTimeSpan ? null : provider.GetUtcNow() + dueTime;
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            lock (_provider._gate)
            {
                if (_disposed)
                {
                    return false;
                }

                _period = period;
                _dueAt = dueTime == Timeout.InfiniteTimeSpan
                    ? null
                    : _provider._utcNow + dueTime;
                return true;
            }
        }

        public void Dispose()
        {
            lock (_provider._gate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _dueAt = null;
            }

            _provider.Remove(this);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        internal bool TryTakeCallback(
            DateTimeOffset now,
            out (TimerCallback Callback, object? State) callback)
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

internal static class TestAsync
{
    internal static async Task UntilAsync(Func<bool> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        var startedAt = TimeProvider.System.GetTimestamp();
        while (!condition())
        {
            if (TimeProvider.System.GetElapsedTime(startedAt) >= TimeSpan.FromSeconds(5))
            {
                throw new TimeoutException("The asynchronous test condition was not met within five seconds.");
            }

            await Task.Delay(10).ConfigureAwait(false);
        }
    }

    internal static async Task AdvanceUntilCompletedAsync(
        ManualTimeProvider timeProvider,
        Task task,
        TimeSpan step)
    {
        while (!task.IsCompleted)
        {
            await UntilAsync(() => task.IsCompleted || timeProvider.ScheduledTimerCount > 0).ConfigureAwait(false);
            if (task.IsCompleted)
            {
                break;
            }

            timeProvider.Advance(step);
            await Task.Yield();
        }

        await task.ConfigureAwait(false);
    }
}
