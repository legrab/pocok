// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Primitives;

namespace Pocok.Hosting;

/// <summary>
/// Coordinates explicit startup, readiness, failure, cancellation, shutdown, and restart transitions.
/// </summary>
public sealed class ReadinessSource : IReadinessSignal
{
    private readonly object _gate = new();
    private TaskCompletionSource _completion = CreateCompletion();
    private Error? _error;
    private long _sequence;
    private ReadinessState _state = ReadinessState.Stopped;

    /// <inheritdoc />
    public ReadinessState State
    {
        get
        {
            lock (_gate)
            {
                return _state;
            }
        }
    }

    /// <inheritdoc />
    public Error? Failure
    {
        get
        {
            lock (_gate)
            {
                return _error;
            }
        }
    }

    /// <summary>
    /// Begins a startup attempt from the stopped or failed state.
    /// </summary>
    public ReadinessCycle BeginStartup()
    {
        lock (_gate)
        {
            if (_state is not ReadinessState.Stopped and not ReadinessState.Failed)
            {
                throw InvalidTransition(nameof(BeginStartup));
            }

            var sequence = checked(_sequence + 1);

            if (_completion.Task.IsCompleted)
            {
                _completion = CreateCompletion();
            }

            _state = ReadinessState.Starting;
            _error = null;
            _sequence = sequence;
            return new ReadinessCycle(this, sequence);
        }
    }

    /// <summary>
    /// Marks the active startup attempt ready and releases all of its waiters.
    /// </summary>
    public void MarkReady(ReadinessCycle cycle)
    {
        lock (_gate)
        {
            ValidateActiveCycle(cycle, nameof(MarkReady));
            _state = ReadinessState.Ready;
            _completion.SetResult();
        }
    }

    /// <summary>
    /// Fails the active startup attempt and faults all of its waiters.
    /// </summary>
    public void MarkFailed(ReadinessCycle cycle, Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        lock (_gate)
        {
            ValidateActiveCycle(cycle, nameof(MarkFailed));
            _state = ReadinessState.Failed;
            _error = error;
            _completion.SetException(new ReadinessFailedException(error));
        }
    }

    /// <summary>
    /// Cancels the active startup attempt without converting cancellation into failure.
    /// </summary>
    public void CancelStartup(ReadinessCycle cycle, CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            throw new ArgumentException("The cancellation token must already be cancelled.", nameof(cancellationToken));
        }

        lock (_gate)
        {
            ValidateActiveCycle(cycle, nameof(CancelStartup));
            _state = ReadinessState.Stopped;
            _error = null;
            ReplaceCompletion().SetCanceled(cancellationToken);
        }
    }

    /// <summary>
    /// Begins shutdown and makes the active readiness cycle unavailable immediately.
    /// </summary>
    public void BeginShutdown()
    {
        lock (_gate)
        {
            if (_state is not ReadinessState.Starting and not ReadinessState.Ready and not ReadinessState.Failed)
            {
                throw InvalidTransition(nameof(BeginShutdown));
            }

            var sequence = checked(_sequence + 1);

            _state = ReadinessState.Stopping;
            _error = null;
            _sequence = sequence;
            ReplaceCompletion().TrySetException(new ReadinessStoppedException());
        }
    }

    /// <summary>
    /// Completes an active shutdown and permits a later startup attempt.
    /// </summary>
    public void MarkStopped()
    {
        lock (_gate)
        {
            if (_state is not ReadinessState.Stopping)
            {
                throw InvalidTransition(nameof(MarkStopped));
            }

            _state = ReadinessState.Stopped;
        }
    }

    /// <summary>
    /// Fails an active shutdown and faults waiters until the next startup attempt begins.
    /// </summary>
    public void MarkShutdownFailed(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        lock (_gate)
        {
            if (_state is not ReadinessState.Stopping)
            {
                throw InvalidTransition(nameof(MarkShutdownFailed));
            }

            _state = ReadinessState.Failed;
            _error = error;
            _completion.SetException(new ReadinessFailedException(error));
        }
    }

    /// <inheritdoc />
    public Task WaitUntilReadyAsync(CancellationToken cancellationToken = default)
    {
        Task completion;

        lock (_gate)
        {
            completion = _completion.Task;
        }

        return cancellationToken.CanBeCanceled
            ? completion.WaitAsync(cancellationToken)
            : completion;
    }

    private static TaskCompletionSource CreateCompletion() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private InvalidOperationException InvalidTransition(string operation) =>
        new($"{operation} is not valid while readiness is {_state}.");

    private TaskCompletionSource ReplaceCompletion()
    {
        var previous = _completion;
        _completion = CreateCompletion();
        return previous;
    }

    private void ValidateActiveCycle(ReadinessCycle cycle, string operation)
    {
        ArgumentNullException.ThrowIfNull(cycle);

        if (!ReferenceEquals(cycle.Owner, this) || cycle.Sequence != _sequence || _state is not ReadinessState.Starting)
        {
            throw InvalidTransition(operation);
        }
    }
}
