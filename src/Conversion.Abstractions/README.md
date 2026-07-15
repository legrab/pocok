# Pocok.Conversion.Abstractions

`Pocok.Conversion.Abstractions` defines the policy context and converter contract shared by conversion implementations and consumers.

The strict default is invariant and deterministic: null is preserved only for nullable targets, overflow and fractional loss fail, numeric booleans are disabled, enum values must be declared or valid flag combinations, and temporal text must use round-trip formats.

The package is an experimental alpha contract. It is thread-safe because contexts are immutable and a converter receives the complete policy for every call. It performs no I/O, uses no serializer, and has no cancellation surface.
