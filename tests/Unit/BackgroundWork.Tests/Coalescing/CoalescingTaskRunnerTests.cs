// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.BackgroundWork.Coalescing;
using Pocok.BackgroundWork.FailureHandling;
using Pocok.BackgroundWork.Tests.TestSupport;

namespace Pocok.BackgroundWork.Tests.Coalescing;

public sealed class CoalescingTaskRunnerTests
{
    [Test]
    public async Task IdleRequestExecutesOnce()
    {
        var executions = 0;
        await using var runner = new CoalescingTaskRunner(_ =>
        {
            executions++;
            return ValueTask.CompletedTask;
        });

        await runner.RequestAsync();

        executions.ShouldBe(1);
        runner.IsRunning.ShouldBeFalse();
    }

    [Test]
    public async Task RequestsDuringExecutionCollapseIntoOneRerun()
    {
        var firstRunStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstRun = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executions = 0;

        await using var runner = new CoalescingTaskRunner(async _ =>
        {
            var execution = Interlocked.Increment(ref executions);
            if (execution == 1)
            {
                firstRunStarted.SetResult();
                await releaseFirstRun.Task;
            }
        });

        Task first = runner.RequestAsync();
        await firstRunStarted.Task;

        Task second = runner.RequestAsync();
        Task third = runner.RequestAsync();
        Task fourth = runner.RequestAsync();

        releaseFirstRun.SetResult();
        await Task.WhenAll(first, second, third, fourth);

        executions.ShouldBe(2);
    }

    [Test]
    public async Task OperationsNeverOverlap()
    {
        var firstRunStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstRun = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var active = 0;
        var maximumActive = 0;
        var executions = 0;

        await using var runner = new CoalescingTaskRunner(async _ =>
        {
            var currentActive = Interlocked.Increment(ref active);
            maximumActive = Math.Max(maximumActive, currentActive);
            var execution = Interlocked.Increment(ref executions);
            try
            {
                if (execution == 1)
                {
                    firstRunStarted.SetResult();
                    await releaseFirstRun.Task;
                }
            }
            finally
            {
                Interlocked.Decrement(ref active);
            }
        });

        Task drain = runner.RequestAsync();
        await firstRunStarted.Task;
        Task[] requests = Enumerable.Range(0, 100).Select(_ => runner.RequestAsync()).ToArray();
        releaseFirstRun.SetResult();

        await Task.WhenAll(requests.Append(drain));

        executions.ShouldBe(2);
        maximumActive.ShouldBe(1);
    }

    [Test]
    public async Task MinimumIntervalUsesConfiguredTimeProvider()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var firstRunStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstRun = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executions = 0;

        await using var runner = new CoalescingTaskRunner(
            async _ =>
            {
                var execution = Interlocked.Increment(ref executions);
                if (execution == 1)
                {
                    firstRunStarted.SetResult();
                    await releaseFirstRun.Task;
                }
            },
            new CoalescingTaskRunnerOptions
            {
                MinimumInterval = TimeSpan.FromSeconds(5),
                TimeProvider = time
            });

        Task drain = runner.RequestAsync();
        await firstRunStarted.Task;
        _ = runner.RequestAsync();
        releaseFirstRun.SetResult();

        await TestAsync.UntilAsync(() => time.ScheduledTimerCount == 1);
        executions.ShouldBe(1);
        time.Advance(TimeSpan.FromSeconds(4));
        await Task.Yield();
        executions.ShouldBe(1);
        time.Advance(TimeSpan.FromSeconds(1));

