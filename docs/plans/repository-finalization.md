# Pocok repository finalization and synchronized prerelease plan

**Plan date:** 2026-07-18  
**Status:** Ready for implementation; architecture and validation decisions are closed  
**Scope:** Package alpha-readiness, Scripting engine split, package/release tooling, synchronized publication, and final MVP evidence

## Execution contract

This is the executable repository plan. Current source remains authoritative if a filename has moved, but implementation agents must not redesign the selected approaches. A source mismatch is handled by updating the named integration point to its current equivalent, not by reopening a decision.

Use these skills and do not restate their generic procedure in session notes:

- `.agents/skills/pocok-package-engineering/SKILL.md` for R4-R5 package work;
- `.agents/skills/pocok-release-engineering/SKILL.md` for R2-R3 and R5-R10;
- `.agents/skills/pocok-showcase-engineering/SKILL.md` only at the two Showcase handoffs;
- `.agents/skills/pocok-agentic-workflow/SKILL.md` only for plan/handoff coordination.

Each slice is complete only when its commands and acceptance criteria pass at one recorded commit. Preserve a real failing command and stop the dependent slices when a gate fails. Fix defects while preserving the package's documented contract; do not weaken a gate, omit a target, publish a substitute version, or make a new public-contract decision inside the implementation slice.

## Locked decisions

| Area | Required implementation |
|---|---|
| Scripting exposure | JavaScript/Jint may run in the public Showcase. C# and Python are complete alpha packages but are disabled in Production and enabled only for explicitly trusted/local deployments. |
| Scripting architecture | `Pocok.Scripting` becomes engine-neutral with an explicit registry and engine ID. The current implicit Jint runner is removed; repository consumers migrate in the same slice. |
| C# execution | Roslyn validation/compilation plus a killable framework-dependent child worker. The worker is a non-packable project carried as a private adapter asset. |
| Python execution | External CPython 3.14 only, invoked as a child process with `-I -S`; no Python.NET dependency or CLR bridge. |
| Validation | Every engine has a standardized fail-closed validator. Validators are guardrails, never described as a sandbox. |
| Package policy | Replace `releasable` with `publicationPolicy: Disabled | PrereleaseOnly | StableAllowed`; API maturity remains in `state`. Remove hand-maintained `releaseTier`. |
| Global versions | Use `NuGet.Versioning`, include listed and unlisted versions, and suggest the lowest deterministic channel-preserving version greater than all targets. |
| Release implementation | Add one typed non-packable `Pocok.ReleaseTool`; existing PowerShell entry points become thin compatibility wrappers. |
| Concurrency | Package and global workflows use the same constant concurrency group with `cancel-in-progress: false` and `queue: max`. |
| Recovery | Audit once, upload the candidate as an Actions artifact and draft GitHub Release, then publish exact bytes. Recovery verifies and reuses those assets. |
| Source Link | Automated portable-PDB mapping, commit, URL, checksum, and installed-stack-frame proof is authoritative; GUI debugging is optional. |
| Deployment | The exact checked Render rollout PR is the deployment approval. Showcase S9 owns deployment and rollback. |

Workflow edits use these immutable action refs; implementation agents must not look up or float replacements during an MVP slice:

```text
actions/checkout@9c091bb21b7c1c1d1991bb908d89e4e9dddfe3e0 # v7.0.0
actions/setup-dotnet@26b0ec14cb23fa6904739307f278c14f94c95bf1 # v5.4.0
actions/setup-python@83679a892e2d95755f2dac6acb0bfd1e9ac5d548 # v6.1.0
actions/upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02 # v4.6.2
actions/download-artifact@d3f86a106a0bac45b974a628896c90dbdf5c8093 # v4.3.0
NuGet/login@d22cc5f58ff5b88bf9bd452535b4335137e24544 # v1.1.0
```

## Repository capabilities to reuse

