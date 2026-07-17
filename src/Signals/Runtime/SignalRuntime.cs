// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Collections.Concurrent;
using Pocok.Conversion;
using Pocok.Signals.Operations;
using Pocok.Signals.Sources;

namespace Pocok.Signals.Runtime;

/// <summary>
///     Owns shared raw subscriptions and creates typed explicit leases.
/// </summary>
public sealed class SignalRuntime : IAsyncDisposable
{
    private readonly IValueConverter _converter;

    private readonly TaskCompletionSource _disposeCompletion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly ConcurrentDictionary<SignalAddress, EntryCreation> _entries = [];
    private readonly Lock _lifecycleSync = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly SignalRuntimeOptions _options;
    private readonly SignalSourceFactory _sourceFactory;
    private readonly TimeProvider _timeProvider;
    private int _disposed;

    /// <summary>
    ///     Initializes a signal runtime with explicit source resolution and policies.
    /// </summary>
    public SignalRuntime(
        SignalSourceFactory sourceFactory,
        SignalRuntimeOptions? options = null,
        TimeProvider? timeProvider = null,
        IValueConverter? converter = null)
    {
        ArgumentNullException.ThrowIfNull(sourceFactory);

        _sourceFactory = sourceFactory;
        _options = options ?? new SignalRuntimeOptions();
        _timeProvider = timeProvider ?? TimeProvider.System;
        _converter = converter ?? new ValueConverter();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        EntryCreation[]? creations = null;
        lock (_lifecycleSync)
        {
            if (_disposed == 0)
            {
                _disposed = 1;
                creations = _entries.Values.ToArray();
                foreach (EntryCreation creation in creations) creation.Retire();

                _entries.Clear();
            }
        }

        if (creations is null)
        {
            await _disposeCompletion.Task.ConfigureAwait(false);
            return;
        }

        try
        {
            await _lifetime.CancelAsync().ConfigureAwait(false);

            foreach (EntryCreation creation in creations)
                try
                {
                    SignalResult<SharedSignalEntry> created = await creation.Task.Value.ConfigureAwait(false);
                    if (created.IsSuccess) await created.Value!.CloseAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
                {
                }
        }
        catch (Exception exception)
        {
            _disposeCompletion.TrySetException(exception);
            throw;
        }
        finally
        {
            _lifetime.Dispose();
        }

        _disposeCompletion.TrySetResult();
    }

    /// <summary>
    ///     Connects a typed lease, sharing source resolution and subscription work by address.
    /// </summary>
    public async ValueTask<SignalResult<SignalConnection<T>>> ConnectAsync<T>(
        SignalAddress address,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        cancellationToken.ThrowIfCancellationRequested();

        while (true)
        {
            EntryCreation creation;
            Task<SignalResult<SharedSignalEntry>> creationTask;
            lock (_lifecycleSync)
            {
                ObjectDisposedException.ThrowIf(_disposed != 0, this);
                creation = _entries.GetOrAdd(
                    address,
                    static (key, runtime) => new EntryCreation(() => Task.Run(() => runtime.CreateEntryAsync(key))),
                    this);

                if (!creation.TryEnter())
                {
                    RemoveCreation(address, creation);
                    continue;
                }

                creationTask = creation.Task.Value;
            }

            SignalResult<SharedSignalEntry> created;
            try
            {
                created = await creationTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (creation.ExitAndIsLast()) _ = CleanupUnusedCreationAsync(address, creation);

                throw;
            }
            catch
            {
                creation.Retire();
                creation.Exit();
                RemoveCreation(address, creation);
                throw;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                if (creation.ExitAndIsLast()) _ = CleanupUnusedCreationAsync(address, creation);

                throw new OperationCanceledException(cancellationToken);
            }

            if (!created.IsSuccess)
            {
                creation.Retire();
                creation.Exit();
                RemoveCreation(address, creation);
                return SignalResult.Failed<SignalConnection<T>>(created.Failure!);
            }

            SignalConnection<T>? connection = null;
            var disposed = false;
            lock (_lifecycleSync)
            {
                disposed = _disposed != 0;
                if (!disposed) created.Value!.TryAcquire(out connection);
            }

            creation.Exit();
            ObjectDisposedException.ThrowIf(disposed, this);
            if (connection is not null) return SignalResult.Success(connection);

            await created.Value!.WaitForCloseAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<SignalResult<SharedSignalEntry>> CreateEntryAsync(SignalAddress address)
    {
        SignalResult<ISignalSource> resolved;
        try
        {
            resolved = await _sourceFactory(address.Source, _lifetime.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return SignalResult.Failed<SharedSignalEntry>(new SignalFailure(
                SignalRuntimeErrorCodes.SourceUnavailable,
                "The signal source factory failed.",
                exception));
        }

        if (!resolved.IsSuccess) return SignalResult.Failed<SharedSignalEntry>(resolved.Failure!);

        ISignalSource? source = resolved.Value!;
        if (source is null)
            return SignalResult.Failed<SharedSignalEntry>(new SignalFailure(
                SignalRuntimeErrorCodes.SourceUnavailable,
                "The signal source factory returned no source."));

        if (source.Id != address.Source)
            return SignalResult.Failed<SharedSignalEntry>(new SignalFailure(
                SignalRuntimeErrorCodes.SourceMismatch,
                "The resolved signal source identity does not match the address."));

        if (!source.Capabilities.HasFlag(SignalSourceCapabilities.Subscribe) || source is not ISignalSubscriber)
            return SignalResult.Failed<SharedSignalEntry>(new SignalFailure(
                SignalRuntimeErrorCodes.SubscribeUnsupported,
                "The signal source does not support subscriptions."));

        return SignalResult.Success(new SharedSignalEntry(
            address,
            source,
            _options,
            _timeProvider,
            _converter,
            entry => RemoveEntry(address, entry),
            _lifetime.Token));
    }

    private async Task CleanupUnusedCreationAsync(
        SignalAddress address,
        EntryCreation creation)
    {
        try
        {
            SignalResult<SharedSignalEntry> created = await creation.Task.Value.ConfigureAwait(false);
            if (created.IsSuccess)
            {
                await created.Value!.CloseIfUnusedAsync().ConfigureAwait(false);
            }
            else
            {
                creation.Retire();
                RemoveCreation(address, creation);
            }
        }
        catch (OperationCanceledException)
        {
            creation.Retire();
            RemoveCreation(address, creation);
        }
    }

    private void RemoveEntry(SignalAddress address, SharedSignalEntry entry)
    {
        if (_entries.TryGetValue(address, out EntryCreation? creation) &&
            creation.Task.IsValueCreated &&
            creation.Task.Value.IsCompletedSuccessfully &&
            creation.Task.Value.Result.IsSuccess &&
            ReferenceEquals(creation.Task.Value.Result.Value, entry))
        {
            creation.Retire();
            RemoveCreation(address, creation);
        }
    }

    private void RemoveCreation(
        SignalAddress address,
        EntryCreation creation)
    {
        ((ICollection<KeyValuePair<SignalAddress, EntryCreation>>)_entries)
            .Remove(new KeyValuePair<SignalAddress, EntryCreation>(address, creation));
    }

    private sealed class EntryCreation(Func<Task<SignalResult<SharedSignalEntry>>> factory)
    {
        private readonly Lock _sync = new();
        private bool _retired;
        private int _waiters;

        internal Lazy<Task<SignalResult<SharedSignalEntry>>> Task { get; } =
            new(factory, LazyThreadSafetyMode.ExecutionAndPublication);

        internal bool TryEnter()
        {
            lock (_sync)
            {
                if (_retired) return false;

                _waiters++;
                return true;
            }
        }

        internal void Exit()
        {
            lock (_sync)
            {
                _waiters--;
            }
        }

        internal bool ExitAndIsLast()
        {
            lock (_sync)
            {
                _waiters--;
                return _waiters == 0;
            }
        }

        internal void Retire()
        {
            lock (_sync)
            {
                _retired = true;
            }
        }
    }
}
