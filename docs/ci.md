# Dependency-aware continuous integration

Pocok CI plans validation from the checked-out commit and repository metadata. Pull requests validate affected slices. Pushes to `main` remain complete repository validation.

## Impact model

The planning job reads changed paths, all project references, linked `Compile Include` files, and `eng/packages.json`. A source project change selects that project and every reverse transitive source and package dependent. Tests, samples, and benchmarks are then selected from their project-reference closure.

Test-only and sample-only changes remain narrow. A package README, public API snapshot, or package-smoke consumer is mapped back to its package. Package packing includes unchanged internal dependencies required to create a usable local feed, but those dependencies are not reported as directly affected.

The generated `artifacts/ci/impact.json` records changed files, reasons, selected projects, affected packages, packing closure, smoke and audit decisions, and coverage slices. Paths and arrays are normalized and ordinally sorted.

## Conservative full validation

Full validation is used for:

- pushes to `main`;
- manual runs with the `full` input enabled;
- pull requests carrying the `ci:full` label;
- workflow, CI-tooling, central build, solution, package-catalog, package-metadata, licensing, or repository-policy changes;
- missing history, deleted project definitions, graph cycles, catalog mismatches, parsing failures, and unknown relevant source or build paths.

False positives are acceptable. A plan that cannot prove partial validation safe falls back to full validation.

## Jobs and stable gate

The workflow has planning, Linux validation, Windows validation, public release validation, pull-request coverage comparison, and a final `CI gate` job. Branch protection should require `CI gate`, whose name is stable even when dynamic jobs are intentionally skipped.

Linux restores, formats, builds, and tests selected projects. It also packs, smoke-tests, and audits selected packages. Windows runs the same affected behavioral tests and samples without duplicate coverage collection.

Public release validation restores, formats, builds, and tests `Pocok.Core.slnx` with `IncludeExperimental=false`. This keeps the non-experimental packaging references and public API snapshots executable before a publication tag reaches the release workflow. The planning job skips it only for documentation-only changes, and the final gate requires it whenever selected.

Documentation-only pull requests run repository-owned tooling checks and produce a plan, but do not start the .NET validation jobs.

## Coverage

Normal slice-owned tests reference `coverlet.collector` through `tests/Directory.Build.props`. Fixtures and repository-wide architecture or packaging tests do not receive the collector.

Linux collects head coverage while running affected tests, rather than running the head tests twice. Coverage is filtered to source files owned by each package project, and duplicate line hits from multiple reports are merged. Branch coverage is shown only when condition identities can be merged safely.

For pull requests, the coverage job creates a detached worktree for the exact PR base SHA and runs the same affected slice tests there. A new slice or a base commit that predates coverage tooling reports `N/A` instead of zero. Line and branch deltas are percentage-point changes.

Coverage regression is advisory initially. A negative line delta emits a warning but does not block merging. Head coverage collection failure remains a test failure. Raw Cobertura, TRX, impact, and summary files are retained for 14 days.

## Local reproduction

Resolve an actual Git diff:

```pwsh
./tools/Ci/Test-CiTooling.ps1
./tools/Ci/Resolve-CiImpact.ps1 `
    -BaseSha <base-sha> `
    -HeadSha <head-sha> `
    -EventName pull_request `
    -OutputPath artifacts/ci/impact.json
```

Resolve a representative path without Git history:

```pwsh
./tools/Ci/Resolve-CiImpact.ps1 `
    -ChangedFiles 'src/Conversion/ValueConverter.cs' `
    -OutputPath artifacts/ci/impact.json
```

Run the selected Linux or Windows behavior locally:

```pwsh
./tools/Ci/Invoke-AffectedValidation.ps1 -Platform Linux
./tools/Ci/Invoke-AffectedValidation.ps1 -Platform Windows
```

Run the publication-shaped source validation locally:

```pwsh
./tools/Ci/Invoke-PublicReleaseValidation.ps1
```

Create the PR coverage comparison after Linux head coverage exists:

```pwsh
./tools/Ci/Invoke-SliceCoverageComparison.ps1
```

## Adding a discoverable slice

A package becomes discoverable by adding its project and dependency metadata to `eng/packages.json`. A normal test project should end in `.Tests`, reference its owning source project, and use a name matching the package slice. Repository-wide tests must be explicitly classified in `Get-PocokTestOwnership`.

Samples and benchmarks are discovered by directory and project references. Package-smoke consumers use the catalog `consumer` name. Package-local READMEs and verified public API snapshots are mapped automatically.

When selection is unexpected, inspect `artifacts/ci/impact.json` and the planning job summary. A full fallback always includes a reason.
