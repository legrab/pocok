# Agent Instructions

Act as a senior library engineer. Prefer correctness, explicit contracts, maintainability, and compatibility over shortcuts.

## Repository boundary

- Treat this directory as an independent repository.
- Never add project references that escape the repository root. Consume external libraries through versioned package references and a local feed during development.
- Public packages must remain product-neutral and contain no company, customer, historical, operational, or sensitive material.
- Do not add the excluded complete application, business-workflow engine, or application UI surface in any form.
- Keep new packages internal until their provenance, dependencies, API, tests, documentation, and package contents pass review.

## Architecture

- Keep packages cohesive and independently consumable.
- Integrations depend on abstractions; abstractions never depend on integrations.
- The internal convenience facade is a leaf. Public packages never depend on it.
- Avoid global mutable initialization, service locators, hidden reflection discovery, and implicit serializer, culture, time, or comparison policies.
- Add a dependency only when its value exceeds its transitive, security, and maintenance cost.

## C#

- Enable nullable reference types and warnings as errors.
- Use modern C# consistently, collection expressions where clear, and primary constructors for lean services.
- Use Result types for expected operational failures. Preserve exceptions for invalid arguments, broken invariants, and cancellation. Never swallow failures.
- I/O and long-running operations accept `CancellationToken`; time-dependent code uses `TimeProvider`.
- Make null, culture, comparison, casing, overflow, serialization, concurrency, and ownership semantics explicit at public boundaries.
- Prefer immutable state and instance-based services over static state.
- Default to no comments. Comment only a hidden constraint or surprising reason.
- Do not add extension methods that merely duplicate obvious framework behavior.

## Scripting and integrations

- Script runtimes expose allowlisted named capabilities and enforce cancellation and resource limits.
- A denylist is not a sandbox. Untrusted scripts receive no arbitrary reflection, filesystem, network, or service-provider access.
- JavaScript uses modern ECMAScript, no assumed browser globals, and `??` for option defaults.
- Live-value samples distinguish uninitialized, null, stale, bad-quality, and failed states and include timestamps.
- Every external source adapter passes the shared behavior contract suite.

## Testing

- Use NUnit, Shouldly, and Verify.
- Test public behavior and invariants rather than private implementation details.
- Use deterministic fakes, `TimeProvider`, and synthetic fixtures.
- Fast tests require no network, database, browser, device, or machine-specific configuration.
- Categorize integration, migration, packaging, and hardware tests explicitly.
- Update intentional snapshots; never delete them merely to make a test pass.
- Validate dependency injection, generated-artifact drift, package contents, symbols, Source Link, and installation from a local feed.

## Files and documentation

- Use UTF-8 without BOM for source, configuration, JSON, and tooling files.
- Every public package has a README, compatibility tier, minimal sample, and documented nullability, cancellation, thread-safety, and security behavior.
- Generated artifacts are deterministic and CI verifies they are current.
- Keep architecture documentation synchronized when a public contract or lifecycle changes.
- Comments and documentation describe current constraints, never task history.
- Original hand-authored source uses `SPDX-License-Identifier: Apache-2.0` and `Copyright 2026 Pocok contributors`; generated files, snapshots, project/configuration files, and ordinary documentation do not need repetitive headers.
- Preserve `LICENSE`, `NOTICE`, third-party notices, and provenance when moving or adapting code. A stewardship request is nonbinding and never narrows Apache-2.0 permissions.

## Current handoff

- Read `docs/plans/repository-consolidation.md` and `sessions/2026-07-15-package-semantics-appdefaults.md` before changing code or release configuration.
- Waves C and D are implemented structurally; execute their .NET 10 and PowerShell 7 acceptance matrix before extending packages.
- Keep Modularity non-releasable and treat Wave E as a separate cross-platform proof task.
- Do not create release tags until candidate-scoped local-closure, publication, audit, and CI checks pass.

## Workflow

- Read `prompts/agent-base.prompt.md` before executing a plan.
- Read `prompts/agent-base.plan.md` before creating a multi-step plan.
- Record active work in `sessions/` using the session template.
- Implement one independently compilable, reviewable step at a time.
- Inspect consumers, tests, scripts, generated artifacts, reflection surfaces, caches, fixtures, and adapters before changing a contract.
- Run focused tests first, then the relevant package, contract, packaging, and full validations required by risk.
- Record deviations and follow-ups explicitly. Do not hide incomplete work behind broad catches, skipped tests, or silent fallbacks.
- Avoid destructive git operations and force pushes unless explicitly requested.