| Need | Reuse |
|---|---|
| Application defaults and logging | `IApplicationConfigurator`, `ConfigureWith(...)`, `LoggingDefaultsConfigurator`, and the established AppDefaults logging configuration. Do not create a parallel logging bootstrap. |
| Intentional background activity | `Pocok.BackgroundWork`, including `.Observe(...)`, `TaskRepeater`, coalescing, and debouncing where the operation is genuinely background work. Release publication and propagation waits remain directly awaited. |
| Deterministic time | Existing `TimeProvider` patterns and test fakes. |
| Package identity and closure | `eng/packages.json`, current PackageCatalog scripts, PackageMetadata module, PackageSmoke consumers, and PublicReleaseAudit rules. Migrate logic rather than duplicating it. |
| Hosting/lifecycle | Existing AppDefaults, Readiness, options validation, DI, cancellation, and hosted-service patterns. |
| Modularity | Current manifest-led `AssemblyLoadContext` implementation and clean-room fixtures; do not introduce a second plugin framework. |
| Serialization | `System.Text.Json` with explicit bounded DTOs for process and manifest protocols. |
| Test stack | NUnit, Shouldly, Verify, existing package consumers/samples, Linux/Windows CI, and temporary feeds. |

## Target catalog and dependency truth

R2 changes the catalog to schema version 2. Every non-retired entry must end the MVP as `PrereleaseOnly` or `StableAllowed`; retired entries are `Disabled`. The five currently active packages retain `StableAllowed` only after their exact-candidate evidence is reconfirmed. Experimental packages remain `Experimental` while becoming `PrereleaseOnly` after their gates pass.

Add these public Scripting packages and exact tag prefixes:

| Package | Project | Tag prefix | Direct internal dependencies |
|---|---|---|---|
| `Pocok.Scripting` | `src/Scripting/Pocok.Scripting.csproj` | `scripting-v` | `Pocok.Conversion` |
| `Pocok.Scripting.JavaScript` | `src/Scripting.JavaScript/Pocok.Scripting.JavaScript.csproj` | `scripting.javascript-v` | `Pocok.Scripting` |
| `Pocok.Scripting.CSharp` | `src/Scripting.CSharp/Pocok.Scripting.CSharp.csproj` | `scripting.csharp-v` | `Pocok.Scripting` |
| `Pocok.Scripting.Python` | `src/Scripting.Python/Pocok.Scripting.Python.csproj` | `scripting.python-v` | `Pocok.Scripting` |

`src/Scripting.CSharp.Worker/Pocok.Scripting.CSharp.Worker.csproj` is non-packable and never enters the catalog. Showcase S4 later adds the six `ShowcaseBundle` entries and their exact library edges. All waves are derived from `internalDependencies`; no numeric release tier is authoritative.

## Sequential runbook

| Slice | Depends on | Handoff |
|---|---|---|
| R1 exact baseline | none | Recorded commit and honest failures |
| R2 typed release/catalog foundation | R1 | Schema v2 and tested `Pocok.ReleaseTool` |
| R3 package-tag coverage | R2 | Every eligible package prefix reaches the strict resolver |
| R4 package alpha gates and Scripting split | R1-R2 | Every library is an alpha candidate |
| R5 artifact and Source Link proof | R4 | Exact proven library artifacts |
| R6 library-only zero-push rehearsal | R2-R5 | Immutable library manifest |
| Showcase S1-S7 | R6 | Six proven sample bundles and three temporary-feed modes |
| R7 complete graph reconciliation | Showcase S7 | Library-plus-bundle graph and waves |
| R8 global/package workflow implementation | R7 | Recoverable zero-push workflows |
| R9 complete release rehearsal | R8 | Approved-ready immutable candidate |
| R10 synchronized prerelease | R9 plus explicit approval | Published graph or exact recovery state |
| Showcase S8-S9 | R10 | Public-feed composition and Render evidence |
| R11 final MVP closure | Showcase S9 | Current release handoff |

## R1. Establish the exact baseline

From a clean checkout at the candidate commit, run:

```powershell
pwsh -File tools/Ci/Test-CiTooling.ps1
pwsh -File tools/PackageCatalog/Test-PackageCatalog.ps1
pwsh -File tools/PackageMetadata/Test-PackageMetadata.ps1
dotnet restore Pocok.slnx --locked-mode
dotnet format Pocok.slnx --verify-no-changes --no-restore
dotnet build Pocok.slnx --configuration Release --no-restore
dotnet test Pocok.slnx --configuration Release --no-build
pwsh -File tools/PackageSmoke/Invoke-PackageSmoke.ps1 -Mode LocalClosure
pwsh -File tools/PublicReleaseAudit/Invoke-PublicReleaseAudit.ps1
```

The Linux CI must also run the focused reconnect regression:

```powershell
dotnet test tests/Unit/Signals.Tests/Pocok.Signals.Tests.csproj --configuration Release --filter FullyQualifiedName~SourceFailurePublishesFailureAndReconnects
```

