// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Hosting.Tests;

public sealed class ReadinessSourceTests
{
    [Test]
    public void InitialStateWaitsForFutureStartup()
    {
        var source = new ReadinessSource();

        source.State.ShouldBe(ReadinessState.Stopped);
        source.Failure.ShouldBeNull();
        source.WaitUntilReadyAsync().IsCompleted.ShouldBeFalse();
    }

    [Test]
    public async Task MarkReadyReleasesConcurrentWaiters()
    {
        var source = new ReadinessSource();
        var waiters = Enumerable.Range(0, 32)
            .Select(_ => source.WaitUntilReadyAsync())
            .ToArray();

        var cycle = source.BeginStartup();
        source.State.ShouldBe(ReadinessState.Starting);

        source.MarkReady(cycle);
        await Task.WhenAll(waiters);

        source.State.ShouldBe(ReadinessState.Ready);
        source.Failure.ShouldBeNull();
        source.WaitUntilReadyAsync().IsCompletedSuccessfully.ShouldBeTrue();
    }

    [Test]
    public async Task FailureIsStructuredAndObservable()
    {
        var source = new ReadinessSource();
        var diagnostic = new InvalidOperationException("diagnostic");
        var error = new Error("hosting.start.failed", "Startup failed.", diagnostic);
        var waiter = source.WaitUntilReadyAsync();
        var cycle = source.BeginStartup();

        source.MarkFailed(cycle, error);

        var exception = await Should.ThrowAsync<ReadinessFailedException>(async () => await waiter);
        exception.Error.ShouldBeSameAs(error);
        exception.InnerException.ShouldBeSameAs(diagnostic);
        exception.Message.ShouldBe("Startup failed.");
        source.State.ShouldBe(ReadinessState.Failed);
        source.Failure.ShouldBeSameAs(error);
        await Should.ThrowAsync<ReadinessFailedException>(async () => await source.WaitUntilReadyAsync());
    }

    [Test]
    public async Task WaiterCancellationDoesNotCancelTheSharedCycle()
    {
        var source = new ReadinessSource();
        var cycle = source.BeginStartup();
        using var cancellation = new CancellationTokenSource();
        var cancelledWaiter = source.WaitUntilReadyAsync(cancellation.Token);
        var remainingWaiter = source.WaitUntilReadyAsync();

        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(async () => await cancelledWaiter);
        remainingWaiter.IsCompleted.ShouldBeFalse();
        source.State.ShouldBe(ReadinessState.Starting);

        source.MarkReady(cycle);
        await remainingWaiter;
    }

    [Test]
    public async Task StartupCancellationPropagatesAndAllowsRestart()
    {
        var source = new ReadinessSource();
        var waiter = source.WaitUntilReadyAsync();
        var cancelledCycle = source.BeginStartup();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        source.CancelStartup(cancelledCycle, cancellation.Token);

        var exception = await Should.ThrowAsync<OperationCanceledException>(async () => await waiter);
        exception.CancellationToken.ShouldBe(cancellation.Token);
        source.State.ShouldBe(ReadinessState.Stopped);
        source.Failure.ShouldBeNull();

        var restartWaiter = source.WaitUntilReadyAsync();
        var restart = source.BeginStartup();
        restart.Sequence.ShouldBeGreaterThan(cancelledCycle.Sequence);
        source.MarkReady(restart);
        await restartWaiter;
    }

    [Test]
    public void StartupCancellationRequiresCancelledToken()
    {
        var source = new ReadinessSource();
        var cycle = source.BeginStartup();

        Should.Throw<ArgumentException>(() => source.CancelStartup(cycle, CancellationToken.None));
        source.State.ShouldBe(ReadinessState.Starting);
    }

