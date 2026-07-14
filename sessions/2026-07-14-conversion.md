# Session: Pocok.Conversion

- Date: 2026-07-14
- Status: Complete for maintainer handoff
- Plan: Common extraction plan
- Step: 4 — policy-driven conversion

## Objective

Extract alpha-releaseable conversion abstractions and a serializer-free implementation with strict invariant defaults and explicit relaxation policies.

## Starting state

- Pocok contained the released Primitives package and repository validation harness.
- Sanitized reference code demonstrated useful scalar, numeric, temporal, enum, collection, pair, and dictionary conversions but depended on broad utilities, static initialization, and JSON fallback.

## Decisions and deviations

- Split policy/API contracts from the implementation.
- Use immutable per-call contexts and a stateless converter.
- Reject implicit numeric booleans, fractional loss, overflow, local-time inference, undefined enums, and arbitrary object mapping.
- Preserve valid flags combinations in the strict default when every bit is declared.
- Defer serializer conversion to a separately reviewed adapter.

## Changes

- Added `Pocok.Conversion.Abstractions` and `Pocok.Conversion`.
- Added comprehensive unit matrices, concurrency checks, exact reviewed API baselines, a console sample, ADR, package smoke coverage, and repository integration.

## Validation

- `dotnet restore Pocok.slnx --locked-mode` passed.
- `dotnet format Pocok.slnx --verify-no-changes --no-restore` passed.
- Release build passed with zero warnings and zero errors.
- All 137 tests passed: 96 conversion, 30 primitives, 8 packaging, and 3 architecture tests.
- The conversion sample ran and demonstrated strict, culture-aware, saturating, flags, and collection conversion.
- Packed `Pocok.Conversion.Abstractions` and `Pocok.Conversion` as `0.1.0-alpha.1.8` with symbols and Source Link metadata.
- Isolated local-feed consumers passed for Conversion, Conversion.Abstractions, and Primitives.
- Public release audit and the transitive vulnerability scan passed.
- Repeated packs produced identical meaningful entry payloads. Raw archive hashes differ because NuGet regenerates OPC relationship and core-property metadata; this non-blocking release-hardening item is documented for follow-up.

## Follow-ups

- LOW: Add a named Newtonsoft.Json adapter only when a concrete consumer establishes its allowlist and settings contract.
- LOW: Investigate byte-for-byte reproducible NuGet archives or formalize the normalized payload comparator.

## Next step

Hand the reviewed, validated diff to the maintainer for the package commit.