        await drain;
        executions.ShouldBe(2);
    }

    [Test]
    public async Task CallerCancellationDoesNotCancelSharedWork()
    {
        var operationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseOperation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var callerCancellation = new CancellationTokenSource();
        using var sharedCancellation = new CancellationTokenSource();
        var executions = 0;

        await using var runner = new CoalescingTaskRunner(async _ =>
        {
            Interlocked.Increment(ref executions);
            operationStarted.TrySetResult();
            await releaseOperation.Task;
        });

        Task shared = runner.RequestAsync(sharedCancellation.Token);
        Task canceledWait = runner.RequestAsync(callerCancellation.Token);
        await operationStarted.Task;
        await callerCancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(async () => await canceledWait);
        shared.IsCompleted.ShouldBeFalse();
        releaseOperation.SetResult();
        await shared;

        executions.ShouldBe(2);
    }

    [Test]
    public async Task PreCanceledWaitStillQueuesSharedWork()
    {
        var operationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseOperation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var callerCancellation = new CancellationTokenSource();
        await callerCancellation.CancelAsync();
        var executions = 0;

        await using var runner = new CoalescingTaskRunner(async _ =>
        {
            Interlocked.Increment(ref executions);
            operationStarted.TrySetResult();
            await releaseOperation.Task;
        });

        Task canceledWait = runner.RequestAsync(callerCancellation.Token);
        await operationStarted.Task;

        await Should.ThrowAsync<OperationCanceledException>(async () => await canceledWait);
        runner.IsRunning.ShouldBeTrue();

        releaseOperation.SetResult();
        await TestAsync.UntilAsync(() => !runner.IsRunning);

        executions.ShouldBe(1);
    }

    [Test]
    public async Task MinimumIntervalIsSkippedWithoutPendingRerun()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var executions = 0;
        await using var runner = new CoalescingTaskRunner(
            _ =>
            {
                executions++;
                return ValueTask.CompletedTask;
            },
            new CoalescingTaskRunnerOptions
            {
                MinimumInterval = TimeSpan.FromMinutes(1),
                TimeProvider = time
            });

        await runner.RequestAsync();

        executions.ShouldBe(1);
        time.ScheduledTimerCount.ShouldBe(0);
    }

    [Test]
    public async Task StopDuringMinimumIntervalCancelsPendingRerun()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var firstRunStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstRun = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executions = 0;

        await using var runner = new CoalescingTaskRunner(
            async _ =>
            {
                var execution = Interlocked.Increment(ref executions);
                if (execution == 1)
                {
                    firstRunStarted.TrySetResult();
                    await releaseFirstRun.Task;
                }
            },
            new CoalescingTaskRunnerOptions
            {
                MinimumInterval = TimeSpan.FromMinutes(1),
                TimeProvider = time
            });

        Task drain = runner.RequestAsync();
        await firstRunStarted.Task;
        _ = runner.RequestAsync();
        releaseFirstRun.SetResult();
        await TestAsync.UntilAsync(() => time.ScheduledTimerCount == 1);

        await runner.StopAsync();

        await Should.ThrowAsync<OperationCanceledException>(async () => await drain);
        executions.ShouldBe(1);
        runner.IsRunning.ShouldBeFalse();
    }

    [Test]
    public async Task StopCancelsActiveWorkAndRejectsLaterRequests()
    {
        var operationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var runner = new CoalescingTaskRunner(async cancellationToken =>
        {
            operationStarted.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        });

        Task drain = runner.RequestAsync();
        await operationStarted.Task;
        await runner.StopAsync();

        await Should.ThrowAsync<OperationCanceledException>(async () => await drain);
        Should.Throw<ObjectDisposedException>(() => runner.RequestAsync());
        runner.IsRunning.ShouldBeFalse();
    }

    [Test]
    public async Task StopFailurePolicyFaultsDrainAndStopsRunner()
    {
        var failure = new InvalidOperationException("operation");
        await using var runner = new CoalescingTaskRunner(_ => ValueTask.FromException(failure));

        InvalidOperationException thrown =
            await Should.ThrowAsync<InvalidOperationException>(async () => await runner.RequestAsync());

        thrown.ShouldBeSameAs(failure);
        Should.Throw<ObjectDisposedException>(() => runner.RequestAsync());
    }

    [Test]
    public async Task ContinueFailurePolicyHandlesFailureAndServicesPendingRerun()
    {
        var firstRunStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstRun = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executions = 0;
        var failures = 0;

        await using var runner = new CoalescingTaskRunner(
            async _ =>
            {
                var execution = Interlocked.Increment(ref executions);
                if (execution == 1)
                {
                    firstRunStarted.SetResult();
                    await releaseFirstRun.Task;
                    throw new InvalidOperationException("first");
                }
            },
            new CoalescingTaskRunnerOptions
            {
                FailurePolicy = BackgroundWorkFailurePolicy.Continue,
                OnFailure = (_, _) =>
                {
                    failures++;
                    return ValueTask.CompletedTask;
                }
            });

        Task drain = runner.RequestAsync();
        await firstRunStarted.Task;
        _ = runner.RequestAsync();
        releaseFirstRun.SetResult();
        await drain;

        executions.ShouldBe(2);
        failures.ShouldBe(1);
    }

    [Test]
    public async Task ContinuePolicyKeepsRunnerReusableAfterHandledFailure()
    {
        var executions = 0;
        var failures = 0;
        await using var runner = new CoalescingTaskRunner(
            _ =>
            {
                if (Interlocked.Increment(ref executions) == 1)
                    return ValueTask.FromException(new InvalidOperationException("first"));

                return ValueTask.CompletedTask;
            },
            new CoalescingTaskRunnerOptions
            {
                FailurePolicy = BackgroundWorkFailurePolicy.Continue,
                OnFailure = (_, _) =>
                {
                    failures++;
                    return ValueTask.CompletedTask;
                }
            });

        await runner.RequestAsync();
        await runner.RequestAsync();

        executions.ShouldBe(2);
        failures.ShouldBe(1);
        runner.IsRunning.ShouldBeFalse();
    }

    [Test]
    public async Task FailureHandlerFailureIsAggregated()
    {
        await using var runner = new CoalescingTaskRunner(
            _ => ValueTask.FromException(new InvalidOperationException("operation")),
            new CoalescingTaskRunnerOptions
            {
                FailurePolicy = BackgroundWorkFailurePolicy.Continue,
                OnFailure = (_, _) => ValueTask.FromException(new FormatException("handler"))
            });

        AggregateException exception =
            await Should.ThrowAsync<AggregateException>(async () => await runner.RequestAsync());

        exception.InnerExceptions.Count.ShouldBe(2);
    }

    [Test]
    public void ContinuePolicyRequiresFailureHandler()
    {
        Should.Throw<ArgumentException>(() => new CoalescingTaskRunner(
            _ => ValueTask.CompletedTask,
            new CoalescingTaskRunnerOptions
            {
                FailurePolicy = BackgroundWorkFailurePolicy.Continue
            }));
    }

    [Test]
    public async Task DisposalIsIdempotent()
    {
        var runner = new CoalescingTaskRunner(_ => ValueTask.CompletedTask);

        await runner.DisposeAsync();
        await runner.DisposeAsync();

        Should.Throw<ObjectDisposedException>(() => runner.RequestAsync());
    }
}