Record the commit, SDK, PowerShell version, OS jobs, artifact directory, and each failing command in `docs/current-handoff.md`. Do not relabel old evidence as current.

**Accept when:** all commands and required Linux/Windows workflows pass, or the plan is stopped at the first real package/tool failure with its exact output and owner.

## R2. Implement the typed release and catalog foundation

### Projects and dependencies

Create non-packable projects:

- `tools/Pocok.ReleaseTool/Pocok.ReleaseTool.csproj`;
- `tests/Tooling/Pocok.ReleaseTool.Tests/Pocok.ReleaseTool.Tests.csproj`.

Add them to `Pocok.slnx`. Add central pins for `NuGet.Protocol` 7.6.0 and `NuGet.Versioning` 7.6.0 in `Directory.Packages.props`; reference both only from `Pocok.ReleaseTool`. Use `NuGet.Protocol` for service-index, flat-container, download, and visibility operations and `NuGet.Versioning` for every NuGet version parse/comparison. Use the `System.Reflection.Metadata` assembly supplied by .NET 10 for PDB inspection; do not add a separate package reference. Do not add the tool to `eng/packages.json`.

The tool exposes these stable commands; exit code `0` means success, `2` means invalid invocation/configuration, `3` means a policy/preflight rejection, and `4` means an unavailable or malformed external service response:

```text
catalog validate --catalog <path>
tag resolve --catalog <path> --tag <tag> [--output <json>]
graph write --catalog <path> --scope global|package --package-id <id> --output <json>
version preflight --catalog <path> --tag GLOBAL-v<version> --source <v3-url> --output <json>
manifest create --graph <json> --artifacts <dir> --commit <sha> --tag <tag> --output <json>
manifest verify --manifest <json> --artifacts <dir>
nuget verify --manifest <json> --source <v3-url> [--published-only]
nuget wait --package-id <id> --version <version> --source <v3-url> --timeout 00:10:00
sourcelink verify --manifest <json> --repository-root <path> [--connected]
```

Keep these paths as thin wrappers with their current parameter names where applicable:

- `tools/PackageCatalog/Test-PackageCatalog.ps1`;
- `tools/PackageCatalog/Resolve-PackageFromTag.ps1`;
- `tools/PackageCatalog/Resolve-PackageClosure.ps1`;
- `tools/PackageCatalog/New-ReleaseVersionsProps.ps1`.

Wrappers invoke the tool and do not retain a second graph, SemVer, or policy implementation. `PackageSmoke` and `PublicReleaseAudit` remain PowerShell and consume generated graph/manifest JSON.

### Catalog schema v2

Update `eng/packages.schema.json`, `eng/packages.json`, architecture tests, CI planning, metadata tooling, audits, and generated Showcase catalog consumption. Required fields per entry are:

```json
{
  "id": "Pocok.Example",
  "kind": "Library",
  "project": "src/Example/Pocok.Example.csproj",
  "tagPrefix": "example-v",
  "versionProperty": "PocokExamplePackageVersion",
  "family": "Capability",
  "state": "Experimental",
  "publicationPolicy": "PrereleaseOnly",
  "consumer": "ExampleConsumer",
  "internalDependencies": [],
  "allowedExternalDependencies": [],
  "proofProfile": "Library"
}
```

Allowed `kind`/`proofProfile` pairs are `Library/Library` and `ShowcaseBundle/ShowcaseBundle`. `family` also permits `Showcase`. Remove `releasable` and `releaseTier`. Validate project existence, package ID/tag/version-property agreement, unique identities, known non-retired edges, cycles, policy/channel agreement, real project references, and packed dependency agreement. A global graph includes every non-retired packable entry; any `Disabled` target rejects preflight rather than disappearing.

### Version policy

Resolve the NuGet V3 `PackageBaseAddress` from the service index and read its version index, which includes listed and unlisted versions. Normalize and compare with `NuGetVersion`/`VersionComparer.VersionRelease`.

For a conflicting prerelease request, preserve its nonnumeric prerelease identifiers and increment the final numeric identifier when that result exceeds the maximum; otherwise increment the required core patch and reapply the original suffix. For a stable request, choose the lowest stable core greater than the maximum. Print every conflict and one suggested `GLOBAL-v...` tag.

Release tags require exactly `major.minor.patch` with an optional SemVer 2 prerelease suffix; reject build metadata because NuGet package identity and flat-container normalization cannot provide a distinct immutable release for metadata-only differences. Offline fixtures cover 404/no versions, lower/equal/higher stable and prerelease versions, normalized versions, unlisted versions, malformed service documents, timeouts, metadata rejection, and the examples from the decision round. Connected tests are read-only and `[Category("External")]`.

