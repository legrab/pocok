// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.IO;

namespace Pocok.BackgroundWork.Tests;

public sealed class TaskObservationTests
{
    [Test]
    public async Task SuccessCallsConfiguredHandler()
    {
        var called = false;

        TaskObservation observation = Task.CompletedTask.Observe(
            onFault: _ => Assert.Fail("Fault fallback should not run."),
            configure: options => options.OnSuccess(() => called = true));

        TaskObservationResult result = await observation.Completion;

        called.ShouldBeTrue();
        result.Outcome.ShouldBe(TaskObservationOutcome.Succeeded);
        result.SourceException.ShouldBeNull();
        result.ObserverException.ShouldBeNull();
        result.CallbackInvoked.ShouldBeTrue();
        observation.Completion.IsFaulted.ShouldBeFalse();
    }

    [Test]
    public async Task FaultCallsMandatoryFallback()
    {
        Exception? observed = null;
        var sourceException = new InvalidOperationException("source");

        TaskObservation observation = Task.FromException(sourceException).Observe(
            onFault: exception => observed = exception);

        TaskObservationResult result = await observation.Completion;

        observed.ShouldBeSameAs(sourceException);
        result.Outcome.ShouldBe(TaskObservationOutcome.Faulted);
        result.SourceException.ShouldBeSameAs(sourceException);
        result.ObserverException.ShouldBeNull();
        result.CallbackInvoked.ShouldBeTrue();
    }

    [Test]
    public async Task CancellationCallsConfiguredHandlerAndPreservesSourceException()
    {
        using var sourceCancellation = new CancellationTokenSource();
        await sourceCancellation.CancelAsync();
        OperationCanceledException? observed = null;

        TaskObservation observation = Task.FromCanceled(sourceCancellation.Token).Observe(
            onFault: _ => Assert.Fail("Fault fallback should not run."),
            configure: options => options.OnCanceled(exception => observed = exception));

        TaskObservationResult result = await observation.Completion;

        observed.ShouldNotBeNull();
        observed.CancellationToken.ShouldBe(sourceCancellation.Token);
        result.Outcome.ShouldBe(TaskObservationOutcome.Canceled);
        result.SourceException.ShouldBeOfType<TaskCanceledException>();
        result.ObserverException.ShouldBeNull();
    }


    [Test]
    public async Task FaultedOperationCanceledExceptionRemainsFaulted()
    {
        var fallbackCalled = false;

        TaskObservation observation = Task.FromException(new OperationCanceledException("faulted")).Observe(
            onFault: _ => fallbackCalled = true,
            configure: options => options.OnCanceled(_ => Assert.Fail("Cancellation handler should not run.")));

        TaskObservationResult result = await observation.Completion;

        fallbackCalled.ShouldBeTrue();
        result.Outcome.ShouldBe(TaskObservationOutcome.Faulted);
        result.SourceException.ShouldBeOfType<OperationCanceledException>();
    }

    [Test]
    public async Task GenericObservationDeliversFirstMatchingResult()
    {
        int? observed = null;
        var fallbackCalled = false;

        TaskObservation observation = Task.FromResult(42).Observe(
            onFault: _ => Assert.Fail("Fault fallback should not run."),
            configure: options => options
                .OnSuccess(value => value < 0, _ => Assert.Fail("Predicate should not match."))
                .OnSuccess(value => value == 42, value => observed = value)
                .OnSuccess(_ => fallbackCalled = true));

        TaskObservationResult result = await observation.Completion;

        observed.ShouldBe(42);
        fallbackCalled.ShouldBeFalse();
        result.Outcome.ShouldBe(TaskObservationOutcome.Succeeded);
        result.ObserverException.ShouldBeNull();
    }

    [Test]
    public async Task FilteredSuccessHandlersRunBeforeAnEarlierFallback()
    {
        int? observed = null;
        var fallbackCalled = false;

        TaskObservation observation = Task.FromResult(42).Observe(
            onFault: _ => Assert.Fail("Fault fallback should not run."),
            configure: options => options
                .OnSuccess(_ => fallbackCalled = true)
                .OnSuccess(value => value == 42, value => observed = value));

        TaskObservationResult result = await observation.Completion;

        observed.ShouldBe(42);
        fallbackCalled.ShouldBeFalse();
        result.ObserverException.ShouldBeNull();
    }

    [Test]
    public async Task TypedFaultHandlerRunsBeforeFallback()
    {
        var typedCalled = false;
        var fallbackCalled = false;

        TaskObservation observation = Task.FromException(new IOException("broken")).Observe(
            onFault: _ => fallbackCalled = true,
            configure: options => options.OnFault<IOException>(_ => typedCalled = true));

        TaskObservationResult result = await observation.Completion;

        typedCalled.ShouldBeTrue();
        fallbackCalled.ShouldBeFalse();
        result.CallbackInvoked.ShouldBeTrue();
    }

