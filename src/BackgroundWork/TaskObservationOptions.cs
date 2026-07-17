// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.BackgroundWork;

/// <summary>Configures callbacks for observing a non-generic task.</summary>
public sealed class TaskObservationOptions
{
    internal List<Func<CancellationToken, ValueTask>> SuccessHandlers { get; } = [];

    internal List<Func<OperationCanceledException, CancellationToken, ValueTask>> CancellationHandlers { get; } = [];

    internal List<FaultHandlerRegistration> FaultHandlers { get; } = [];

    /// <summary>Gets or sets the token that controls callback dispatch and callback waiting.</summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>Registers a synchronous success callback.</summary>
    /// <param name="handler">The callback.</param>
    /// <returns>This options instance.</returns>
    public TaskObservationOptions OnSuccess(Action handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        SuccessHandlers.Add(cancellationToken => InvokeAsync(handler, cancellationToken));
        return this;
    }

    /// <summary>Registers an asynchronous success callback.</summary>
    /// <param name="handler">The callback.</param>
    /// <returns>This options instance.</returns>
    public TaskObservationOptions OnSuccess(Func<CancellationToken, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        SuccessHandlers.Add(handler);
        return this;
    }

    /// <summary>Registers a synchronous cancellation callback.</summary>
    /// <param name="handler">The callback.</param>
    /// <returns>This options instance.</returns>
    public TaskObservationOptions OnCanceled(Action<OperationCanceledException> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        CancellationHandlers.Add((exception, cancellationToken) => InvokeAsync(handler, exception, cancellationToken));
        return this;
    }

    /// <summary>Registers an asynchronous cancellation callback.</summary>
    /// <param name="handler">The callback.</param>
    /// <returns>This options instance.</returns>
    public TaskObservationOptions OnCanceled(
        Func<OperationCanceledException, CancellationToken, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        CancellationHandlers.Add(handler);
        return this;
    }

    /// <summary>Registers a synchronous handler for the first matching exception type.</summary>
    /// <typeparam name="TException">The exception type.</typeparam>
    /// <param name="handler">The callback.</param>
    /// <returns>This options instance.</returns>
    public TaskObservationOptions OnFault<TException>(Action<TException> handler)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(handler);
        return OnFault(
            exception => exception is TException,
            exception => handler((TException)exception));
    }

    /// <summary>Registers an asynchronous handler for the first matching exception type.</summary>
    /// <typeparam name="TException">The exception type.</typeparam>
    /// <param name="handler">The callback.</param>
    /// <returns>This options instance.</returns>
    public TaskObservationOptions OnFault<TException>(
        Func<TException, CancellationToken, ValueTask> handler)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(handler);
        return OnFault(
            exception => exception is TException,
            (exception, cancellationToken) => handler((TException)exception, cancellationToken));
    }

    /// <summary>Registers a synchronous filtered fault handler.</summary>
    /// <param name="predicate">The predicate evaluated in registration order.</param>
    /// <param name="handler">The callback for the first matching predicate.</param>
    /// <returns>This options instance.</returns>
    public TaskObservationOptions OnFault(
        Func<Exception, bool> predicate,
        Action<Exception> handler)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(handler);
        FaultHandlers.Add(new FaultHandlerRegistration(
            predicate,
            (exception, cancellationToken) => InvokeAsync(handler, exception, cancellationToken)));
        return this;
    }

    /// <summary>Registers an asynchronous filtered fault handler.</summary>
    /// <param name="predicate">The predicate evaluated in registration order.</param>
    /// <param name="handler">The callback for the first matching predicate.</param>
    /// <returns>This options instance.</returns>
    public TaskObservationOptions OnFault(
        Func<Exception, bool> predicate,
        Func<Exception, CancellationToken, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(handler);
        FaultHandlers.Add(new FaultHandlerRegistration(predicate, handler));
        return this;
    }

    private static ValueTask InvokeAsync(Action handler, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        handler();
        return ValueTask.CompletedTask;
    }

    private static ValueTask InvokeAsync<T>(Action<T> handler, T value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        handler(value);
        return ValueTask.CompletedTask;
    }
}

/// <summary>Configures callbacks for observing a task result.</summary>
/// <typeparam name="T">The task result type.</typeparam>
public sealed class TaskObservationOptions<T>
{
    internal List<SuccessHandlerRegistration<T>> SuccessHandlers { get; } = [];

    internal List<Func<OperationCanceledException, CancellationToken, ValueTask>> CancellationHandlers { get; } = [];

    internal List<FaultHandlerRegistration> FaultHandlers { get; } = [];

    /// <summary>Gets or sets the token that controls callback dispatch and callback waiting.</summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>Registers a synchronous fallback success callback.</summary>
    /// <param name="handler">The callback.</param>
    /// <returns>This options instance.</returns>
    public TaskObservationOptions<T> OnSuccess(Action<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        SuccessHandlers.Add(new SuccessHandlerRegistration<T>(
            null,
            (value, cancellationToken) => InvokeAsync(handler, value, cancellationToken)));
        return this;
    }