**Validation:** 

```powershell
dotnet test tests/Tooling/Pocok.ReleaseTool.Tests/Pocok.ReleaseTool.Tests.csproj --configuration Release
dotnet run --project tools/Pocok.ReleaseTool -- catalog validate --catalog eng/packages.json
pwsh -File tools/Ci/Test-CiTooling.ps1
```

**Accept when:** the typed tool is authoritative, wrappers have parity fixtures, schema v2 describes the current graph, and no hand-maintained release tier or `releasable` check remains.

## R3. Fix every package-specific tag path

Change `.github/workflows/publish.yml` to:

```yaml
on:
  push:
    tags:
      - '*-v*'
      - '!GLOBAL-v*'

concurrency:
  group: pocok-publication
  cancel-in-progress: false
  queue: max
```

The trigger is deliberately broad; `Pocok.ReleaseTool tag resolve` is authoritative and rejects unknown, retired, disabled, malformed, wrong-channel, or ambiguous tags before restore or authentication. Use the locked action refs above. Update CI tooling fixtures so every catalog prefix is generated and resolved, while `GLOBAL-v*` and unknown prefixes are rejected.

Do not publish in this slice. Exercise the resolver with fixture tags for every `StableAllowed` and `PrereleaseOnly` entry; a stable tag for `PrereleaseOnly` must fail with exit code `3`.

**Accept when:** the current five-prefix limitation is gone, catalog additions cannot omit tag coverage silently, and package/global workflows share the exact concurrency group.

## R4. Close every library alpha gate

Run package work dependency-first. Existing public contracts remain unchanged unless this plan explicitly changes Scripting.

| Package group | Exact evidence required before policy change |
|---|---|
| Conversion | Unit/API snapshots, `Conversion.Console`, trimmed publish/run, installed consumer, package audit. |
| Readiness | Startup/failure/cancellation/shutdown/restart and concurrency tests, console sample, installed consumer. |
| AppDefaults, Logging, Logging.Serilog | Configurator ordering/duplicate-application/configuration tests, console/host consumer, dependency metadata, real structured log emission. Reuse AppDefaults logging; add no parallel bootstrap. |
| Modularity.Contracts, Modularity, AppDefaults.Modularity | Existing unit tests, `tests/Integration/Modularity.Tests` clean-room matrix on Linux/Windows, `samples/ModularCommunicator/Stage-Plugin.ps1`, host consumption through `IEnumerable<T>`, and Showcase plugin staging. The resolved design is `docs/implementation/modularity-spike.md`. |
| BackgroundWork | Existing observation/coalescing/debouncing/repetition suites with `ManualTimeProvider`, cancellation/disposal races, `.Observe(...)` sample, installed consumer. |
| Localization | JSON/RESX/culture/fallback/reload/disposal tests, BackgroundWork-backed reload, console sample, installed consumer. |
| Signals | Source identity/capabilities, live-value distinctions, write/read/subscription lifecycle, deterministic reconnect regression, console sample, installed consumer. |
| Subscriptions | Filtering/mapping, keyed ownership, concurrent add/remove, callback failure, disposal, console sample, installed consumer. |
| Licensing and AppDefaults.Licensing | Every command and behavior in `docs/licensing.md`, including keygen-to-checker, startup block/warn, periodic revalidation, package consumers, and Linux/Windows proof. Private keys and secrets never enter artifacts/logs. |

For each group run its focused tests first, then:

```powershell
pwsh -File tools/PackageSmoke/Invoke-PackageSmoke.ps1 -Mode LocalClosure -PackageIds <comma-separated-ids>
pwsh -File tools/PublicReleaseAudit/Invoke-PublicReleaseAudit.ps1 -PackageIds <comma-separated-ids>
```

Change `publicationPolicy` only after the exact commit passes: proven experimental packages become `PrereleaseOnly`; the five existing stable candidates remain `StableAllowed` only if reconfirmed. Update package README compatibility/security sections, API snapshots, `PUBLICATION.md`, generated views, and `docs/current-handoff.md` in the same slice.

### R4a. Split the Scripting engine family

Create:

- `src/Scripting.JavaScript` and `tests/Unit/Scripting.JavaScript.Tests`;
- `src/Scripting.CSharp`, `src/Scripting.CSharp.Worker`, and `tests/Unit/Scripting.CSharp.Tests`;
- `src/Scripting.Python` and `tests/Unit/Scripting.Python.Tests`.

