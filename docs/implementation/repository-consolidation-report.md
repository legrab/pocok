# Repository consolidation report

## Current result

The repository is a coherent implementation candidate for five initial public packages and three experimental Modularity packages. The previous executable baseline passed 182 tests. The V2 follow-up finalized pending Modularity WIP, implemented package-semantic Wave C, and made AppDefaults policy Wave D explicit. Those latest changes still require a fresh .NET 10 and PowerShell 7 acceptance run before release.

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

## V2 improvements

### Package semantics

- One dependency-first closure resolver now drives release packing, smoke feeds, catalog validation, and candidate-scoped auditing.
- Local-closure smoke uses only the exact Pocok closure and package source mapping prevents nuget.org fallback for `Pocok.*`.
- Publication smoke keeps only the candidate local and requires internal dependencies to resolve from nuget.org.
- Local smoke packing can pack only requested package closures instead of the whole repository.
- The publication workflow builds the core release graph but packs and audits only the candidate closure.
- The audit rejects missing, duplicate, stale, unrelated, and repository-contaminated artifacts and checks concrete dependency versions.
- CI executes core samples and publishes and runs the trimmed Conversion sample.

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

The repository passed static checks for JSON, XML, workflow YAML, project paths, package catalog consistency, dependency acyclicity, package closure ordering, source headers, retired references, documentation links, and Git cleanliness.

The V2 environment had no .NET SDK or PowerShell runtime. The latest C#, MSBuild, PowerShell, package, and workflow changes have not been executed here. This is the only release-blocking uncertainty for the initial package set. See the implementation ledger and `PUBLICATION.md` for the exact acceptance commands.
