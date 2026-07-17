// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.BackgroundWork.Observation;

/// <summary>Provides guarded observation for intentionally non-awaited tasks.</summary>
public static class TaskObservationExtensions
{
    /// <summary>Observes a task with a mandatory synchronous fallback fault handler.</summary>
    /// <param name="task">The source task.</param>
    /// <param name="onFault">The fallback fault handler.</param>
    /// <param name="configure">Optional outcome configuration.</param>
    /// <returns>An owned observation handle.</returns>
    public static TaskObservation Observe(
        this Task task,
        Action<Exception> onFault,
        Action<TaskObservationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(onFault);
        return task.Observe((exception, cancellationToken) => InvokeAsync(onFault, exception, cancellationToken),
            configure);
    }

    /// <summary>Observes a task with a mandatory asynchronous fallback fault handler.</summary>
    /// <param name="task">The source task.</param>
    /// <param name="onFault">The fallback fault handler.</param>
    /// <param name="configure">Optional outcome configuration.</param>
    /// <returns>An owned observation handle.</returns>
    public static TaskObservation Observe(
        this Task task,
        Func<Exception, CancellationToken, ValueTask> onFault,
        Action<TaskObservationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(onFault);

        var options = new TaskObservationOptions();
        configure?.Invoke(options);
        return new TaskObservation(ObserveCoreAsync(task, options, onFault));
    }

    /// <summary>Observes a result task with a mandatory synchronous fallback fault handler.</summary>
    /// <typeparam name="T">The task result type.</typeparam>
    /// <param name="task">The source task.</param>
    /// <param name="onFault">The fallback fault handler.</param>
    /// <param name="configure">Optional outcome configuration.</param>
    /// <returns>An owned observation handle.</returns>
    public static TaskObservation Observe<T>(
        this Task<T> task,
        Action<Exception> onFault,
        Action<TaskObservationOptions<T>>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(onFault);
        return task.Observe((exception, cancellationToken) => InvokeAsync(onFault, exception, cancellationToken),
            configure);
    }

    /// <summary>Observes a result task with a mandatory asynchronous fallback fault handler.</summary>
    /// <typeparam name="T">The task result type.</typeparam>
    /// <param name="task">The source task.</param>
    /// <param name="onFault">The fallback fault handler.</param>
    /// <param name="configure">Optional outcome configuration.</param>
    /// <returns>An owned observation handle.</returns>
    public static TaskObservation Observe<T>(
        this Task<T> task,
        Func<Exception, CancellationToken, ValueTask> onFault,
        Action<TaskObservationOptions<T>>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(onFault);

        var options = new TaskObservationOptions<T>();
        configure?.Invoke(options);
        return new TaskObservation(ObserveCoreAsync(task, options, onFault));
    }

