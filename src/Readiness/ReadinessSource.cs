// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Readiness;

/// <summary>
///     Coordinates explicit startup, readiness, failure, cancellation, shutdown, and restart transitions.
/// </summary>
public sealed class ReadinessSource : IReadinessSignal
{
    private readonly object _gate = new();
    private TaskCompletionSource _completion = CreateCompletion();
    private ReadinessSnapshot _snapshot = new(ReadinessState.Stopped, 0, null);

    /// <inheritdoc />
    public ReadinessSnapshot Snapshot
    {
        get
        {
            lock (_gate)
            {
                return _snapshot;
            }
        }
    }

    /// <inheritdoc />
    public ReadinessState State => Snapshot.State;

    /// <inheritdoc />
    public ReadinessFailure? Failure => Snapshot.Failure;

    /// <inheritdoc />
    public Task WaitUntilReadyAsync(CancellationToken cancellationToken = default)
    {
        Task completion;
        lock (_gate)
        {
            completion = _completion.Task;
        }

        return cancellationToken.CanBeCanceled ? completion.WaitAsync(cancellationToken) : completion;
    }

    /// <summary>Begins a startup attempt from the stopped or failed state.</summary>
    public ReadinessCycle BeginStartup()
    {
        lock (_gate)
        {
            if (_snapshot.State is not ReadinessState.Stopped and not ReadinessState.Failed)
                throw InvalidTransition(nameof(BeginStartup));

            var sequence = checked(_snapshot.Sequence + 1);
            if (_completion.Task.IsCompleted) _completion = CreateCompletion();

            _snapshot = new ReadinessSnapshot(ReadinessState.Starting, sequence, null);
            return new ReadinessCycle(this, sequence);
        }
    }

    /// <summary>Marks the active startup attempt ready and releases all of its waiters.</summary>
    public void MarkReady(ReadinessCycle cycle)
    {
        lock (_gate)
        {
            ValidateActiveCycle(cycle, nameof(MarkReady));
            _snapshot = _snapshot with { State = ReadinessState.Ready, Failure = null };
            _completion.SetResult();
        }
    }

    /// <summary>Fails the active startup attempt and faults all of its waiters.</summary>
    public void MarkFailed(ReadinessCycle cycle, ReadinessFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        lock (_gate)
        {
            ValidateActiveCycle(cycle, nameof(MarkFailed));
            _snapshot = _snapshot with { State = ReadinessState.Failed, Failure = failure };
            _completion.SetException(new ReadinessFailedException(failure));
        }
    }

    /// <summary>Cancels the active startup attempt without converting cancellation into failure.</summary>
    public void CancelStartup(ReadinessCycle cycle, CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested)
            throw new ArgumentException("The cancellation token must already be cancelled.", nameof(cancellationToken));

        lock (_gate)
        {
            ValidateActiveCycle(cycle, nameof(CancelStartup));
            _snapshot = _snapshot with { State = ReadinessState.Stopped, Failure = null };
            ReplaceCompletion().SetCanceled(cancellationToken);
        }
    }

    /// <summary>Begins shutdown and makes the active readiness cycle unavailable immediately.</summary>
    public void BeginShutdown()
    {
        lock (_gate)
        {
            if (_snapshot.State is not ReadinessState.Starting and not ReadinessState.Ready
                and not ReadinessState.Failed) throw InvalidTransition(nameof(BeginShutdown));

            _snapshot = new ReadinessSnapshot(ReadinessState.Stopping, checked(_snapshot.Sequence + 1), null);
            ReplaceCompletion().TrySetException(new ReadinessStoppedException());
        }
    }

    /// <summary>Completes an active shutdown and permits a later startup attempt.</summary>
    public void MarkStopped()
    {
        lock (_gate)
        {
            if (_snapshot.State is not ReadinessState.Stopping) throw InvalidTransition(nameof(MarkStopped));

            _snapshot = _snapshot with { State = ReadinessState.Stopped, Failure = null };
        }
    }

    /// <summary>Fails an active shutdown and faults waiters until the next startup attempt begins.</summary>
    public void MarkShutdownFailed(ReadinessFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        lock (_gate)
        {
            if (_snapshot.State is not ReadinessState.Stopping) throw InvalidTransition(nameof(MarkShutdownFailed));

            _snapshot = _snapshot with { State = ReadinessState.Failed, Failure = failure };
            _completion.SetException(new ReadinessFailedException(failure));
        }
    }

    /// <summary>Waits for a specific startup cycle and rejects a stale cycle.</summary>
    public Task WaitUntilReadyAsync(ReadinessCycle cycle, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cycle);
        Task completion;
        lock (_gate)
        {
            if (!ReferenceEquals(cycle.Owner, this) || cycle.Sequence != _snapshot.Sequence)
                throw InvalidTransition(nameof(WaitUntilReadyAsync));

            completion = _completion.Task;
        }

        return cancellationToken.CanBeCanceled ? completion.WaitAsync(cancellationToken) : completion;
    }

    private static TaskCompletionSource CreateCompletion()
    {
        return new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private InvalidOperationException InvalidTransition(string operation)
    {
        return new InvalidOperationException(
            $"{operation} is not valid while readiness is {_snapshot.State} at sequence {_snapshot.Sequence}.");
    }

    private TaskCompletionSource ReplaceCompletion()
    {
        TaskCompletionSource previous = _completion;
        _completion = CreateCompletion();
        return previous;
    }

    private void ValidateActiveCycle(ReadinessCycle cycle, string operation)
    {
        ArgumentNullException.ThrowIfNull(cycle);
        if (!ReferenceEquals(cycle.Owner, this) || cycle.Sequence != _snapshot.Sequence ||
            _snapshot.State is not ReadinessState.Starting) throw InvalidTransition(operation);
    }
}
