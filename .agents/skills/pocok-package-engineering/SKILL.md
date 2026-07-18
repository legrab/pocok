---
name: pocok-package-engineering
description: Implement, extend, diagnose, or review Pocok library packages and their public contracts. Use for work under src/, package unit tests, public API snapshots, package READMEs, samples, installed consumers, project/package references, dependency changes, compatibility semantics, or package-specific alpha-readiness. Do not use for Showcase UI work, release workflow orchestration, or general agent documentation.
---

# Pocok package engineering

Read root `AGENTS.md`, then inspect the affected package source, `.csproj`, README, tests, API snapshot, consumers, samples, and `eng/packages.json` entry. Read `docs/current-handoff.md` only when the task changes package closure, release eligibility, or a named current gate. Treat current source and executable evidence as authoritative.

## Frame the contract

1. Identify the package boundary, intended consumers, compatibility tier, and public behavior being changed.
2. Inspect transitive internal/external dependencies and reverse consumers before changing an API or package reference.
3. Apply the repository boundary and public-content rules from root `AGENTS.md` without restating them in task documentation.

## Implement

- Preserve existing behavior unless the request explicitly changes it.
- Make nullability, culture, comparison, overflow, cancellation, time, concurrency, ownership, serialization, and security behavior explicit at public boundaries.
- Use Result types for expected operational failures; reserve exceptions for invalid arguments, invariant failures, and cancellation.
- Use `CancellationToken` for I/O or long work and `TimeProvider` for time-dependent behavior.
- Avoid global mutable initialization, service locators, hidden reflection discovery, and ambient filesystem/network/service-provider access.
- Add a dependency only after reviewing its transitive, security, license, maintenance, and package-size cost.
- For child-process adapters, require explicit executable discovery, version/protocol probing, bounded asynchronous standard streams, kill-tree cancellation, deterministic private assets, and a truthful unavailable state. Validators and child processes are not described as an OS sandbox.
- Update the README and architecture/API documentation when the public contract or lifecycle changes.

## Prove the package

Start with focused behavior tests using the repository test stack, then broaden in proportion to risk:

1. Verify intentional public API snapshot changes member by member.
2. Run affected consumers and samples against the real public API.
3. For packaging changes, inspect `.nupkg`/`.snupkg` contents, metadata, dependencies, XML docs, symbols, Source Link, and deterministic artifacts.
4. Install from an isolated local feed and run the clean external consumer when the artifact boundary changes.
5. Run required Linux/Windows, trimming, integration, or package-specific gates when the claimed compatibility requires them.

Showcase scenarios are supplemental product-level acceptance; they never replace package tests, security proof, installed-consumer checks, or platform validation.

## Handoff

Report source changes separately from executable proof. State unrun checks, remaining contract uncertainty, and release-eligibility consequences explicitly. Do not change `releasable`, create tags, publish, commit, or push without the authority and evidence required by root policy.