    /// <summary>Registers an asynchronous fallback success callback.</summary>
    /// <param name="handler">The callback.</param>
    /// <returns>This options instance.</returns>
    public TaskObservationOptions<T> OnSuccess(Func<T, CancellationToken, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        SuccessHandlers.Add(new SuccessHandlerRegistration<T>(null, handler));
        return this;
    }

    /// <summary>Registers a synchronous filtered success callback.</summary>
    /// <param name="predicate">The predicate evaluated in registration order.</param>
    /// <param name="handler">The callback for the first matching predicate.</param>
    /// <returns>This options instance.</returns>
    public TaskObservationOptions<T> OnSuccess(Func<T, bool> predicate, Action<T> handler)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(handler);
        SuccessHandlers.Add(new SuccessHandlerRegistration<T>(
            predicate,
            (value, cancellationToken) => InvokeAsync(handler, value, cancellationToken)));
        return this;
    }

    /// <summary>Registers an asynchronous filtered success callback.</summary>
    /// <param name="predicate">The predicate evaluated in registration order.</param>
    /// <param name="handler">The callback for the first matching predicate.</param>
    /// <returns>This options instance.</returns>
    public TaskObservationOptions<T> OnSuccess(
        Func<T, bool> predicate,
        Func<T, CancellationToken, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(handler);
        SuccessHandlers.Add(new SuccessHandlerRegistration<T>(predicate, handler));
        return this;
    }

    /// <summary>Registers a synchronous cancellation callback.</summary>
    /// <param name="handler">The callback.</param>
    /// <returns>This options instance.</returns>
    public TaskObservationOptions<T> OnCanceled(Action<OperationCanceledException> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        CancellationHandlers.Add((exception, cancellationToken) => InvokeAsync(handler, exception, cancellationToken));
        return this;
    }

    /// <summary>Registers an asynchronous cancellation callback.</summary>
    /// <param name="handler">The callback.</param>
    /// <returns>This options instance.</returns>
    public TaskObservationOptions<T> OnCanceled(
        Func<OperationCanceledException, CancellationToken, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        CancellationHandlers.Add(handler);
        return this;
    }

    /// <summary>Registers a synchronous handler for the first matching exception type.</summary>
    /// <typeparam name="TException">The exception type.</typeparam>
    /// <param name="handler">The callback.</param>
    /// <returns>This options instance.</returns>
    public TaskObservationOptions<T> OnFault<TException>(Action<TException> handler)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(handler);
        return OnFault(
            exception => exception is TException,
            exception => handler((TException)exception));
    }

    /// <summary>Registers an asynchronous handler for the first matching exception type.</summary>
    /// <typeparam name="TException">The exception type.</typeparam>
    /// <param name="handler">The callback.</param>
    /// <returns>This options instance.</returns>
    public TaskObservationOptions<T> OnFault<TException>(
        Func<TException, CancellationToken, ValueTask> handler)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(handler);
        return OnFault(
            exception => exception is TException,
            (exception, cancellationToken) => handler((TException)exception, cancellationToken));
    }

    /// <summary>Registers a synchronous filtered fault handler.</summary>
    /// <param name="predicate">The predicate evaluated in registration order.</param>
    /// <param name="handler">The callback for the first matching predicate.</param>
    /// <returns>This options instance.</returns>
    public TaskObservationOptions<T> OnFault(
        Func<Exception, bool> predicate,
        Action<Exception> handler)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(handler);
        FaultHandlers.Add(new FaultHandlerRegistration(
            predicate,
            (exception, cancellationToken) => InvokeAsync(handler, exception, cancellationToken)));
        return this;
    }

    /// <summary>Registers an asynchronous filtered fault handler.</summary>
    /// <param name="predicate">The predicate evaluated in registration order.</param>
    /// <param name="handler">The callback for the first matching predicate.</param>
    /// <returns>This options instance.</returns>
    public TaskObservationOptions<T> OnFault(
        Func<Exception, bool> predicate,
        Func<Exception, CancellationToken, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(handler);
        FaultHandlers.Add(new FaultHandlerRegistration(predicate, handler));
        return this;
    }

    private static ValueTask InvokeAsync<TValue>(
        Action<TValue> handler,
        TValue value,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        handler(value);
        return ValueTask.CompletedTask;
    }
}

internal sealed record FaultHandlerRegistration(
    Func<Exception, bool> Predicate,
    Func<Exception, CancellationToken, ValueTask> Handler);

internal sealed record SuccessHandlerRegistration<T>(
    Func<T, bool>? Predicate,
    Func<T, CancellationToken, ValueTask> Handler);
