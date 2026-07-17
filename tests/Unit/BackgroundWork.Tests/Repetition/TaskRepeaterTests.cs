// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.BackgroundWork.FailureHandling;
using Pocok.BackgroundWork.Repetition;
using Pocok.BackgroundWork.Tests.TestSupport;

namespace Pocok.BackgroundWork.Tests.Repetition;

public sealed class TaskRepeaterTests
{
    [Test]
    public async Task MaximumIterationsStopsTheLoop()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var executions = 0;

        Task repeat = TaskRepeater.RepeatAsync(
            _ =>
            {
                executions++;
                return ValueTask.CompletedTask;
            },
            new TaskRepeaterOptions
            {
                Interval = TimeSpan.FromSeconds(1),
                MaximumIterations = 3,
                TimeProvider = time
            });

        await TestAsync.AdvanceUntilCompletedAsync(time, repeat, TimeSpan.FromSeconds(1));

        executions.ShouldBe(3);
    }

    [Test]
    public async Task InitialDelayPrecedesFirstIteration()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var executions = 0;

        Task repeat = TaskRepeater.RepeatAsync(
            _ =>
            {
                executions++;
                return ValueTask.CompletedTask;
            },
            new TaskRepeaterOptions
            {
                InitialDelay = TimeSpan.FromSeconds(2),
                Interval = TimeSpan.FromSeconds(1),
                MaximumIterations = 1,
                TimeProvider = time
            });

        await TestAsync.UntilAsync(() => time.ScheduledTimerCount == 1);
        executions.ShouldBe(0);
        time.Advance(TimeSpan.FromSeconds(1));
        await Task.Yield();
        executions.ShouldBe(0);
        time.Advance(TimeSpan.FromSeconds(1));
        await repeat;

        executions.ShouldBe(1);
    }

    [Test]
    public async Task IntervalIsAppliedAfterOperationCompletion()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var firstRunStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstRun = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executions = 0;

        Task repeat = TaskRepeater.RepeatAsync(
            async _ =>
            {
                var execution = Interlocked.Increment(ref executions);
                if (execution == 1)
                {
                    firstRunStarted.SetResult();
                    await releaseFirstRun.Task;
                }
            },
            new TaskRepeaterOptions
            {
                Interval = TimeSpan.FromSeconds(1),
                MaximumIterations = 2,
                TimeProvider = time
            });

        await firstRunStarted.Task;
        time.Advance(TimeSpan.FromSeconds(10));
        releaseFirstRun.SetResult();
        await TestAsync.UntilAsync(() => time.ScheduledTimerCount == 1);
        executions.ShouldBe(1);
        time.Advance(TimeSpan.FromSeconds(1));
        await repeat;

        executions.ShouldBe(2);
    }

    [Test]
    public async Task ShouldContinueCanPreventFirstIteration()
    {
        var executions = 0;

        await TaskRepeater.RepeatAsync(
            _ =>
            {
                executions++;
                return ValueTask.CompletedTask;
            },
            new TaskRepeaterOptions
            {
                Interval = TimeSpan.FromSeconds(1),
                ShouldContinue = () => false
            });

        executions.ShouldBe(0);
    }

    [Test]
    public async Task CancellationDuringOperationRemainsCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failureCalled = false;

        Task repeat = TaskRepeater.RepeatAsync(
            async cancellationToken =>
            {
                started.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            },
            new TaskRepeaterOptions
            {
                Interval = TimeSpan.FromSeconds(1),
                FailurePolicy = BackgroundWorkFailurePolicy.Continue,
                OnFailure = (_, _) =>
                {
                    failureCalled = true;
                    return ValueTask.CompletedTask;
                }
            },
            cancellation.Token);

        await started.Task;
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(async () => await repeat);
        failureCalled.ShouldBeFalse();
    }

    [Test]
    public async Task CancellationAfterIgnoringOperationIsStillObserved()
    {
        using var cancellation = new CancellationTokenSource();

        Task repeat = TaskRepeater.RepeatAsync(
            async _ => { await cancellation.CancelAsync(); },
            new TaskRepeaterOptions
            {
                Interval = TimeSpan.FromSeconds(1),
                MaximumIterations = 1
            },
            cancellation.Token);

        await Should.ThrowAsync<OperationCanceledException>(async () => await repeat);
    }

    [Test]
    public async Task StopFailurePolicyPropagatesOperationFailure()
    {
        var failure = new InvalidOperationException("operation");

        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await TaskRepeater.RepeatAsync(
                _ => ValueTask.FromException(failure),
                new TaskRepeaterOptions
                {
                    Interval = TimeSpan.FromSeconds(1),
                    MaximumIterations = 1
                }));

        thrown.ShouldBeSameAs(failure);
    }

    [Test]
    public async Task ContinueFailurePolicyHandlesFailureAndContinues()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var executions = 0;
        var failures = 0;

        Task repeat = TaskRepeater.RepeatAsync(
            _ =>
            {
                var execution = Interlocked.Increment(ref executions);
                return execution == 1
                    ? ValueTask.FromException(new InvalidOperationException("first"))
                    : ValueTask.CompletedTask;
            },
            new TaskRepeaterOptions
            {
                Interval = TimeSpan.FromSeconds(1),
                MaximumIterations = 2,
                TimeProvider = time,
                FailurePolicy = BackgroundWorkFailurePolicy.Continue,
                OnFailure = (_, _) =>
                {
                    failures++;
                    return ValueTask.CompletedTask;
                }
            });

        await TestAsync.AdvanceUntilCompletedAsync(time, repeat, TimeSpan.FromSeconds(1));

        executions.ShouldBe(2);
        failures.ShouldBe(1);
    }

    [Test]
    public async Task FailureHandlerFailureIsAggregated()
    {
        AggregateException exception = await Should.ThrowAsync<AggregateException>(async () =>
            await TaskRepeater.RepeatAsync(
                _ => ValueTask.FromException(new InvalidOperationException("operation")),
                new TaskRepeaterOptions
                {
                    Interval = TimeSpan.FromSeconds(1),
                    MaximumIterations = 1,
                    FailurePolicy = BackgroundWorkFailurePolicy.Continue,
                    OnFailure = (_, _) => ValueTask.FromException(new FormatException("handler"))
                }));

        exception.InnerExceptions.Count.ShouldBe(2);
    }

    [Test]
    public void InvalidOptionsAreRejectedSynchronously()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => TaskRepeater.RepeatAsync(
            _ => ValueTask.CompletedTask,
            new TaskRepeaterOptions { Interval = TimeSpan.Zero }));

        Should.Throw<ArgumentOutOfRangeException>(() => TaskRepeater.RepeatAsync(
            _ => ValueTask.CompletedTask,
            new TaskRepeaterOptions
            {
                Interval = TimeSpan.FromSeconds(1),
                MaximumIterations = 0
            }));

        Should.Throw<ArgumentException>(() => TaskRepeater.RepeatAsync(
            _ => ValueTask.CompletedTask,
            new TaskRepeaterOptions
            {
                Interval = TimeSpan.FromSeconds(1),
                FailurePolicy = BackgroundWorkFailurePolicy.Continue
            }));
    }
}