Update `Pocok.slnx`, `Directory.Packages.props`, package consumers, `samples/Scripting.Console`, API snapshots, catalog entries, CI ownership, and package documentation.

The neutral core owns these exact contracts under `Pocok.Scripting.Execution`:

```csharp
public readonly record struct ScriptEngineId(string Value);
public sealed record ScriptEngineCapabilities(
    bool EnforcesHardTimeout,
    bool EnforcesCancellation,
    bool EnforcesStatementLimit,
    bool EnforcesRecursionLimit,
    bool EnforcesMemoryLimit);
public sealed record ScriptEngineDescriptor(
    ScriptEngineId Id,
    string Language,
    bool IsAvailable,
    ScriptEngineCapabilities Capabilities,
    string? UnavailableCode = null,
    string? UnavailableMessage = null);
public interface IScriptValidator
{
    ScriptEngineId EngineId { get; }
    ValueTask<ScriptValidationResult> ValidateAsync(
        ScriptExecutionRequest request,
        ScriptExecutionOptions options,
        CancellationToken cancellationToken = default);
}
public interface IScriptEngineAdapter
{
    ScriptEngineDescriptor Descriptor { get; }
    IScriptValidator Validator { get; }
    ValueTask<ScriptResult<object?>> ExecuteAsync(
        ValidatedScript script,
        ScriptExecutionOptions options,
        CancellationToken cancellationToken = default);
}
public sealed class ScriptEngineRegistry;
public sealed class ScriptRunner;
```

`ValidatedScript` is a public sealed immutable token with an internal constructor; adapters can read it but callers cannot construct it. `ScriptValidationResult` is an immutable success/diagnostic collection, and each diagnostic contains code, safe message, severity, and optional line/column. `ScriptExecutionRequest` contains engine ID, request identifier, source, expected-result flag, and explicit bindings. `ScriptExecutionOptions` contains timeout, maximum source characters, maximum output bytes, and nullable statement/recursion/memory limits.

`ScriptRunner` requires an explicit registry, rejects duplicate/unknown/unavailable engines, enforces common source/output/time bounds, invokes the registered `IScriptValidator`, creates `ValidatedScript` only after success, and only then calls the adapter. A requested nullable limit that the descriptor cannot enforce is rejected as `scripting.limit.unsupported`. Remove the parameterless implicit-Jint runner. Retain `ScriptResult<T>` conversion behavior through `Pocok.Conversion`; normalize cross-process values to JSON-compatible null/bool/string/number/list/object shapes. Expected failures contain code/message/line/column and no raw worker exception or source text.

Make imports engine-aware. Keep module/reference/source and deterministic graph resolution in core with an engine ID; move `// #import` parsing/injection to JavaScript. Add C# and Python parsers only for their own syntax. Persistent repositories, file watching, and multi-file workspaces remain post-MVP.

Common limits are immutable `ScriptExecutionOptions`: source characters, output bytes, timeout, and cancellation. Engine descriptors separately report hard timeout/cancellation, statements, recursion, and memory support. A request containing an unsupported mandatory bound is rejected; it is never silently ignored.

#### JavaScript adapter

Move Jint and all Jint-specific options/failures into `Pocok.Scripting.JavaScript`. Preserve strict mode, disabled CLR interop, `AllowGetType=false`, disabled string compilation, explicit scalar/function bindings, statement/recursion/memory/time/cancellation limits. Inspect the parsed JavaScript AST before engine creation and reject direct/aliased `eval`, `Function` construction, dynamic import/code construction, and unavailable host capabilities.

#### C# adapter and worker

Use `Microsoft.CodeAnalysis.CSharp` 5.6.0 in the non-packable worker only; add that exact central pin to `Directory.Packages.props`. The public `Pocok.Scripting.CSharp` adapter must not load Roslyn into the host process. Its validator invokes the worker's `--validate` operation, which performs Roslyn syntax and semantic analysis with a fixed metadata-reference allowlist. Its execution operation compiles the already validated source in a fresh worker process. Defaults import only:

```text
System
System.Collections.Generic
System.Linq
System.Threading
System.Threading.Tasks
```

`System.IO`, `System.Net*`, `System.Reflection`, `System.Runtime.Loader`, `System.Diagnostics`, `Microsoft.Win32`, unsafe code, P/Invoke, dynamic assembly/reference loading, `#r`, and `#load` are not default capabilities and are rejected. Additional imports/references must be preconfigured at adapter registration and intersect the host allowlist; request text cannot add references.

