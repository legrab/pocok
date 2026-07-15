// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Readiness.Tests;

public sealed class ReadinessConcurrencyTests
{
    [Test]
    public async Task ConcurrentWaitersObserveOneReadyTransition()
    {
        var source = new ReadinessSource();
        ReadinessCycle cycle = source.BeginStartup();
        Task[] waiters = [.. Enumerable.Range(0, 256).Select(_ => Task.Run(() => source.WaitUntilReadyAsync(cycle)))];

        source.MarkReady(cycle);

        await Task.WhenAll(waiters);
        source.Snapshot.ShouldBe(new ReadinessSnapshot(ReadinessState.Ready, cycle.Sequence, null));
    }

    [Test]
    public async Task CancellingManyCallersDoesNotCancelSharedReadiness()
    {
        var source = new ReadinessSource();
        ReadinessCycle cycle = source.BeginStartup();
        using var cancellation = new CancellationTokenSource();
        Task[] cancelled =
            [.. Enumerable.Range(0, 64).Select(_ => source.WaitUntilReadyAsync(cycle, cancellation.Token))];
        Task[] survivors =
            [.. Enumerable.Range(0, 64).Select(_ => source.WaitUntilReadyAsync(cycle, cancellation.Token))];

        await cancellation.CancelAsync();
        foreach (Task waiter in cancelled)
            await Should.ThrowAsync<OperationCanceledException>(async () => await waiter);

        survivors.ShouldAllBe(waiter => !waiter.IsCompleted);
        source.MarkReady(cycle);
        await Task.WhenAll(survivors);
    }

    [Test]
    public async Task ShutdownAndStaleCompletionRaceHasOneValidWinner()
    {
        for (var attempt = 0; attempt < 128; attempt++)
        {
            var source = new ReadinessSource();
            ReadinessCycle cycle = source.BeginStartup();
            Task waiter = source.WaitUntilReadyAsync(cycle);
            using var barrier = new Barrier(2);

            var ready = Task.Run(() =>
            {
                barrier.SignalAndWait();
                try
                {
                    source.MarkReady(cycle);
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            });

            var stopping = Task.Run(() =>
            {
                barrier.SignalAndWait();
                try
                {
                    source.BeginShutdown();
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            });

            var outcomes = await Task.WhenAll(ready, stopping);
            outcomes.Count(value => value).ShouldBe(1);

            if (ready.Result)
            {
                await waiter;
                source.State.ShouldBe(ReadinessState.Ready);
            }
            else
            {
                await Should.ThrowAsync<ReadinessStoppedException>(async () => await waiter);
                source.State.ShouldBe(ReadinessState.Stopping);
            }
        }
    }

    [Test]
    public async Task FailurePublishesAtomicSnapshotBeforeWaitersResume()
    {
        var source = new ReadinessSource();
        ReadinessCycle cycle = source.BeginStartup();
        var failure = new ReadinessFailure("readiness.start.failed", "Startup failed.");
        Task<ReadinessSnapshot>[] snapshots =
        [
            .. Enumerable.Range(0, 64)
                .Select(_ => source.WaitUntilReadyAsync(cycle).ContinueWith(
                    _ => source.Snapshot,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default))
        ];

        source.MarkFailed(cycle, failure);
        await Task.WhenAll(snapshots);

        snapshots.ShouldAllBe(task =>
            task.Result.State == ReadinessState.Failed &&
            ReferenceEquals(task.Result.Failure, failure));
    }

    [Test]
    public void SnapshotReadsRemainInternallyConsistentDuringRestartLoop()
    {
        var source = new ReadinessSource();
        var invalidSnapshotObserved = false;
        using var cancellation = new CancellationTokenSource();
        var reader = Task.Run(() =>
        {
            while (!cancellation.IsCancellationRequested)
            {
                ReadinessSnapshot snapshot = source.Snapshot;
                if (snapshot.State == ReadinessState.Failed != snapshot.Failure is not null)
                {
                    invalidSnapshotObserved = true;
                    return;
                }
            }
        });

        for (var attempt = 0; attempt < 128; attempt++)
        {
            ReadinessCycle cycle = source.BeginStartup();
            source.MarkReady(cycle);
            source.BeginShutdown();
            source.MarkStopped();
        }

        cancellation.Cancel();
        reader.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();
        invalidSnapshotObserved.ShouldBeFalse();
    }
}
