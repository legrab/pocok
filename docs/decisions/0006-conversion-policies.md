# ADR 0006: Explicit conversion policies

- Status: Accepted, revised after consolidation
- Date: 2026-07-14
- Revised: 2026-07-15

## Context

Runtime values arrive from configuration, files, scripts, protocols, and dynamic APIs. Conversion must not depend on process culture, machine time zone, unchecked overflow, serializer configuration, or process-global registration.

## Decision

`Pocok.Conversion` owns its complete public contract and implementation. The separate Abstractions package and generic Primitives dependency are retired.

`ConversionContext` is immutable and names every policy that can change an outcome:

| Concern | Strict behavior |
|---|---|
| Culture | invariant, cloned, and read-only |
| Null | preserved only when the target permits null |
| Overflow | failure |
| Fractional-to-integral loss | failure |
| Numeric booleans | disabled |
| Enums | declared values and valid declared-bit flag combinations |
| Temporal text | invariant round-trip formats only |
| Nesting | bounded by maximum depth |
| Collections | bounded by a total item budget |

Collections convert recursively, fail at the first rejected item, and retain a source path. Duplicate converted dictionary keys fail explicitly. Strings are never treated as arbitrary enumerables.

Custom behavior is supplied through explicitly ordered immutable `IConversionStrategy` instances. A strategy receives policies, the current path, and a bounded nested-conversion continuation. There is no global registry, service locator, serializer fallback, or property-matching object mapper.

## Consequences

Callers can reason about conversion from the call site and safely share converter instances across threads. A serializer-assisted adapter, should a real consumer require it, must be separately named and dependency-explicit rather than becoming an implicit fallback in the core.
