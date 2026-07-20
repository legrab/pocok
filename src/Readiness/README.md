# Pocok.Readiness

`Pocok.Readiness` separates actual readiness from the fact that a lifecycle method was scheduled. It provides a
thread-safe, restartable readiness source for hosted services, caches, adapters, and integration runtimes.

Compatibility tier: **releasable alpha**. Publication remains tag-driven and subject to the repository release gates.

```csharp
var readiness = new ReadinessSource();
var cycle = readiness.BeginStartup();

try
{
    await InitializeAsync(cancellationToken);
    readiness.MarkReady(cycle);
}
catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
{
    readiness.CancelStartup(cycle, cancellationToken);
    throw;
}
catch (Exception exception)
{
    readiness.MarkFailed(
        cycle,
        ReadinessFailure.FromException("cache.start.failed", "The cache could not start.", exception));
    throw;
}

await readiness.WaitUntilReadyAsync(cancellationToken);
```

## Lifecycle contract

- A source begins in `Stopped`; waiters then observe the next startup cycle.
- `BeginStartup` is valid from `Stopped` or `Failed` and returns an opaque cycle token.
- Only the active cycle can become ready, fail, or be cancelled. A stale or foreign token is rejected.
- A successful `MarkReady` releases all waiters. Continuations are scheduled asynchronously.
- Failure faults waiters with `ReadinessFailedException` and preserves a caller-safe `ReadinessFailure`.
- Startup cancellation cancels current waiters with the supplied cancelled token and remains distinct from failure.
- `BeginShutdown` removes readiness immediately. Waiters from an interrupted startup receive
  `ReadinessStoppedException`; new waiters observe a later restart.
- Shutdown failure remains observable until a new startup begins.

The source does not own a timeout policy. A caller may cancel its own wait or use `Task.WaitAsync`; neither action
changes shared readiness and a timeout is never treated as success.

All state transitions are thread-safe. The source contains no ambient state, logging, dependency injection, or
process-global registration. Dispose values attached to a `ReadinessFailure` according to their own ownership contract;
this package does not own them.

See [ADR 0007](https://github.com/legrab/pocok/blob/main/docs/decisions/0007-readiness-lifecycle.md) for transition and
restart rationale.
