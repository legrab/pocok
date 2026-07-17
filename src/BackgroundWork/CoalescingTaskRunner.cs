// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.BackgroundWork;

/// <summary>Runs one operation at a time and collapses requests received while active into at most one rerun.</summary>
/// <remarks>
/// Caller cancellation cancels only the caller's wait. <see cref="StopAsync"/> owns cancellation of shared work.
/// </remarks>
public sealed class CoalescingTaskRunner : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly Func<CancellationToken, ValueTask> _operation;
    private readonly CoalescingTaskRunnerOptions _options;
    private readonly CancellationTokenSource _lifetime = new();
    private TaskCompletionSource? _drainCompletion;
    private Task? _disposeTask;
    private bool _pending;
    private bool _isRunning;
    private bool _stopped;
    private bool _disposed;

    /// <summary>Initializes a new coalescing runner.</summary>
    /// <param name="operation">The non-overlapping operation.</param>
    /// <param name="options">Optional runner configuration.</param>
    public CoalescingTaskRunner(
        Func<CancellationToken, ValueTask> operation,
        CoalescingTaskRunnerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(operation);
        options ??= new CoalescingTaskRunnerOptions();
        ArgumentNullException.ThrowIfNull(options.TimeProvider);

        if (options.MinimumInterval < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MinimumInterval cannot be negative.");
        }

        BackgroundWorkFailure.Validate(options.FailurePolicy, options.OnFailure, nameof(options));
        _operation = operation;
        _options = options;
    }

    /// <summary>Gets whether a drain cycle is active or cooling down.</summary>
    public bool IsRunning
    {
        get
        {
            lock (_gate)
            {
                return _isRunning;
            }
        }
    }

    /// <summary>Requests execution and returns the shared task for the current drain cycle.</summary>
    /// <param name="cancellationToken">Cancels only this caller's wait.</param>
    /// <returns>The current drain-cycle task.</returns>
    public Task RequestAsync(CancellationToken cancellationToken = default)
    {
        Task drain;
        TaskCompletionSource? completionToStart = null;

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_stopped || _disposed, typeof(CoalescingTaskRunner));
            _pending = true;

            if (_drainCompletion is null)
            {
                completionToStart = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _drainCompletion = completionToStart;
                _isRunning = true;
            }

            drain = _drainCompletion.Task;
        }

        if (completionToStart is not null)
        {
            _ = DrainAsync(completionToStart);
        }

        return cancellationToken.CanBeCanceled
            ? drain.WaitAsync(cancellationToken)
            : drain;
    }

    /// <summary>Stops the runner, cancels active shared work, and rejects later requests.</summary>
    /// <param name="cancellationToken">Cancels only the wait for shutdown.</param>
    /// <returns>A task that completes when active coordination has stopped.</returns>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        Task? drain;
        var cancelLifetime = false;

        lock (_gate)
        {
            if (!_stopped)
            {
                _stopped = true;
                _pending = false;
                cancelLifetime = true;
            }

            drain = _drainCompletion?.Task;
        }

        if (cancelLifetime)
        {
            await _lifetime.CancelAsync();
        }

        if (drain is null)
        {
            return;
        }

        try
        {
            await drain.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (
            _lifetime.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
        }
    }

    /// <summary>Stops the runner and releases its cancellation resources.</summary>
    /// <returns>A value task that completes when disposal finishes.</returns>
    public ValueTask DisposeAsync()
    {
        lock (_gate)
        {
            _disposeTask ??= DisposeCoreAsync();
            return new ValueTask(_disposeTask);
        }
    }

    private async Task DrainAsync(TaskCompletionSource completion)
    {
        try
        {
            while (true)
            {
                lock (_gate)
                {
                    if (_stopped)
                    {
                        CompleteDrainAsCanceled(completion);
                        return;
                    }

                    _pending = false;
                }

                try
                {
                    await _operation(_lifetime.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
                {
                    CompleteDrainAsCanceled(completion);
                    return;
                }
                catch (Exception exception)
                {
                    if (_options.FailurePolicy == BackgroundWorkFailurePolicy.Stop)
                    {
                        CompleteDrainAsFaulted(completion, exception);
                        return;
                    }

                    try
                    {
                        await BackgroundWorkFailure.HandleAsync(
                            exception,
                            _options.OnFailure!,
                            _lifetime.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
                    {
                        CompleteDrainAsCanceled(completion);
                        return;
                    }
                    catch (Exception handlerException)
                    {
                        CompleteDrainAsFaulted(completion, handlerException);
                        return;
                    }
                }

                bool rerun;
                lock (_gate)
                {
                    if (_stopped)
                    {
                        CompleteDrainAsCanceled(completion);
                        return;
                    }

                    rerun = _pending;
                    if (!rerun)
                    {
                        CompleteDrainAsSucceeded(completion);
                        return;
                    }
                }

                if (_options.MinimumInterval > TimeSpan.Zero)
                {
                    try
                    {
                        await Task.Delay(
                            _options.MinimumInterval,
                            _options.TimeProvider,
                            _lifetime.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
                    {
                        CompleteDrainAsCanceled(completion);
                        return;
                    }
                }
            }
        }
        catch (Exception exception)
        {
            CompleteDrainAsFaulted(completion, exception);
        }
    }

    private async Task DisposeCoreAsync()
    {
        try
        {
            await StopAsync().ConfigureAwait(false);
        }
        finally
        {
            lock (_gate)
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _lifetime.Dispose();
                }
            }
        }
    }

    private void CompleteDrainAsSucceeded(TaskCompletionSource completion)
    {
        lock (_gate)
        {
            if (!ReferenceEquals(_drainCompletion, completion))
            {
                return;
            }

            _drainCompletion = null;
            _isRunning = false;
        }

        completion.TrySetResult();
    }

    private void CompleteDrainAsCanceled(TaskCompletionSource completion)
    {
        lock (_gate)
        {
            if (!ReferenceEquals(_drainCompletion, completion))
            {
                return;
            }

            _drainCompletion = null;
            _isRunning = false;
            _pending = false;
        }

        completion.TrySetCanceled(_lifetime.Token);
    }

    private void CompleteDrainAsFaulted(TaskCompletionSource completion, Exception exception)
    {
        lock (_gate)
        {
            if (!ReferenceEquals(_drainCompletion, completion))
            {
                return;
            }

            _drainCompletion = null;
            _isRunning = false;
            _pending = false;
            _stopped = true;
        }

        completion.TrySetException(exception);
    }
}