Compile a bounded wrapper to the non-packable worker and execute with a versioned JSON stdin/stdout protocol. Publish the worker as a framework-dependent .NET 10 application, including its private Roslyn dependency closure. Put that complete output and a SHA-256 manifest under `tools/csharp-worker/` in the adapter package; `buildTransitive/Pocok.Scripting.CSharp.targets` copies it to `Pocok.Scripting/CSharpWorker/` on build/publish without compile assets or transitive Roslyn references. Resolve the host only from configured `DotNetHostPath`, then `DOTNET_HOST_PATH`; no arbitrary PATH search. Missing/mismatched/hash-invalid assets make the descriptor unavailable. Capture bounded stdout/stderr asynchronously and use `Process.Kill(entireProcessTree: true)` on cancellation/timeout.

#### Python adapter

Support CPython `3.14.x` only. Resolve the executable only from configured `PythonExecutable`, then `POCOK_PYTHON_EXECUTABLE`; probe `sys.implementation.name` and `sys.version_info` before reporting availability. Invoke `python -I -S` without a shell, with a sanitized environment, worker directory as the working directory, bounded stdin/stdout/stderr, and kill-tree cancellation.

Run validation in a separate `--validate` worker invocation using Python's `ast` module. Reject `eval`, `exec`, `compile`, `__import__`, dynamic import, dunder traversal, native/process/environment/filesystem/network access, and any non-allowlisted import. Execute only after validation success. Do not expose CLR objects or Python.NET.

Neither child adapter logs source, bindings, results, environment, paths, or stderr verbatim. It returns safe failure codes and bounded diagnostics; callers use their existing logger. The Showcase uses its existing AppDefaults logging and allowlisted in-app sink.

**Focused validation:** 

```powershell
dotnet test tests/Unit/Scripting.Tests/Pocok.Scripting.Tests.csproj --configuration Release
dotnet test tests/Unit/Scripting.JavaScript.Tests/Pocok.Scripting.JavaScript.Tests.csproj --configuration Release
dotnet test tests/Unit/Scripting.CSharp.Tests/Pocok.Scripting.CSharp.Tests.csproj --configuration Release
$env:POCOK_PYTHON_EXECUTABLE = (Get-Command python).Source
python --version
dotnet test tests/Unit/Scripting.Python.Tests/Pocok.Scripting.Python.Tests.csproj --configuration Release
dotnet run --project samples/Scripting.Console/Pocok.Scripting.Console.csproj --configuration Release
```

CI uses `actions/setup-python` with `python-version: '3.14'` on Linux and Windows. Tests cover registry selection, unavailable runtimes, validator-before-execution, harmless bypass spellings, false positives, source/output bounds, worker hash/protocol errors, cancellation, timeout kill, orphan prevention, disposal, JSON result normalization, default/configured imports, and clean installed-package execution.

**R4 accept when:** every non-retired library has current package/API/consumer/platform evidence and is at least `PrereleaseOnly`; Scripting has four independently packable public packages and no public Production path to C# or Python.

## R5. Prove exact artifacts and Source Link

Extend `PublicReleaseAudit` by `proofProfile`. `Library` packages require DLL/XML in `lib/net10.0`, `.snupkg`, exact dependency metadata, README/LICENSE/NOTICE, repository commit, deterministic hashes, and installed consumer proof. Showcase bundles are handled after S4 by their own profile.

Implement `sourcelink verify` in `Pocok.ReleaseTool`:

1. open each portable PDB from the exact `.snupkg`;
2. read Source Link mappings and document checksums with `System.Reflection.Metadata`;
3. require the nuspec repository URL and commit to equal the candidate manifest;
4. resolve every document to a file at that exact local Git commit and compare checksums;
5. with `--connected`, fetch at least one generated commit-specific source URL per package and compare the same checksum;
6. run an installed consumer that produces a controlled stack frame and verify its file/line maps to a validated document.

If raw GitHub is unavailable, the local Git-object proof still runs but connected proof is recorded failed, not skipped; live release cannot proceed until connected proof passes.

**Accept when:** one manifest ties every library nupkg/snupkg, SHA-256, dependency, repository commit, portable PDB, source mapping, and installed consumer to the same R4 commit.

## R6. Rehearse the library graph without pushing

