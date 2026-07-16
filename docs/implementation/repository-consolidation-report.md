# Repository consolidation report

## Current result

The repository is a coherent implementation candidate for five intended initial public packages, three experimental Modularity packages, and four additional experimental capability packages. The current .NET 10 and PowerShell 7 acceptance run passed formatting, the Release build, 231 tests, core samples, package catalog validation, local-closure smoke, and public release audit. The experimental packages remain non-releasable pending their documented cross-platform and publication-shaped gates.

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
- `Pocok.Scripting`
- `Pocok.Signals`
- `Pocok.Localization`
- `Pocok.Subscriptions`

### Retired shapes

- `Pocok.Primitives`
- `Pocok.Hosting`
- `Pocok.Conversion.Abstractions`

## Consolidation outcome

- Generic Error/Result coupling was replaced by package-owned failure models.
- Conversion contracts and implementation were consolidated into one package.
- Hosting was renamed to Readiness and gained atomic snapshots, restart cycles, stale-token rejection, and concurrency-oriented tests.
- AppDefaults established one deliberately small explicit configurator contract.
- Provider-neutral logging and Serilog policy remain separate packages.
- Modularity remains startup-only, trusted, manifest-led, and non-releasable.
- Public package dependencies, tags, versions, consumers, and release tiers are governed by one catalog.
- Member-level public API snapshots use Verify and PublicApiGenerator.
- The five initial packages are isolated from experimental Modularity through `Pocok.Core.slnx`.
- Four neutral capability slices are independently packageable, tested, documented, and covered by installed-package smoke consumers while remaining experimental.

## V2 improvements

### Package semantics

- One dependency-first closure resolver now drives release packing, smoke feeds, catalog validation, and candidate-scoped auditing.
- Local-closure smoke uses only the exact Pocok closure and package source mapping prevents nuget.org fallback for `Pocok.*`.
- Publication smoke keeps only the candidate local and requires internal dependencies to resolve from nuget.org.
- Local smoke packing can pack only requested package closures instead of the whole repository.
- The publication workflow builds the core release graph but packs and audits only the candidate closure.
- The audit rejects missing, duplicate, stale, unrelated, and repository-contaminated artifacts and checks concrete dependency versions.
- CI executes core samples and publishes and runs an explicit trimmed-array Conversion smoke fixture.
- General Conversion APIs are marked with `RequiresUnreferencedCode`; the fixture is a narrow opt-in regression check, not a package-wide trim-compatibility claim.

### AppDefaults policy

- Configuration binding precedes code delegates.
- Duplicate concern configurator application throws instead of hiding later conflicting options.
- Resolved startup policy is exposed as `IOptions<T>`.
- Logging providers remain additive unless explicit clearing is selected.
- Simple-console registration is disabled by default because the standard host already owns providers.
- Serilog remains sink-free and application-owned.
- The same duplicate and options policy is applied to experimental Modularity defaults.

### Pending Modularity WIP

- Redundant tests tied to local output layouts were removed.
- Required duplicate module IDs remain fatal.
- Optional duplicate module IDs remain diagnostics unless strict optional failure mode is selected.
- The stronger external plugin integration fixtures remain the authoritative loader tests.

## Deliberate boundaries

- Capability packages do not depend on AppDefaults.
- `Pocok.AppDefaults.Logging.Serilog` does not depend on `Pocok.AppDefaults.Logging`.
- No public Common, Utils, Foundation, or generic Primitives package exists.
- No application-specific origin implementation is copied into the package portfolio.
- Modularity is trusted in-process extension loading, not a security sandbox.
- Runtime plugin installation, hot reload, child containers, and unload guarantees remain out of scope.

## Validation status

The repository passed static checks for JSON, XML, workflow YAML, project paths, package catalog consistency, dependency acyclicity, package closure ordering, source headers, retired references, documentation links, and Git cleanliness. The current Windows acceptance run also passed the executable build, tests, samples, package catalog validation, local-closure smoke, and public release audit. Cross-platform CI, debugger Source Link verification from an installed candidate package, and publication-shaped restore remain release gates.
