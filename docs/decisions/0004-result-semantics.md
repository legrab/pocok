# ADR 0004: Generic result semantics

- Status: Superseded by package-owned failure models
- Date: 2026-07-14
- Superseded: 2026-07-15

## Original decision

The initial extraction introduced public `Error`, `Result`, and `Result<T>` primitives for explicit operational failures.

## Superseding decision

Do not maintain a generic result package merely to share a small amount of code. Conversion now owns `ConversionFailure` and `ConversionResult<T>`. Readiness owns `ReadinessFailure` and communicates lifecycle failure through `ReadinessFailedException` to asynchronous waiters.

The shared invariants remain useful:

- success and failure are mutually exclusive;
- safe code and message are explicit;
- cancellation is not converted into a failure object;
- diagnostic exceptions are optional and are not caller-facing messages;
- invalid API arguments and broken invariants still throw.

These semantics are implemented in the package that owns the behavior rather than exposed through `Pocok.Primitives`.

## Consequences

Consumers no longer acquire a generic foundation dependency. A future cross-package result abstraction requires independent public demand, not merely two similar implementations inside this repository.
