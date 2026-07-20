# Current repository handoff

Status revision: provisional pre-MVP evidence; refresh against the exact current commit in `docs/plans/repository-finalization.md` R1 before changing eligibility

Refresh owner: any change that alters release eligibility, acceptance evidence, package closure, or the Modularity gate must update this file or remove obsolete claims.

## Current state

- Pull-request CI now derives affected source, test, sample, benchmark, package, smoke, audit, and coverage work from changed paths plus reverse transitive project and package dependencies. Pushes to `main`, explicit full runs, global policy changes, and unsafe graph states retain complete validation through the stable `CI gate`.
- CI tooling now rejects workflow actions that are not repository-local or pinned to a full commit SHA. Local PowerShell validation passes all 47 CI-tooling assertions; a hosted GitHub Actions run remains required for runner/action compatibility evidence.
- Per-slice head coverage is collected during the Linux test run and compared with the exact pull-request base SHA when base tooling exists. The CI scripts, workflow structure, XML, JSON, and repository graph have received static validation in the current environment; a PowerShell 7 and .NET 10 Linux/Windows execution remains required.
- Waves C and D are structurally implemented.
- A Windows .NET 10.0.102 and PowerShell 7.3.6 acceptance run passed formatting, solution build, 236 tests, core samples, package catalog validation, local-closure smoke, and public release audit on the pre-licensing baseline. The licensing addition has not yet received executable proof. Scripting has a tracked public API snapshot, ten focused behavior tests, source-size and memory bounds, and an installed-package consumer check; Signals has a tracked public API snapshot, twenty-two focused contract/runtime tests, shared subscription lifecycle, point-in-time reads, and structured typed-read conversion failures, plus an installed-package consumer check; Localization has a tracked public API snapshot, focused composition, culture, JSON, RESX, reload, watcher and enum-localization tests, a sample, and an installed-package consumer check; Subscriptions has a tracked public API snapshot, five focused registry tests, a sample, and an installed-package consumer check.
- The Ubuntu failure in `SourceFailurePublishesFailureAndReconnects` was a fake-time test race: the test could advance time before the reconnect timer was registered. The test now waits for timer registration before advancing time and reports a clear timeout when asynchronous conditions are not reached. A fresh Linux CI run remains required.
- Packing succeeded and produced the expected packages and symbols; local MinVer emitted `MINVER1001` warnings because the sandbox Git identity cannot treat the parent repository as a valid working directory.
- The Operations worker sample explicitly owns a console-only logging provider set and does not cancel an already-ready startup cycle during shutdown.
- Modularity remains experimental and non-releasable until its separate Linux and Windows proof gate passes.
- Pocok.BackgroundWork is an experimental, non-releasable alpha package covering guarded task observation, one-active-plus-one-pending coalescing, quiet-period debounce, and awaited non-overlapping repetition. Source, unit tests, public API tracking, package catalog wiring, and an installed-package console consumer are present; executable .NET 10 proof is still required.
- Pocok.Scripting is now an experimental, non-releasable alpha package containing the neutral bounded execution/import slice; product-specific providers and UI/persistence integrations remain outside the extraction.
- Pocok.Signals is now an experimental, non-releasable alpha package containing the neutral live-value contracts and shared runtime, including point-in-time reads; protocol adapters, persistence, caching backends, and product-specific integrations remain outside this extraction.
- Pocok.Localization is now an experimental, non-releasable alpha package containing deterministic composition over standard .NET string-localizer providers, enum translation fallback, and explicit resource-file culture resolution; database, filesystem, resource-assembly discovery, caching, global culture mutation, and application-specific integrations remain outside this extraction.
- Pocok.Subscriptions is now an experimental, non-releasable alpha package containing a thread-safe keyed listener registry with typed filtering and mapping; transport connections, retry timers, logging, persistence, and network lifecycle remain outside this extraction.
- Pocok.Licensing and Pocok.AppDefaults.Licensing are experimental, non-releasable alpha packages. Static review covers signed and optionally encrypted licenses, module/time/runtime/machine/PSK constraints, explicit reload, host enforcement, CLI tooling, tests, sample, and package consumers. The .NET 10, PowerShell, package-smoke, and Windows/Linux acceptance gate in `docs/licensing.md` is still required.
- Do not create release tags until Linux CI, candidate-scoped publication-shaped restore, audit, and the automated portable-PDB/Source Link proof specified by `docs/plans/repository-finalization.md` are current.

## Next action

Execute `docs/plans/repository-finalization.md` from R1. The immediate evidence remains `./tools/Ci/Test-CiTooling.ps1`, complete Linux/Windows CI, the licensing acceptance gate, automated portable-PDB/Source Link verification from an installed candidate package, and candidate-scoped publication smoke before eligibility changes.

Use `.agents/skills/pocok-release-engineering/SKILL.md` for the release acceptance procedure and `.agents/skills/pocok-package-engineering/SKILL.md` for each package-specific gate. This handoff records state only.

## GLOBAL-v* interim orchestration

A schema-v1 synchronized global release workflow is now present. It remains blocked by the existing exact-commit release gates and performs no publication until a GLOBAL-v* tag is explicitly pushed. Future finalization migrates it to typed tooling and immutable manifests.
