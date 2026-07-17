// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Localization;
using Pocok.BackgroundWork.Debouncing;
using Pocok.BackgroundWork.FailureHandling;
using Pocok.BackgroundWork.Observation;

namespace Pocok.Localization.FileResources;

/// <summary>Loads one logical localization resource set from external JSON and RESX files.</summary>
/// <remarks>
///     Reads use immutable snapshots and remain available after disposal. Reloading is rejected after disposal.
/// </remarks>
public sealed class FileStringLocalizer : IAsyncDisposable, IStringLocalizer
{
    private readonly object _disposeGate = new();
    private readonly SemaphoreSlim _reloadGate = new(1, 1);
    private readonly DebouncedTaskRunner? _reloadRunner;
    private readonly FileStringLocalizerSettings _settings;
    private readonly FileSystemWatcher? _watcher;
    private int _disposed;
    private Task? _disposeTask;
    private FileResourceSnapshot _snapshot;
    private FileLocalizationStatus _status;

    /// <summary>Initializes a file-backed localizer and performs the initial load synchronously.</summary>
    /// <param name="options">The resource-set and reload configuration.</param>
    public FileStringLocalizer(FileStringLocalizerOptions options)
    {
        _settings = FileStringLocalizerSettings.Create(options);
        DateTimeOffset attemptedAt = _settings.TimeProvider.GetUtcNow();
        _snapshot = FileResourceLoader.Load(_settings, null);
        _status = new FileLocalizationStatus(attemptedAt, attemptedAt, true, null);

        if (_settings.WatchForChanges)
        {
            _reloadRunner = new DebouncedTaskRunner(
                ReloadFromWatcherAsync,
                new DebouncedTaskRunnerOptions
                {
                    QuietPeriod = _settings.ReloadDebounce,
                    TimeProvider = _settings.TimeProvider,
                    FailurePolicy = BackgroundWorkFailurePolicy.Continue,
                    OnFailure = static (_, _) => ValueTask.CompletedTask
                });

            try
            {
                _watcher = CreateWatcher();
            }
            catch
            {
                _reloadRunner.DisposeAsync().AsTask().GetAwaiter().GetResult();
                throw;
            }
        }

        NotifyStatusChanged(_status);
    }

    /// <summary>Gets the latest stored load status.</summary>
    public FileLocalizationStatus Status => Volatile.Read(ref _status);

    /// <summary>Stops file watching and reload work while preserving the last snapshot for reads.</summary>
    /// <returns>A value task that completes after active reload work has stopped.</returns>
    public ValueTask DisposeAsync()
    {
        lock (_disposeGate)
        {
            _disposeTask ??= DisposeCoreAsync();
            return new ValueTask(_disposeTask);
        }
    }

