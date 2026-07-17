// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.BackgroundWork.Tests;

public sealed class DebouncedTaskRunnerTests
{
    [Test]
    public async Task OneRequestRunsAfterQuietPeriod()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var executions = 0;
        await using DebouncedTaskRunner runner = CreateRunner(time, _ =>
        {
            executions++;
            return ValueTask.CompletedTask;
        });

        Task drain = runner.RequestAsync();
        await TestAsync.UntilAsync(() => time.ScheduledTimerCount == 1);
        executions.ShouldBe(0);

        time.Advance(TimeSpan.FromSeconds(1));
        await drain;

        executions.ShouldBe(1);
    }

    [Test]
    public async Task NewRequestRestartsQuietPeriod()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var executions = 0;
        await using DebouncedTaskRunner runner = CreateRunner(time, _ =>
        {
            executions++;
            return ValueTask.CompletedTask;
        });

        Task drain = runner.RequestAsync();
        await TestAsync.UntilAsync(() => time.ScheduledTimerCount == 1);
        time.Advance(TimeSpan.FromMilliseconds(500));
        _ = runner.RequestAsync();
        time.Advance(TimeSpan.FromMilliseconds(500));
        await Task.Yield();

        executions.ShouldBe(0);
        await TestAsync.UntilAsync(() => time.ScheduledTimerCount == 1);
        time.Advance(TimeSpan.FromMilliseconds(500));
        await drain;

        executions.ShouldBe(1);
    }

    [Test]
    public async Task BurstProducesOneOperation()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var executions = 0;
        await using DebouncedTaskRunner runner = CreateRunner(time, _ =>
        {
            executions++;
            return ValueTask.CompletedTask;
        });

        Task[] requests = Enumerable.Range(0, 100).Select(_ => runner.RequestAsync()).ToArray();
        await TestAsync.UntilAsync(() => time.ScheduledTimerCount == 1);
        time.Advance(TimeSpan.FromSeconds(1));
        await Task.WhenAll(requests);

        executions.ShouldBe(1);
    }

    [Test]
    public async Task RequestDuringExecutionSchedulesLaterDebouncedOperation()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var firstRunStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstRun = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executions = 0;

        await using DebouncedTaskRunner runner = CreateRunner(time, async _ =>
        {
            var execution = Interlocked.Increment(ref executions);
            if (execution == 1)
            {
                firstRunStarted.SetResult();
                await releaseFirstRun.Task;
            }
        });

        Task drain = runner.RequestAsync();
        await TestAsync.UntilAsync(() => time.ScheduledTimerCount == 1);
        time.Advance(TimeSpan.FromSeconds(1));
        await firstRunStarted.Task;
        _ = runner.RequestAsync();
        releaseFirstRun.SetResult();

        await TestAsync.UntilAsync(() => time.ScheduledTimerCount == 1);
        executions.ShouldBe(1);
        time.Advance(TimeSpan.FromSeconds(1));
        await drain;

        executions.ShouldBe(2);
    }

    [Test]
    public async Task RequestThatAlreadyWaitedQuietPeriodRunsImmediatelyAfterCurrentOperation()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var firstRunStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstRun = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executions = 0;

        await using DebouncedTaskRunner runner = CreateRunner(time, async _ =>
        {
            var execution = Interlocked.Increment(ref executions);
            if (execution == 1)
            {
                firstRunStarted.SetResult();
                await releaseFirstRun.Task;
            }
        });

        Task drain = runner.RequestAsync();
        await TestAsync.UntilAsync(() => time.ScheduledTimerCount == 1);
        time.Advance(TimeSpan.FromSeconds(1));
        await firstRunStarted.Task;

        _ = runner.RequestAsync();
        time.Advance(TimeSpan.FromSeconds(1));
        releaseFirstRun.SetResult();
        await drain;

        executions.ShouldBe(2);
    }

    [Test]
    public async Task CallerCancellationDoesNotCancelSharedDebounce()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        using var callerCancellation = new CancellationTokenSource();
        var executions = 0;
        await using DebouncedTaskRunner runner = CreateRunner(time, _ =>
        {
            executions++;
            return ValueTask.CompletedTask;
        });

        Task shared = runner.RequestAsync();
        Task canceledWait = runner.RequestAsync(callerCancellation.Token);
        await callerCancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(async () => await canceledWait);
        await TestAsync.UntilAsync(() => time.ScheduledTimerCount == 1);
        time.Advance(TimeSpan.FromSeconds(1));
        await shared;

        executions.ShouldBe(1);
    }

    [Test]
    public async Task StopCancelsPendingDelayAndRejectsLaterRequests()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        await using DebouncedTaskRunner runner = CreateRunner(time, _ => ValueTask.CompletedTask);

        Task drain = runner.RequestAsync();
        await TestAsync.UntilAsync(() => time.ScheduledTimerCount == 1);
        await runner.StopAsync();

        await Should.ThrowAsync<OperationCanceledException>(async () => await drain);
        Should.Throw<ObjectDisposedException>(() => runner.RequestAsync());
    }

    [Test]
    public async Task ContinueFailurePolicyAllowsLaterRequest()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var executions = 0;
        var failures = 0;
        await using var runner = new DebouncedTaskRunner(
            _ =>
            {
                var execution = Interlocked.Increment(ref executions);
                return execution == 1
                    ? ValueTask.FromException(new InvalidOperationException("first"))
                    : ValueTask.CompletedTask;
            },
            new DebouncedTaskRunnerOptions
            {
                QuietPeriod = TimeSpan.FromSeconds(1),
                TimeProvider = time,
                FailurePolicy = BackgroundWorkFailurePolicy.Continue,
                OnFailure = (_, _) =>
                {
                    failures++;
                    return ValueTask.CompletedTask;
                }
            });

        Task first = runner.RequestAsync();
        await TestAsync.UntilAsync(() => time.ScheduledTimerCount == 1);
        time.Advance(TimeSpan.FromSeconds(1));
        await first;

        Task second = runner.RequestAsync();
        await TestAsync.UntilAsync(() => time.ScheduledTimerCount == 1);
        time.Advance(TimeSpan.FromSeconds(1));
        await second;

        executions.ShouldBe(2);
        failures.ShouldBe(1);
    }

    [Test]
    public void InvalidOptionsAreRejected()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new DebouncedTaskRunner(
            _ => ValueTask.CompletedTask,
            new DebouncedTaskRunnerOptions { QuietPeriod = TimeSpan.Zero }));

        Should.Throw<ArgumentException>(() => new DebouncedTaskRunner(
            _ => ValueTask.CompletedTask,
            new DebouncedTaskRunnerOptions
            {
                QuietPeriod = TimeSpan.FromSeconds(1),
                FailurePolicy = BackgroundWorkFailurePolicy.Continue
            }));
    }

    private static DebouncedTaskRunner CreateRunner(
        TimeProvider timeProvider,
        Func<CancellationToken, ValueTask> operation) =>
        new(
            operation,
            new DebouncedTaskRunnerOptions
            {
                QuietPeriod = TimeSpan.FromSeconds(1),
                TimeProvider = timeProvider
            });
}
