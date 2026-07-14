# ADR 0006: Observable readiness lifecycle

- Status: Accepted
- Date: 2026-07-14

## Context

Scheduling or completing a host startup callback does not always mean that its cache, adapter, or background integration is ready for consumers. Waiters also need deterministic behavior when startup fails, is cancelled, is interrupted by shutdown, or is retried.

## Decision

`ReadinessSource` owns one explicit lifecycle and implements the read-only `IReadinessSignal` contract. Its states are `Stopped`, `Starting`, `Ready`, `Stopping`, and `Failed`.

Each startup receives an opaque, monotonically sequenced `ReadinessCycle`. Only that active cycle may report readiness, failure, or cancellation. This prevents completion from an abandoned startup racing with a later restart.

Current waiters share one task with asynchronous continuations. Per-waiter cancellation never cancels the shared lifecycle. Startup cancellation cancels the current shared task and remains cancellation rather than a structured failure. Startup and shutdown failures use `Pocok.Primitives.Error` for safe inspection and fault waiters with `ReadinessFailedException`.

Shutdown removes readiness as soon as it begins. It faults waiters interrupted before readiness and creates a new pending generation for consumers awaiting a later restart. A failed cycle remains failed until an explicit new startup replaces it.

The package does not supply a timeout that mutates lifecycle state. Caller timeouts affect only the caller and cannot produce a ready state.

## Consequences

Lifecycle owners must report transitions explicitly and retain their cycle token until startup completes. Invalid, duplicate, foreign, and stale transitions fail immediately instead of being silently ignored. The primitive remains independent of a specific host, logger, service provider, or background-service base class and can therefore be composed into those outer layers.