    [Test]
    public async Task UnmatchedFilteredFaultUsesFallback()
    {
        var fallbackCalled = false;

        TaskObservation observation = Task.FromException(new InvalidOperationException("source")).Observe(
            onFault: _ => fallbackCalled = true,
            configure: options => options.OnFault<IOException>(_ => Assert.Fail("Handler should not run.")));

        TaskObservationResult result = await observation.Completion;

        fallbackCalled.ShouldBeTrue();
        result.ObserverException.ShouldBeNull();
    }

    [Test]
    public async Task PredicateFailureFallsBackAndIsReported()
    {
        var fallbackCalled = false;

        TaskObservation observation = Task.FromException(new InvalidOperationException("source")).Observe(
            onFault: _ => fallbackCalled = true,
            configure: options => options.OnFault(
                _ => throw new FormatException("predicate"),
                _ => Assert.Fail("Handler should not run.")));

        TaskObservationResult result = await observation.Completion;

        fallbackCalled.ShouldBeTrue();
        result.SourceException.ShouldBeOfType<InvalidOperationException>();
        result.ObserverException.ShouldBeOfType<FormatException>();
    }

    [Test]
    public async Task PredicateAndFallbackFailuresAreAggregated()
    {
        TaskObservation observation = Task.FromException(new InvalidOperationException("source")).Observe(
            onFault: _ => throw new IOException("fallback"),
            configure: options => options.OnFault(
                _ => throw new FormatException("predicate"),
                _ => Assert.Fail("Handler should not run.")));

        TaskObservationResult result = await observation.Completion;

        AggregateException aggregate = result.ObserverException.ShouldBeOfType<AggregateException>();
        aggregate.InnerExceptions.Count.ShouldBe(2);
        aggregate.InnerExceptions[0].ShouldBeOfType<FormatException>();
        aggregate.InnerExceptions[1].ShouldBeOfType<IOException>();
        observation.Completion.IsFaulted.ShouldBeFalse();
    }

    [Test]
    public async Task HandlerFailureIsCapturedInsteadOfFaultingCompletion()
    {
        TaskObservation observation = Task.CompletedTask.Observe(
            onFault: _ => { },
            configure: options => options.OnSuccess(
                () => throw new InvalidOperationException("handler")));

        TaskObservationResult result = await observation.Completion;

        result.Outcome.ShouldBe(TaskObservationOutcome.Succeeded);
        result.ObserverException.ShouldBeOfType<InvalidOperationException>();
        result.CallbackInvoked.ShouldBeTrue();
        observation.Completion.IsFaulted.ShouldBeFalse();
    }

    [Test]
    public async Task AsynchronousHandlersAreAwaited()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = false;

        TaskObservation observation = Task.CompletedTask.Observe(
            onFault: _ => { },
            configure: options => options.OnSuccess(async _ =>
            {
                await release.Task;
                completed = true;
            }));

        observation.Completion.IsCompleted.ShouldBeFalse();
        release.SetResult();
        await observation.Completion;

        completed.ShouldBeTrue();
    }

    [Test]
    public async Task ObservationCancellationDoesNotCancelSourceTask()
    {
        using var observerCancellation = new CancellationTokenSource();
        var sourceCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var successCalled = false;

        TaskObservation observation = sourceCompletion.Task.Observe(
            onFault: _ => { },
            configure: options =>
            {
                options.CancellationToken = observerCancellation.Token;
                options.OnSuccess(() => successCalled = true);
            });

        await observerCancellation.CancelAsync();
        sourceCompletion.SetResult();

        TaskObservationResult result = await observation.Completion;

        sourceCompletion.Task.IsCompletedSuccessfully.ShouldBeTrue();
        successCalled.ShouldBeFalse();
        result.Outcome.ShouldBe(TaskObservationOutcome.Succeeded);
        result.ObserverException.ShouldBeOfType<OperationCanceledException>();
        result.CallbackInvoked.ShouldBeFalse();
    }

    [Test]
    public async Task CancellationDuringAsynchronousHandlerIsReported()
    {
        using var observerCancellation = new CancellationTokenSource();
        var handlerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        TaskObservation observation = Task.CompletedTask.Observe(
            onFault: _ => { },
            configure: options =>
            {
                options.CancellationToken = observerCancellation.Token;
                options.OnSuccess(async cancellationToken =>
                {
                    handlerStarted.SetResult();
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                });
            });

        await handlerStarted.Task;
        await observerCancellation.CancelAsync();
        TaskObservationResult result = await observation.Completion;

        result.Outcome.ShouldBe(TaskObservationOutcome.Succeeded);
        result.ObserverException.ShouldBeOfType<TaskCanceledException>();
        result.CallbackInvoked.ShouldBeTrue();
    }

    [Test]
    public void NullArgumentsAreRejectedSynchronously()
    {
        Task nullTask = null!;

        Should.Throw<ArgumentNullException>(() => nullTask.Observe(_ => { }));
        Should.Throw<ArgumentNullException>(() => Task.CompletedTask.Observe((Action<Exception>)null!));
        Should.Throw<ArgumentNullException>(() => Task.CompletedTask.Observe(
            _ => { },
            options => options.OnSuccess((Action)null!)));
    }
}
