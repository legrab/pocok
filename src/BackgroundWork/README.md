# Pocok.BackgroundWork

Compatibility tier: experimental alpha. This package is packable and tested but is not release-eligible until its Windows and Ubuntu acceptance gate passes.

`Pocok.BackgroundWork` provides small lifecycle primitives for event-driven .NET services:

- guarded observation of intentionally non-awaited `Task` and `Task<T>` instances;
- coalescing bursts into one active execution and at most one pending rerun;
- quiet-period debounce without overlapping operations;
- awaited, non-overlapping repeated work.

It is not a retry library, durable scheduler, distributed queue, cron implementation, or hosted-service framework. Polly may be used inside an operation when resilience policies are required.

## Guarded task observation

A source task may be detached from the caller only when a general fault handler is supplied. Filtered handlers run in registration order before the fallback. The returned `TaskObservation` keeps the outcome observable during tests and controlled shutdown.

```csharp
TaskObservation observation = ReadDeviceAsync(cancellationToken).Observe(
    onFault: exception => LogFailure(exception),
    configure: options => options
        .OnSuccess(() => RecordSuccess())
        .OnCanceled(exception => RecordCancellation(exception))
        .OnFault<TimeoutException>(exception => RecordTimeout(exception)));

TaskObservationResult result = await observation.Completion;
```

`Completion` never faults. Source faults and cancellations are exposed through `SourceException`. Predicate and callback failures, including observation-token cancellation, are exposed through `ObserverException`.

The observation token controls callback dispatch and callback waiting. It cannot cancel a source task that was not created with that token.

## Coalescing

Use `CoalescingTaskRunner` when many triggers should produce one current operation and at most one later rerun.

```csharp
await using var refresh = new CoalescingTaskRunner(
    ReloadAsync,
    new CoalescingTaskRunnerOptions
    {
        MinimumInterval = TimeSpan.FromMilliseconds(250)
    });

await refresh.RequestAsync(cancellationToken);
```

All requests in one drain cycle share its completion task. Caller cancellation cancels only that caller's wait. `StopAsync` cancels shared work and permanently closes the runner.

## Debounce

Use `DebouncedTaskRunner` when an operation should begin only after incoming events remain quiet.

```csharp
await using var reload = new DebouncedTaskRunner(
    ReloadAsync,
    new DebouncedTaskRunnerOptions
    {
        QuietPeriod = TimeSpan.FromMilliseconds(250)
    });

_ = reload.RequestAsync().Observe(ReportReloadFailure);
```

Requests received while the operation is running schedule one later execution. The quiet period is measured from the latest request, so the rerun starts immediately when that period has already elapsed by the time the current operation completes.

## Repetition

`TaskRepeater.RepeatAsync` returns the complete repeat lifecycle and never overlaps iterations. The interval is measured from one operation completion to the next operation start.

```csharp
await TaskRepeater.RepeatAsync(
    PollAsync,
    new TaskRepeaterOptions
    {
        Interval = TimeSpan.FromSeconds(5),
        MaximumIterations = 10
    },
    cancellationToken);
```

All timing primitives accept `TimeProvider`, allowing deterministic tests without wall-clock sleeps.

## Failure policy

Coalescing, debounce, and repetition stop on the first failure by default. `BackgroundWorkFailurePolicy.Continue` requires an explicit `OnFailure` callback. If that callback fails, the operation and callback failures are combined in an `AggregateException` and execution stops.

All runners are safe for concurrent requests. Operations are awaited and never overlap. Dispose runners asynchronously to cancel pending work and release lifecycle resources.