    /// <inheritdoc />
    public LocalizedString this[string name]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);
            return Find(name);
        }
    }

    /// <inheritdoc />
    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(arguments);

            LocalizedString value = Find(name);
            if (value.ResourceNotFound || arguments.Length == 0) return value;

            return new LocalizedString(
                name,
                string.Format(CultureInfo.CurrentCulture, value.Value, arguments),
                false,
                value.SearchedLocation);
        }
    }

    /// <inheritdoc />
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        FileResourceSnapshot snapshot = Volatile.Read(ref _snapshot);
        foreach (KeyValuePair<string, string> entry in
                 snapshot.Enumerate(CultureInfo.CurrentUICulture, includeParentCultures))
            yield return new LocalizedString(entry.Key, entry.Value, false, _settings.BasePath);
    }

    /// <summary>Reloads all matching files and atomically publishes a complete valid snapshot.</summary>
    /// <param name="cancellationToken">Cancels this reload and retry delays.</param>
    /// <returns>A task representing the complete reload attempt.</returns>
    public Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return ReloadCoreAsync(cancellationToken);
    }

    private LocalizedString Find(string name)
    {
        FileResourceSnapshot snapshot = Volatile.Read(ref _snapshot);
        return snapshot.TryGetValue(CultureInfo.CurrentUICulture, name, out var value)
            ? new LocalizedString(name, value, false, _settings.BasePath)
            : new LocalizedString(name, name, true, _settings.BasePath);
    }

    private async Task ReloadCoreAsync(CancellationToken cancellationToken)
    {
        await _reloadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            Exception? finalFailure = null;
            DateTimeOffset attemptedAt = _settings.TimeProvider.GetUtcNow();

            for (var attempt = 0; attempt <= _settings.ReloadRetryCount; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    FileResourceSnapshot current = Volatile.Read(ref _snapshot);
                    FileResourceSnapshot next = FileResourceLoader.Load(_settings, current);
                    Volatile.Write(ref _snapshot, next);
                    DateTimeOffset succeededAt = _settings.TimeProvider.GetUtcNow();
                    PublishStatus(new FileLocalizationStatus(attemptedAt, succeededAt, true, null));
                    return;
                }
                catch (Exception exception) when (IsReloadFailure(exception))
                {
                    finalFailure = exception;
                    if (attempt == _settings.ReloadRetryCount) break;
                }

                if (_settings.ReloadRetryDelay > TimeSpan.Zero)
                    await Task.Delay(
                        _settings.ReloadRetryDelay,
                        _settings.TimeProvider,
                        cancellationToken).ConfigureAwait(false);
            }

            FileLocalizationStatus previous = Status;
            PublishStatus(new FileLocalizationStatus(
                attemptedAt,
                previous.LastSuccessfulAt,
                previous.HasValidSnapshot,
                finalFailure));
            ExceptionDispatchInfo.Capture(finalFailure!).Throw();
        }
        finally
        {
            _reloadGate.Release();
        }
    }

    private async ValueTask ReloadFromWatcherAsync(CancellationToken cancellationToken)
    {
        await ReloadCoreAsync(cancellationToken).ConfigureAwait(false);
    }

    private FileSystemWatcher CreateWatcher()
    {
        var watcher = new FileSystemWatcher(_settings.ContainingDirectory)
        {
            Filter = "*",
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
        };

        watcher.Changed += OnFileChanged;
        watcher.Created += OnFileChanged;
        watcher.Deleted += OnFileChanged;
        watcher.Renamed += OnFileRenamed;
        watcher.Error += OnWatcherError;
        watcher.EnableRaisingEvents = true;
        return watcher;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs eventArgs)
    {
        if (FileResourceLoader.MayBeCandidatePath(_settings, eventArgs.FullPath)) RequestWatcherReload();
    }

    private void OnFileRenamed(object sender, RenamedEventArgs eventArgs)
    {
        if (FileResourceLoader.MayBeCandidatePath(_settings, eventArgs.OldFullPath) ||
            FileResourceLoader.MayBeCandidatePath(_settings, eventArgs.FullPath))
            RequestWatcherReload();
    }

    private void OnWatcherError(object sender, ErrorEventArgs eventArgs)
    {
        FileLocalizationStatus previous = Status;
        PublishStatus(new FileLocalizationStatus(
            _settings.TimeProvider.GetUtcNow(),
            previous.LastSuccessfulAt,
            previous.HasValidSnapshot,
            eventArgs.GetException()));
        RequestWatcherReload();
    }

    private void RequestWatcherReload()
    {
        DebouncedTaskRunner? runner = _reloadRunner;
        if (runner is null || Volatile.Read(ref _disposed) != 0) return;

        try
        {
            _ = runner.RequestAsync().Observe(PublishWatcherCoordinationFailure);
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void PublishWatcherCoordinationFailure(Exception exception)
    {
        FileLocalizationStatus previous = Status;
        PublishStatus(new FileLocalizationStatus(
            _settings.TimeProvider.GetUtcNow(),
            previous.LastSuccessfulAt,
            previous.HasValidSnapshot,
            exception));
    }

    private async Task DisposeCoreAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

        if (_watcher is not null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnFileChanged;
            _watcher.Created -= OnFileChanged;
            _watcher.Deleted -= OnFileChanged;
            _watcher.Renamed -= OnFileRenamed;
            _watcher.Error -= OnWatcherError;
            _watcher.Dispose();
        }

        if (_reloadRunner is not null) await _reloadRunner.DisposeAsync().ConfigureAwait(false);

        await _reloadGate.WaitAsync().ConfigureAwait(false);
        _reloadGate.Release();
    }

    private void PublishStatus(FileLocalizationStatus status)
    {
        Volatile.Write(ref _status, status);
        NotifyStatusChanged(status);
    }

    private void NotifyStatusChanged(FileLocalizationStatus status)
    {
        try
        {
            _settings.StatusChanged?.Invoke(status);
        }
        catch (Exception)
        {
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
    }

    private static bool IsReloadFailure(Exception exception)
    {
        return exception is IOException or UnauthorizedAccessException or FormatException or InvalidOperationException;
    }
}
