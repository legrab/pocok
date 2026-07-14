# Pocok.Primitives

`Pocok.Primitives` provides explicit success and structured failure contracts with no runtime package dependencies.

```csharp
Error invalidInput = new("input.invalid", "The supplied value is not valid.");

Result<int> parsed = int.TryParse(text, out int value)
    ? Result<int>.Success(value)
    : Result<int>.Failure(invalidInput);

Result<string> formatted = parsed
    .Map(value => value * 2)
    .Bind(value => value >= 0
        ? Result<string>.Success(value.ToString(CultureInfo.InvariantCulture))
        : Result<string>.Failure(new Error("value.negative", "The value cannot be negative.")));
```

## Contract

- Success and failure are the only states.
- A failure always has an `Error` and never carries a partial value.
- A successful `Result<T>` may contain null when `T` permits it.
- `Value` throws on failure; use `TryGetValue` for branch-oriented access.
- Error messages are caller-safe. Attached exceptions are diagnostic context and should not be displayed or serialized by default.
- Cancellation is not failure and must propagate normally.
- Mapping delegates are not caught. Programming errors and broken invariants remain exceptions.
- Instances are immutable and safe to share between threads when their contained value and exception are safe to share.

See [ADR 0004](../../docs/decisions/0004-result-semantics.md) for the complete rationale.
