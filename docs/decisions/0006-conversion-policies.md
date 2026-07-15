# ADR 0006: Explicit conversion policies

- Status: Accepted
- Date: 2026-07-14

## Context

Runtime values arrive from configuration, files, scripts, protocols, and dynamic APIs. Converting them without an explicit policy can make behavior depend on process culture, machine time zone, unchecked overflow, serializer configuration, or global initialization.

The previous implementation provided useful numeric and collection behavior but coupled it to a broad utility assembly, mutable process-wide registration, logging, and implicit JSON fallback. Its behavior for numeric booleans, flags, date/time values, and fractional loss was not a stable public contract.

## Decision

`Pocok.Conversion.Abstractions` owns `IValueConverter`, immutable `ConversionContext`, policy enums, and stable error codes. `Pocok.Conversion` provides one stateless implementation. Both depend on `Pocok.Primitives`; no process-global registration or service locator exists.

The strict context is:

| Concern | Strict behavior |
|---|---|
| Culture | Invariant, cloned, and read-only |
| Null | Preserved only when the target permits null |
| Overflow | Failure |
| Fractional-to-integral loss | Failure |
| Numeric booleans | Disabled |
| Enums | Declared values and valid declared-bit flag combinations |
| Temporal text | Invariant round-trip formats only |

Relaxed behavior requires a new context naming the policy. Saturation clamps to finite target boundaries. Rounding uses nearest with midpoint away from zero. `ZeroOrOne` numeric booleans accept only exact zero and one; `NonZeroIsTrue` accepts any finite numeric value. Enum names are matched ordinally without regard to case. Undefined enum values and undeclared flag bits fail.

Temporal text uses `O` for date/time values and `c` for time spans. Culture-aware parsing is opt-in. `DateTime` text and formatting reject local values; offset-bearing text targets `DateTimeOffset`. `DateTimeOffset` converts to UTC `DateTime`, and only UTC `DateTime` converts to `DateTimeOffset`, preventing a machine-local offset from entering conversion. Date and time projections are explicit and deterministic.

Collections convert each item recursively. Arrays, common sequence/set interfaces, mutable concrete collections, dictionaries, concurrent mutable containers, key/value pairs, and dictionary entries are supported. Strings are not treated as arbitrary enumerables. Conversion stops at the first failed item, and duplicate converted dictionary keys return a failure.

Unsupported object shapes fail. The core never invokes a JSON serializer and never maps arbitrary objects by matching properties.

## Consequences

Callers can reason about conversion from the call site and safely share converter instances across threads. Existing callers relying on implicit default values, local dates, nonzero numeric booleans, undefined enums, or serializer fallback must opt into a named policy or a future adapter.

A serializer-assisted adapter is intentionally deferred. If a real consumer requires it, it will be a separately named package with an explicit serializer dependency and options rather than a fallback in the core.