Generate exact version properties before restore. Build, test, pack, and audit once in a clean candidate directory; do not rebuild after manifest creation.

```powershell
dotnet run --project tools/Pocok.ReleaseTool -- graph write --catalog eng/packages.json --scope global --output artifacts/release/graph.json
dotnet run --project tools/Pocok.ReleaseTool -- manifest create --graph artifacts/release/graph.json --artifacts artifacts/packages --commit <sha> --tag GLOBAL-v<version> --output artifacts/release/manifest.json
dotnet run --project tools/Pocok.ReleaseTool -- manifest verify --manifest artifacts/release/manifest.json --artifacts artifacts/packages
pwsh -File tools/PackageSmoke/Invoke-PackageSmoke.ps1 -NoPack -Mode LocalClosure
pwsh -File tools/PublicReleaseAudit/Invoke-PublicReleaseAudit.ps1
```

Use an isolated temporary NuGet config/cache and exact package-source mapping. Every internal dependency in packed nuspec metadata must use the synchronized exact version. No authentication or push occurs.

**Accept when:** all library artifacts restore and run from the temporary feed, the manifest is complete, and its hashes survive a second verification.

## Showcase handoff A

Execute S1-S7 of `docs/plans/showcase-revision.md`. Return only when six `ShowcaseBundle` packages are catalogued, alpha-ready, and proven in all three source modes against a temporary feed. No nuget.org publication or Render mutation occurs in that handoff.

## R7. Reconcile the complete library-plus-bundle graph

Re-run catalog validation after Showcase S4 adds:

- `Pocok.Showcase.Conversion`;
- `Pocok.Showcase.Scripting`;
- `Pocok.Showcase.Licensing`;
- `Pocok.Showcase.AppDefaults.Logging`;
- `Pocok.Showcase.Readiness`;
- `Pocok.Showcase.Localization`.

Every bundle has `kind: ShowcaseBundle`, `family: Showcase`, `proofProfile: ShowcaseBundle`, `publicationPolicy: PrereleaseOnly`, its own `showcase.<name>-v` tag prefix/version property, `Pocok.Modularity.Contracts` as a direct internal edge, and every exact demonstrated/bundled Pocok library as an edge. The packed bundle dependency manifest and catalog edges must agree even though host-shared assemblies are excluded from its payload.

During candidate construction, pack/audit library waves first into an isolated candidate feed without rebuilding them. Restore and publish each Showcase bundle against those exact candidate packages (`PocokShowcaseLibrarySource=NuGet` and the synchronized version), then pack the resulting deterministic plugin tree. A global candidate must not build bundle payloads from library ProjectReferences.

Derive deterministic waves using Kahn's algorithm with ordinal package-ID ordering inside a wave. Reject unknown/retired/self/cyclic edges and mismatches with project or bundle metadata.

**Accept when:** graph fixtures cover roots, diamonds, all adapters, bundles, cycles, disabled targets, and deterministic order; the real graph contains every non-retired packable entry exactly once.

## R8. Implement recoverable package and global workflows

Create `.github/workflows/publish-global.yml` with only:

```yaml
on:
  push:
    tags: ['GLOBAL-v*']
concurrency:
  group: pocok-publication
  cancel-in-progress: false
  queue: max
```

Both publication workflows use the typed tool, immutable candidate builder, audits, and shared concurrency. `GLOBAL-v<major.minor.patch[-prerelease]>` is case-sensitive and forbids build metadata. A fresh run requires the requested version to exceed every listed/unlisted version for every target and to satisfy each target's publication policy. Preflight runs before restore, NuGet authentication, or GitHub Release creation.

After zero-push proof, upload the complete candidate as an Actions artifact and create a draft GitHub Release for the verified existing tag. Attach manifest, graph, hashes, nupkg/snupkg files, and bundle packages before the first push. Authenticate with NuGet Trusted Publishing immediately before publication.

For each graph wave:

1. push each exact missing artifact by explicit manifest path;
2. wait at most ten minutes per package with 2/4/8/15/30-second capped backoff and `Retry-After` support;
3. require the exact normalized version in the flat container;
4. download it, verify nuspec repository commit and package SHA-256 against the manifest;
5. restore a clean generated consumer from nuget.org with global packages/cache/config isolated;
6. advance only after every package in the wave succeeds.

Do not wildcard-push. If any item fails, stop all later waves and summarize published, verified, failed, blocked, and pending IDs plus the draft-release recovery URL.

