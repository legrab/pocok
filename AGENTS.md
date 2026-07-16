# Agent instructions

## Priority and setup

1. Follow this file for stable Pocok engineering policy.
2. Follow `docs/agentic-workflow.md` for collaboration, planning, validation, records, and handoff.
3. Read `docs/current-handoff.md` only when work touches the current consolidation, release, package closure, or Modularity gate.
4. Use `docs/agentic-learning.md` when explaining how this repository or its agentic setup works.
5. Use `prompts/agent-base.prompt.md` and `prompts/agent-base.plan.md` as compatibility entry points for execution and durable plans.

The repository is current truth. Plans, sessions, and handoff notes are evidence that may drift.

## Repository boundary

- Treat this directory as an independent repository.
- Never add project references that escape the repository root. Consume external libraries through versioned package references and a local feed during development.
- Public packages must remain product-neutral and contain no company, customer, historical, operational, or sensitive material.
- Do not add the excluded complete application, business-workflow engine, or application UI surface in any form.
- Keep new packages internal until provenance, dependencies, API, tests, documentation, and package contents pass review.

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

## Testing and package proof

- Use NUnit, Shouldly, and Verify.
- Test public behavior and invariants rather than private implementation details.
- Use deterministic fakes, `TimeProvider`, and synthetic fixtures.
- Fast tests require no network, database, browser, device, or machine-specific configuration.
- Categorize integration, migration, packaging, and hardware tests explicitly.
- Update intentional snapshots; never delete them merely to make a test pass.
- Inspect consumers, tests, scripts, generated artifacts, reflection surfaces, caches, fixtures, adapters, package samples, and local-feed behavior before changing a public contract.
- Validate dependency injection, generated-artifact drift, package contents, symbols, Source Link, and installation from a local feed in proportion to risk.

## Files and documentation

- Use UTF-8 without BOM for source, configuration, JSON, and tooling files.
- Every public package has a README, compatibility tier, minimal sample, and documented nullability, cancellation, thread-safety, and security behavior.
- Generated artifacts are deterministic and CI verifies they are current.
- Keep architecture documentation synchronized when a public contract or lifecycle changes.
- Comments and documentation describe current constraints, never task history.
- Original hand-authored source uses `SPDX-License-Identifier:Apache-2.0` and `Copyright 2026 Pocok contributors`; generated files, snapshots, project/configuration files, and ordinary documentation do not need repetitive headers.
- Preserve `LICENSE`, `NOTICE`, third-party notices, and provenance when moving or adapting code. A stewardship request is nonbinding and never narrows Apache-2.0 permissions.

## Collaboration defaults

- An explicit implementation request permits ordinary reversible work within scope.
- Ask before unresolved architecture or product forks, destructive Git actions, commits, pushes, publication, release tags, or difficult-to-reverse changes.
- Use a short in-chat plan for ordinary multi-step work. Create a durable plan only when risk, duration, or handoff justifies it.
- Prefer coherent reviewable slices over artificial one-file or one-commit steps.
- Run focused checks first, then broaden according to public-surface and package risk.
- Report source changes separately from executable proof.
- Use friendly, concise, summary-first communication. Put optional detail in collapsible Markdown sections where useful, but keep failures and required actions visible.
