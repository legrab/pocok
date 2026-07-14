# ADR 0004: Result Semantics

- Status: Accepted
- Date: 2026-07-14

## Context

Expected operational failures need an explicit return type that does not couple callers to logging, transports, or exception-driven control flow. The contract must remain small enough to sit beneath every other Pocok package.

## Decision

`Result` and `Result<T>` have exactly two states: success and failure. Construction is factory-only so these invariants always hold:

- success has no error;
- failure has one non-null `Error`;
- `Result<T>` success may contain a legitimate null when `T` permits null;
- failure never carries a partial value;
- reading `Value` from a failure throws `InvalidOperationException`;
- `TryGetValue` supports branch-oriented access without exceptions;
- cancellation remains cancellation and `OperationCanceledException` cannot be stored as an error diagnostic;
- error code and safe message are required non-whitespace strings;
- an attached exception is optional diagnostic context and is not a substitute for the safe message.

There are no implicit conversions from booleans, exceptions, values, or tasks. Expected failures are created explicitly. Invalid arguments, broken invariants, and exceptions thrown by mapping delegates propagate normally.

The initial combinator surface is deliberately small:

- `Match` handles both states explicitly;
- `Map` transforms a successful value;
- `Bind` chains an operation already returning a Result;
- `ToResult` intentionally discards a successful value.

No async combinators, exception-catching helpers, serialization contracts, HTTP mappings, logging hooks, process identifiers, or application-specific third states belong in this package.

The conventional C# names `Error` and `Result<T>.Success` / `Result<T>.Failure` are intentional. Their narrowly scoped cross-language and generic-static analyzer warnings are suppressed only on the defining source files.

## Consequences

Callers cannot create contradictory states or accidentally treat failure as a boolean. Nullable success is distinguishable from failure. Consumers must decide where exceptions become safe structured errors, which keeps security-sensitive messaging at the correct boundary. Additional combinators require demonstrated use in at least one real package.
