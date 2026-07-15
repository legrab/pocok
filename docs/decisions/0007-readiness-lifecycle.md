# ADR 0007: Observable readiness lifecycle

- Status: Accepted, revised after package rename
- Date: 2026-07-14
- Revised: 2026-07-15

## Context

Scheduling or completing a host startup callback does not always mean that its cache, adapter, or background integration is ready. Waiters need deterministic behavior when startup fails, is cancelled, is interrupted by shutdown, or is retried.

## Decision

`Pocok.Readiness` owns one explicit lifecycle and implements the read-only `IReadinessSignal` contract. Its states are `Stopped`, `Starting`, `Ready`, `Stopping`, and `Failed`.

Each startup receives an opaque, monotonically sequenced `ReadinessCycle`. Only the active cycle may report readiness, failure, or cancellation, preventing an abandoned startup from completing a later restart.

State, sequence, and failure are observed atomically through `ReadinessSnapshot`. Current waiters share one task with asynchronous continuations. Per-waiter cancellation never cancels shared readiness. Startup cancellation remains cancellation. Startup and shutdown failures use `ReadinessFailure` and fault waiters with `ReadinessFailedException`.

Shutdown removes readiness immediately. Waiters interrupted before readiness receive `ReadinessStoppedException`; new waiters observe the next generation. A failed cycle remains failed until an explicit new startup begins.

The package supplies no timeout that mutates lifecycle state and has no dependency on hosting, logging, dependency injection, or AppDefaults.

## Consequences

Lifecycle owners report transitions explicitly and retain their cycle token until startup completes. Invalid, duplicate, foreign, and stale transitions fail immediately. Hosting and health-check adapters may be added later as separate packages only after repeated application demand.