Recovery reruns download the draft assets, verify tag commit/manifest/hashes, and compare already-published packages byte-for-byte. Continue only missing packages. An equal version with unknown provenance or any higher version is a hard failure. Never rebuild, delete, unlist, or overwrite as recovery.

On complete success, publish the existing draft GitHub Release, mark prerelease from the tag, and include the wave summary. Package-specific releases reuse the same primitives but publish only the candidate's dependency closure and obey its channel policy.

**Accept when:** offline/disposable-feed workflow fixtures prove preflight, queueing, wave waits, token/auth failure, malformed NuGet responses, propagation timeout, partial publication, safe resume, and final release publication without making a live push.

## R9. Rehearse the complete release

At one unused prerelease version, run the complete library-plus-bundle path with push disabled and a disposable HTTP NuGet feed that delays visibility for fixtures. Build once, run all package and Showcase temporary-feed evidence, create the draft-release-shaped asset set locally, then execute publication/resume simulations.

The final rehearsal manifest contains commit/tag/version, policy for every target, nodes/edges/waves, exact artifact names/hashes, package kinds, repository metadata, Source Link results, local-feed consumer results, current-local Showcase ledger, and all three temporary-feed composition results. It contains no credentials, absolute paths, cache paths, or secret feed URLs.

**Accept when:** every target appears exactly once, every proof is green, rerunning manifest verification is byte-identical, and no package/tag/release was published.

## R10. Perform the synchronized prerelease

**Approval gate:** present the exact commit, proposed `GLOBAL-v...` tag, target list, graph/waves, R9 manifest hash, and zero-push summary. Do not create or push the tag without explicit approval.

After approval:

1. rerun connected version preflight;
2. if it conflicts, stop and present the tool's suggested tag for new approval;
3. create one annotated tag at the approved commit and push that tag only;
4. observe the queued workflow through every wave;
5. verify all public packages with clean nuget.org consumers;
6. record the published GitHub Release and exact version for Showcase S8.

If publication stops, follow only the manifest-proven recovery path in R8 or request approval for a new globally valid version. NuGet immutability means there is no rollback.

**Accept when:** every manifest target and the GitHub Release is publicly available at the exact version, or the handoff lists immutable published/pending state and the single safe recovery command.

## Showcase handoff B

Execute S8-S9 of `docs/plans/showcase-revision.md`: clean nuget.org composition, approved rollout PR, Render checked deployment, live smoke, and rollback if required.

## R11. Final MVP closure

Run the R1 command set plus connected Source Link, clean public-feed consumers for every package, Showcase local/public modes, Playwright, Docker, and deployed-route smoke at the final commit. Label local-current, temporary-feed, nuget.org, and Render evidence separately.

Verify:

- every non-retired library and bundle is at least `PrereleaseOnly`;
- every package-specific prefix resolves through `publish.yml`;
- the global release contains the complete graph at one version;
- `PUBLICATION.md`, README package tables, architecture records, `docs/current-handoff.md`, and the resolved Modularity spike state only observed behavior;
- the former blanket rule against experimental publication is gone and replaced by `publicationPolicy` channel enforcement;
- remaining work is limited to minor UI defects or `post-mvp-roadmap.md`.

**Accept when:** every required command/evidence layer passes on the final commit. Otherwise the MVP remains open with the exact failure, artifact/manifest, owner, and next executable action.

## External prerequisites and deterministic fallbacks

| Prerequisite | Required behavior | Fallback |
|---|---|---|
| .NET 10 SDK and PowerShell 7 | Versions pinned/checked by repository tooling | Stop before evidence; do not substitute SDK/runtime versions. |
| CPython 3.14 | `POCOK_PYTHON_EXECUTABLE` points to probed CPython 3.14 | Python engine reports unavailable; R4 cannot close until Linux/Windows 3.14 CI passes. |
| NuGet V3/nuget.org | Connected preflight, Source Link, publication, and restore | Offline fixtures and temporary feed permit rehearsal only; never claim publication. |
| NuGet Trusted Publishing | Repository/environment mapping and `NUGET_USERNAME` configured | Stop before first push; candidate remains in the draft release. |
| GitHub Actions/Release permissions | `contents: write`, `id-token: write`, queue support | Local/disposable rehearsal only; never emulate publication with local tags. |
| raw GitHub source access | Connected Source Link checksum proof | Local Git-object proof is retained, but release stays blocked until connected proof passes. |
| Render access | Owned by Showcase S9 | Local exact Docker image and smoke are recovery evidence, not deployment evidence. |
