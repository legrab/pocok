# Repository consolidation report

> **Current status:** This file records the consolidation changes applied. The repository has been formatted and build errors have been fixed. Further stabilization and test verification should follow the [consolidation plan](../plans/repository-consolidation.md).

## Result

The repository has been transformed into a consolidated state with a coherent target package portfolio. It is ready for final stabilization and verification.

### Intended initial release set

- `Pocok.Conversion`
- `Pocok.Readiness`
- `Pocok.AppDefaults`
- `Pocok.AppDefaults.Logging`
- `Pocok.AppDefaults.Logging.Serilog`

### Experimental, non-releasable set

- `Pocok.Modularity.Contracts`
- `Pocok.Modularity`
- `Pocok.AppDefaults.Modularity`

### Retired shapes

- `Pocok.Primitives`
- `Pocok.Hosting`
- `Pocok.Conversion.Abstractions`

## Material changes

- Generic Error/Result coupling was replaced by package-owned failure models.
- Conversion contracts and implementation were consolidated into one package.
- Conversion gained bounded recursive work, path-aware failures, explicit custom strategies, fuzz coverage, trim guidance, and a benchmark project.
- Hosting was renamed to Readiness and gained atomic snapshots, restart cycles, stale-token rejection, and concurrency-oriented tests.
- AppDefaults established one deliberately small explicit configurator contract.
- Logging defaults remain additive unless provider clearing is explicitly selected.
- Serilog integration reads standard Serilog configuration and forces no sink, file, endpoint, or static logger policy.
- Modularity uses manifest-led startup discovery, one BCL load context per plugin, explicit shared assemblies, staged service registration, and immutable diagnostics.
- Public package dependencies are governed by one catalog and one tag-driven publication workflow.
- Package smoke tests now distinguish complete local closure from candidate-plus-nuget.org publication rehearsal.
- Release builds pin internal dependency versions from existing package tags before restore, eliminating unpublished MinVer dependency edges.
- Publication pushes the exact candidate once, lets the NuGet client publish its matching symbols package, and determines GitHub prerelease status from the semantic version rather than the hyphenated tag prefix.

## Deliberate deviations from the original plan

- `Pocok.AppDefaults.Logging.Serilog` does not depend on `Pocok.AppDefaults.Logging`. They are alternative policies and coupling them would pull the built-in console provider into a Serilog-only application.
- Serilog version 1 ships no sink. Sink selection remains explicit application ownership and avoids file or network side effects.
- Modularity uses a small BCL `AssemblyLoadContext` adapter rather than adding McMaster.NETCore.Plugins. The decision and replacement threshold are recorded in ADR 0009.
- Modularity discovers all public parameterless `IServiceModule` implementations in the manifest-selected entry assembly rather than requiring a module type string. Discovery remains bounded to one explicit assembly and deterministic.
- Modularity is implemented for evaluation but remains non-releasable until executable cross-platform fixtures pass.
- Package API inventories are lightweight exported-type baselines in addition to NuGet package validation. A heavier API tool can replace them after the first real release if it provides enough value.

## Known issues for the stabilization session

- `.github/workflows/publish.yml` builds and tests the complete solution, so experimental Modularity can block every initial package release.
- The exported-type baseline is not a sufficient member-level API compatibility mechanism.
- All runtime, packaging, PowerShell, GitHub Actions, trimming, and cross-platform plugin claims remain unverified.

## Static verification performed

The final repository checks:

- JSON, XML, solution XML, and workflow YAML parsing;
- project-reference existence and repository containment;
- central package-version usage;
- package catalog uniqueness, release tiers, and dependency acyclicity;
- active packable-project/catalog agreement;
- publication trigger agreement with releasable entries;
- public exported-type inventory agreement by source inspection;
- required source headers;
- absence of retired project references and build artifacts;
- absence of copied origin archives or origin implementation trees.

## Execution status

The repository is now in an environment containing .NET 10 and PowerShell 7. Initial build and formatting issues have been resolved. Complete test execution and package validation remain as the next steps.
