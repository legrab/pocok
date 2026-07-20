# Current repository handoff

**Status revision:** Documentation topology reconciled against `main`; executable release evidence remains provisional  
**Next execution owner:** [`docs/plans/release-readiness.md`](plans/release-readiness.md) RR1

Any change that alters package eligibility, package closure, release evidence, or the Modularity gate must update this file with observed results. A documentation checkbox is not executable proof.

## Current catalog state

`eng/packages.json` is schema version 1 and currently uses `state`, `releasable`, and `releaseTier`.

There are fifteen non-retired packable libraries.

Current catalog entries with `releasable: true`:

- `Pocok.Conversion`;
- `Pocok.Readiness`;
- `Pocok.AppDefaults`;
- `Pocok.AppDefaults.Logging`;
- `Pocok.AppDefaults.Logging.Serilog`;
- `Pocok.Scripting`;
- `Pocok.Licensing`;
- `Pocok.AppDefaults.Licensing`.

Current experimental entries with `releasable: false`:

- `Pocok.Modularity.Contracts`;
- `Pocok.Modularity`;
- `Pocok.AppDefaults.Modularity`;
- `Pocok.BackgroundWork`;
- `Pocok.Localization`;
- `Pocok.Signals`;
- `Pocok.Subscriptions`.

The catalog, not older prose, is the current eligibility authority. This documentation restructuring does not change any entry.

## Current implementation and evidence

- Pull-request CI derives affected source, test, sample, benchmark, package, smoke, audit, and coverage work from changed paths plus reverse dependencies. Pushes to `main`, explicit full runs, policy changes, and unsafe graph states retain complete validation through the stable CI gate.
- CI tooling rejects non-local workflow actions that are not pinned to full commit SHAs.
- A prior Windows .NET 10.0.102 and PowerShell 7.3.6 run passed formatting, solution build, 236 tests, core samples, catalog validation, local-closure smoke, and public release audit on a pre-licensing baseline. This is historical evidence, not proof for the current candidate.
- The previous Ubuntu `SourceFailurePublishesFailureAndReconnects` fake-time race was corrected by waiting for timer registration. A fresh Linux run remains required for current evidence.
- BackgroundWork, Localization, Signals, and Subscriptions already have source, focused tests, API tracking, samples, and installed-package consumers. Their remaining alpha gates require a current gap audit and executable proof rather than wholesale reimplementation.
- Modularity already follows the resolved BCL `AssemblyLoadContext` design, with clean-room fixtures, integration tests, `ModularCommunicator`, and real Showcase plugin loading. Its eligibility remains blocked until the complete Linux and Windows gate passes.
- `Pocok.Scripting` is currently one releasable package with Jint in the package and an implicit JavaScript runner. Release Readiness owns splitting it into an engine-neutral core plus JavaScript, C#, and Python adapters.
- `Pocok.Licensing` and `Pocok.AppDefaults.Licensing` are currently catalogued as releasable. Their source/tests/sample/consumer and documented acceptance gate still require current-candidate executable confirmation before a synchronized release.
- Existing local Showcase plugins are Conversion, Scripting, and Licensing. Release Readiness owns seven more local plugins and the shared internal Monaco wrapper.

No new test, package, CI, NuGet, browser, Docker, or deployment proof was produced by the documentation-plan patch.

## Current GLOBAL-v* orchestration

A schema-v1 synchronized global workflow is implemented.

It:

- targets only non-retired entries with `releasable: true`;
- derives deterministic dependency-first order from `eng/packages.json`;
- shares `group: pocok-publication`, `cancel-in-progress: false`, and `queue: max` with package-specific publication;
- validates exact versions and repository/tag provenance before authentication;
- performs target-scoped local smoke and public-content audit;
- preserves candidate and state artifacts;
- uses a draft GitHub Release;
- waits for package propagation;
- supports provenance-aware same-tag resume.

The hosted job rejects more than eighteen target packages. Release Readiness expands the library graph from fifteen to exactly eighteen by adding the Scripting JavaScript, C#, and Python adapters, then uses this existing workflow for the first library-only synchronized prerelease.

[`docs/plans/mvp-closure.md`](plans/mvp-closure.md) later owns:

- schema-v2 catalog and publication policy;
- typed release tooling;
- immutable candidate manifests and rebuild-free recovery;
- automated Source Link proof;
- ten Showcase bundle packages;
- capacity for the twenty-eight-package library-plus-bundle graph;
- exact NuGet Showcase composition;
- browser, Docker, public-feed, and Render closure.

## Next action

Execute [`docs/plans/release-readiness.md`](plans/release-readiness.md) from RR1:

1. establish a fresh exact baseline;
2. record package-specific gaps and current failures;
3. split and harden Scripting;
4. close the seven remaining package gates;
5. complete ten-plugin local Showcase coverage;
6. rehearse the eighteen-library graph;
7. request explicit approval before creating a `GLOBAL-v*` tag.

Use package, Showcase, release, and agentic skills only for the slices that need them. Preserve source changes, local executable proof, hosted cross-platform proof, package proof, public-feed proof, and actual publication as distinct evidence layers.
