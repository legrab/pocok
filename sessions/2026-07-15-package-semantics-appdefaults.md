# Session: Package semantics and AppDefaults policy

- Date: 2026-07-15
- Status: Implementation complete, executable verification pending
- Plan: `docs/plans/repository-consolidation.md`
- Step: Waves C and D

## Objective

Finalize pending WIP on `dev/bg/est`, implement package-semantic validation, and make AppDefaults behavior explicit without expanding the public package portfolio.

## Starting state

- The previous .NET 10 and PowerShell 7 stabilization session recorded 182 passing tests.
- The branch contained later WIP changes around Modularity and release tooling.
- This execution environment contained neither .NET nor PowerShell and could not fetch them.
- `origin.zip` remains a repository-root reference artifact but is excluded from Git.

## Decisions and deviations

- Optional duplicate module IDs remain diagnostic unless strict optional failure handling is requested. Required duplicates remain fatal.
- One package-closure resolver is shared by catalog validation, smoke testing, auditing, and release packing.
- Duplicate concern configurator application is rejected rather than silently ignored.
- Configurator policy is exposed through startup `IOptions<T>` snapshots.
- Simple-console registration is opt-in to avoid a duplicate provider in standard hosts.
- No release tags are created because the latest changes could not be executed here.

## Changes

- `.github/workflows/ci.yml` and `.github/workflows/publish.yml` — sample execution, candidate-closure packing, and candidate-scoped audit.
- `tools/PackageCatalog` — dependency-first closure resolution and validation.
- `tools/PackageSmoke` — exact local closure and publication-shaped source mapping.
- `tools/PublicReleaseAudit` — strict candidate-closure package inspection.
- `src/AppDefaults.Logging*` — explicit duplicate, options, provider, and console semantics.
- `src/AppDefaults.Modularity` — consistent configurator behavior while remaining experimental.
- `tests/Unit/AppDefaults.*` and package consumers — focused policy and clean-consumer coverage.
- repository documentation — current evidence, release gates, and remaining verification.

## Validation

- `git diff --check` — passed during implementation.
- JSON, XML, YAML, project graph, catalog graph, documentation-link, source-header, and delimiter checks — passed statically.
- `dotnet` and `pwsh` commands — not available in this environment.

## Follow-ups

- Blocking: run the exact acceptance matrix from `docs/implementation/repository-consolidation-ledger.md` on .NET 10 and PowerShell 7.
- Blocking: fix any factual compile, test, pack, or PowerShell failures found by that run.
- Before Modularity release: execute Wave E on Linux and Windows and verify deployed plugin directories and native dependency behavior.

## Next step

Run the full acceptance matrix on the exact HEAD, then create release tags in dependency order only after both smoke modes pass for each candidate.