    [Test]
    public async Task StopBeforeReadyFailsExistingWaitersAndInvalidatesCycle()
    {
        var source = new ReadinessSource();
        var cycle = source.BeginStartup();
        var waiter = source.WaitUntilReadyAsync();

        source.BeginShutdown();

        await Should.ThrowAsync<ReadinessStoppedException>(async () => await waiter);
        source.State.ShouldBe(ReadinessState.Stopping);
        Should.Throw<InvalidOperationException>(() => source.MarkReady(cycle));

        var nextCycleWaiter = source.WaitUntilReadyAsync();
        nextCycleWaiter.IsCompleted.ShouldBeFalse();
        source.MarkStopped();
        source.State.ShouldBe(ReadinessState.Stopped);

        var restart = source.BeginStartup();
        source.MarkReady(restart);
        await nextCycleWaiter;
    }

    [Test]
    public async Task ReadyStateBecomesUnavailableAsSoonAsShutdownBegins()
    {
        var source = new ReadinessSource();
        var first = source.BeginStartup();
        source.MarkReady(first);
        await source.WaitUntilReadyAsync();

        source.BeginShutdown();

        source.State.ShouldBe(ReadinessState.Stopping);
        var nextWaiter = source.WaitUntilReadyAsync();
        nextWaiter.IsCompleted.ShouldBeFalse();
        source.MarkStopped();
        nextWaiter.IsCompleted.ShouldBeFalse();

        var restart = source.BeginStartup();
        source.MarkReady(restart);
        await nextWaiter;
    }

    [Test]
    public async Task ShutdownFailureIsObservableUntilRestart()
    {
        var source = new ReadinessSource();
        var first = source.BeginStartup();
        source.MarkReady(first);
        source.BeginShutdown();
        var error = new Error("hosting.stop.failed", "Shutdown failed.");

        source.MarkShutdownFailed(error);

        source.State.ShouldBe(ReadinessState.Failed);
        source.Failure.ShouldBeSameAs(error);
        await Should.ThrowAsync<ReadinessFailedException>(async () => await source.WaitUntilReadyAsync());

        var restartWaiter = source.WaitUntilReadyAsync();
        var restart = source.BeginStartup();
        restartWaiter.IsCompleted.ShouldBeTrue();
        await Should.ThrowAsync<ReadinessFailedException>(async () => await restartWaiter);

        var activeWaiter = source.WaitUntilReadyAsync();
        source.MarkReady(restart);
        await activeWaiter;
    }

    [Test]
    public async Task TimeoutDoesNotChangeReadinessState()
    {
        var source = new ReadinessSource();
        source.BeginStartup();
        var waiter = source.WaitUntilReadyAsync();

        await Should.ThrowAsync<TimeoutException>(async () => await waiter.WaitAsync(TimeSpan.Zero));

        source.State.ShouldBe(ReadinessState.Starting);
        waiter.IsCompleted.ShouldBeFalse();
    }

    [Test]
    public void InvalidTransitionsAreRejectedWithoutChangingState()
    {
        var source = new ReadinessSource();

        Should.Throw<InvalidOperationException>(source.MarkStopped);
        Should.Throw<InvalidOperationException>(source.BeginShutdown);
        source.State.ShouldBe(ReadinessState.Stopped);

        var cycle = source.BeginStartup();
        Should.Throw<InvalidOperationException>(source.BeginStartup);
        source.MarkReady(cycle);
        Should.Throw<InvalidOperationException>(() => source.MarkFailed(cycle, new Error("late", "Too late.")));
        source.State.ShouldBe(ReadinessState.Ready);
    }

    [Test]
    public void ForeignAndStaleCyclesAreRejected()
    {
        var firstSource = new ReadinessSource();
        var secondSource = new ReadinessSource();
        var firstCycle = firstSource.BeginStartup();
        var foreignCycle = secondSource.BeginStartup();

        Should.Throw<InvalidOperationException>(() => firstSource.MarkReady(foreignCycle));

        firstSource.BeginShutdown();
        firstSource.MarkStopped();
        var restart = firstSource.BeginStartup();

        Should.Throw<InvalidOperationException>(() => firstSource.MarkReady(firstCycle));
        firstSource.MarkReady(restart);
    }
}