    private static async Task<TaskObservationResult> ObserveCoreAsync(
        Task source,
        TaskObservationOptions options,
        Func<Exception, CancellationToken, ValueTask> fallbackFaultHandler)
    {
        try
        {
            await source.ConfigureAwait(false);
            return await HandleSuccessAsync(options).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (source.IsCanceled)
        {
            return await HandleCancellationAsync(
                exception,
                options.CancellationHandlers,
                options.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return await HandleFaultAsync(
                exception,
                options.FaultHandlers,
                fallbackFaultHandler,
                options.CancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<TaskObservationResult> ObserveCoreAsync<T>(
        Task<T> source,
        TaskObservationOptions<T> options,
        Func<Exception, CancellationToken, ValueTask> fallbackFaultHandler)
    {
        try
        {
            T result = await source.ConfigureAwait(false);
            return await HandleSuccessAsync(result, options).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (source.IsCanceled)
        {
            return await HandleCancellationAsync(
                exception,
                options.CancellationHandlers,
                options.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return await HandleFaultAsync(
                exception,
                options.FaultHandlers,
                fallbackFaultHandler,
                options.CancellationToken).ConfigureAwait(false);
        }
    }

    private static async ValueTask<TaskObservationResult> HandleSuccessAsync(TaskObservationOptions options)
    {
        if (options.SuccessHandlers.Count == 0)
            return new TaskObservationResult(TaskObservationOutcome.Succeeded, null, null, false);

        return await InvokeOutcomeHandlerAsync(
            TaskObservationOutcome.Succeeded,
            null,
            cancellationToken => options.SuccessHandlers[0](cancellationToken),
            options.CancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<TaskObservationResult> HandleSuccessAsync<T>(
        T result,
        TaskObservationOptions<T> options)
    {
        if (options.SuccessHandlers.Count == 0)
            return new TaskObservationResult(TaskObservationOutcome.Succeeded, null, null, false);

        try
        {
            options.CancellationToken.ThrowIfCancellationRequested();
            SuccessHandlerRegistration<T>? fallback = null;
            foreach (SuccessHandlerRegistration<T> registration in options.SuccessHandlers)
            {
                if (registration.Predicate is null)
                {
                    fallback ??= registration;
                    continue;
                }

                if (registration.Predicate(result))
                    return await InvokeOutcomeHandlerAsync(
                        TaskObservationOutcome.Succeeded,
                        null,
                        cancellationToken => registration.Handler(result, cancellationToken),
                        options.CancellationToken).ConfigureAwait(false);
            }

            return fallback is null
                ? new TaskObservationResult(TaskObservationOutcome.Succeeded, null, null, false)
                : await InvokeOutcomeHandlerAsync(
                    TaskObservationOutcome.Succeeded,
                    null,
                    cancellationToken => fallback.Handler(result, cancellationToken),
                    options.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return new TaskObservationResult(TaskObservationOutcome.Succeeded, null, exception, false);
        }
    }

    private static async ValueTask<TaskObservationResult> HandleCancellationAsync(
        OperationCanceledException sourceException,
        List<Func<OperationCanceledException, CancellationToken, ValueTask>> handlers,
        CancellationToken cancellationToken)
    {
        if (handlers.Count == 0)
            return new TaskObservationResult(
                TaskObservationOutcome.Canceled,
                sourceException,
                null,
                false);

        return await InvokeOutcomeHandlerAsync(
            TaskObservationOutcome.Canceled,
            sourceException,
            token => handlers[0](sourceException, token),
            cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<TaskObservationResult> HandleFaultAsync(
        Exception sourceException,
        IReadOnlyList<FaultHandlerRegistration> handlers,
        Func<Exception, CancellationToken, ValueTask> fallbackHandler,
        CancellationToken cancellationToken)
    {
        Exception? predicateException = null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (FaultHandlerRegistration registration in handlers)
            {
                bool matches;
                try
                {
                    matches = registration.Predicate(sourceException);
                }
                catch (Exception exception)
                {
                    predicateException = exception;
                    break;
                }

                if (matches)
                {
                    TaskObservationResult result = await InvokeOutcomeHandlerAsync(
                        TaskObservationOutcome.Faulted,
                        sourceException,
                        token => registration.Handler(sourceException, token),
                        cancellationToken).ConfigureAwait(false);
                    return result;
                }
            }
        }
        catch (Exception exception)
        {
            return new TaskObservationResult(
                TaskObservationOutcome.Faulted,
                sourceException,
                exception,
                false);
        }

        TaskObservationResult fallbackResult = await InvokeOutcomeHandlerAsync(
            TaskObservationOutcome.Faulted,
            sourceException,
            token => fallbackHandler(sourceException, token),
            cancellationToken).ConfigureAwait(false);

        if (predicateException is null) return fallbackResult;

        Exception observerException = fallbackResult.ObserverException is null
            ? predicateException
            : new AggregateException(predicateException, fallbackResult.ObserverException);

        return fallbackResult with { ObserverException = observerException };
    }

    private static async ValueTask<TaskObservationResult> InvokeOutcomeHandlerAsync(
        TaskObservationOutcome outcome,
        Exception? sourceException,
        Func<CancellationToken, ValueTask> handler,
        CancellationToken cancellationToken)
    {
        var callbackInvoked = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            callbackInvoked = true;
            await handler(cancellationToken).ConfigureAwait(false);
            return new TaskObservationResult(outcome, sourceException, null, true);
        }
        catch (Exception exception)
        {
            return new TaskObservationResult(outcome, sourceException, exception, callbackInvoked);
        }
    }

    private static ValueTask InvokeAsync(
        Action<Exception> handler,
        Exception exception,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        handler(exception);
        return ValueTask.CompletedTask;
    }
}
