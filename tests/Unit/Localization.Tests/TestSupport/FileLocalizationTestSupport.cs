// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using System.Security;
using System.Text;
using Pocok.Localization.FileResources;

namespace Pocok.Localization.Tests.TestSupport;

internal sealed class TemporaryDirectory : IDisposable
{
    private TemporaryDirectory(string path)
    {
        Path = path;
        Directory.CreateDirectory(path);
    }

    internal string Path { get; }

    public void Dispose()
    {
        Directory.Delete(Path, true);
    }

    internal static TemporaryDirectory Create()
    {
        return new TemporaryDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            $"pocok-localization-{Guid.NewGuid():N}"));
    }
}

internal sealed class TemporaryCulture : IDisposable
{
    private readonly CultureInfo _culture;
    private readonly CultureInfo _uiCulture;

    internal TemporaryCulture(string cultureName)
    {
        _culture = CultureInfo.CurrentCulture;
        _uiCulture = CultureInfo.CurrentUICulture;
        CultureInfo culture = string.IsNullOrEmpty(cultureName)
            ? CultureInfo.InvariantCulture
            : CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
    }
}

internal sealed class ManualTimeProvider(DateTimeOffset initialTime) : TimeProvider
{
    private readonly object _gate = new();
    private readonly HashSet<ManualTimer> _timers = [];
    private DateTimeOffset _utcNow = initialTime;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

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
            foreach (ManualTimer timer in _timers.ToArray())
                if (timer.TryTakeCallback(_utcNow, out (TimerCallback Callback, object? State) callback))
                    callbacks.Add(callback);
        }

        foreach ((TimerCallback callback, var state) in callbacks) callback(state);
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
        private readonly TimerCallback _callback;
        private readonly ManualTimeProvider _provider;
        private readonly object? _state;
        private bool _disposed;
        private DateTimeOffset? _dueAt;
        private TimeSpan _period;

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
                if (_disposed) return false;

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
                if (_disposed) return;

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

internal static class FileLocalizationTestData
{
    internal static string CreateResx(params (string Name, string Value)[] entries)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        builder.AppendLine("<root>");
        foreach (var (name, value) in entries)
            builder.Append("  <data name=\"")
                .Append(SecurityElement.Escape(name))
                .Append("\" xml:space=\"preserve\"><value>")
                .Append(SecurityElement.Escape(value))
                .AppendLine("</value></data>");

        builder.AppendLine("</root>");
        return builder.ToString();
    }

    internal static FileStringLocalizer CreateLocalizer(
        string root,
        Action<FileStringLocalizerOptionsBuilder>? configure = null)
    {
        var builder = new FileStringLocalizerOptionsBuilder(root);
        configure?.Invoke(builder);
        return new FileStringLocalizer(builder.Build());
    }
}

internal sealed class FileStringLocalizerOptionsBuilder(string rootDirectory)
{
    internal string RootDirectory { get; set; } = rootDirectory;

    internal string BaseName { get; set; } = "Messages";

    internal IReadOnlyList<LocalizationFileFormat> FormatPrecedence { get; set; } =
        [LocalizationFileFormat.Json, LocalizationFileFormat.Resx];

    internal bool WatchForChanges { get; set; }

    internal TimeSpan ReloadDebounce { get; set; } = TimeSpan.FromMilliseconds(50);

    internal int ReloadRetryCount { get; set; }

    internal TimeSpan ReloadRetryDelay { get; set; }

    internal long MaximumFileSizeBytes { get; set; } = 1_048_576;

    internal bool AllowJsonComments { get; set; }

    internal bool AllowTrailingCommas { get; set; }

    internal MissingLocalizationFileBehavior MissingFileBehavior { get; set; } =
        MissingLocalizationFileBehavior.RetainLastKnownGood;

    internal TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    internal Action<FileLocalizationStatus>? StatusChanged { get; set; }

    internal FileStringLocalizerOptions Build()
    {
        return new FileStringLocalizerOptions
        {
            RootDirectory = RootDirectory,
            BaseName = BaseName,
            FormatPrecedence = FormatPrecedence,
            WatchForChanges = WatchForChanges,
            ReloadDebounce = ReloadDebounce,
            ReloadRetryCount = ReloadRetryCount,
            ReloadRetryDelay = ReloadRetryDelay,
            MaximumFileSizeBytes = MaximumFileSizeBytes,
            AllowJsonComments = AllowJsonComments,
            AllowTrailingCommas = AllowTrailingCommas,
            MissingFileBehavior = MissingFileBehavior,
            TimeProvider = TimeProvider,
            StatusChanged = StatusChanged
        };
    }
}

internal static class FileLocalizationTestAsync
{
    internal static async Task UntilAsync(Func<bool> condition, Func<string>? diagnostic = null)
    {
        var startedAt = TimeProvider.System.GetTimestamp();
        while (!condition())
        {
            if (TimeProvider.System.GetElapsedTime(startedAt) >= TimeSpan.FromSeconds(5))
                throw new TimeoutException(diagnostic?.Invoke() ??
                                           "The localization test condition was not met within five seconds.");

            await Task.Delay(10).ConfigureAwait(false);
        }
    }
}
