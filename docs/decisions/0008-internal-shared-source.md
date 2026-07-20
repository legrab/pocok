# ADR 0008: Internal shared source without a Common package

- Status: Accepted
- Date: 2026-07-15

## Context

Tiny repository helpers can be worth reusing without becoming public NuGet surface. A generic `Common`, `Utils`, or `Foundation` assembly would hide ownership, create accidental package dependencies, and repeat the problem that caused `Pocok.Primitives` to be retired.

## Decision

The default is package-local internal code. Identical helpers used by at least four projects may be moved to `src/Shared` as explicitly linked source when the BCL or an accepted dependency does not already solve the problem.

Shared source:

- is compiled as `internal` into every consumer;
- is linked file by file, never by wildcard;
- has no third-party dependencies;
- contains no public API;
- carries its own focused tests through consuming packages;
- is not a place for generic results, errors, guards, reflection wrappers, or extension-method collections without demonstrated reuse.

Packable projects may reference other packable projects, but never a non-packable runtime helper assembly. Test, sample, benchmark, and repository-tool projects may remain non-packable.

## Promotion and demotion

Promote shared source to a public package only after it acquires a coherent public contract, independent users, and a maintenance case. Demote a package when its public identity is weak but a small implementation remains genuinely useful inside this repository. Demotion means copying or linking only the justified implementation, not preserving the old public abstraction by habit.

## Consequences

The repository gains internal reuse without a hidden runtime dependency or a junk-drawer package. Some tiny files may be compiled into several assemblies, which is preferable to coupling unrelated packages through an artificial foundation.
