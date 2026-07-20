# Pocok library release-readiness plan

**Plan date:** 2026-07-20  
**Status:** Implemented in source; retained for exact rehearsal, approval, and first synchronized release  
**Depends on:** Current `main` source and executable evidence  
**Produces:** One approved, synchronized prerelease of every non-retired Pocok library  
**Followed by:** [`mvp-closure.md`](mvp-closure.md)

## Purpose

Finish the existing library set as honest alpha packages before investing in the larger Showcase packaging and release-tooling system.

At completion:

- every non-retired library has current package, API, consumer, and platform evidence;
- all eighteen library packages are represented in the schema-v1 catalog and are `releasable: true` only after their exact gates pass;
- package-specific tag coverage includes every releasable package;
- the existing `GLOBAL-v*` workflow can publish the complete eighteen-library graph at one approved prerelease version;
- the local Showcase covers all eighteen libraries through ten package-owned plugins;
- Scripting has an engine-neutral core plus JavaScript, C#/.NET, and Python adapters;
- the Scripting Showcase offers equivalent samples for all three languages through one Showcase-internal Monaco wrapper.

This plan intentionally stops before typed schema-v2 release tooling, Showcase bundle packages, NuGet-backed Showcase composition, public C#/Python execution, and Render rollout. Those belong to [`mvp-closure.md`](mvp-closure.md) or the post-MVP roadmap.

> **Reading note:** References below to “current” or “non-releasable” packages describe the plan baseline before its
> implementation. The implemented state is summarized above and remains authoritative in `eng/packages.json`.

## Execution contract

Use only the skills required by the active slice:

- `.agents/skills/pocok-package-engineering/SKILL.md` for libraries, public APIs, tests, samples, and package consumers;
- `.agents/skills/pocok-showcase-engineering/SKILL.md` for shared Showcase components and plugins;
- `.agents/skills/pocok-release-engineering/SKILL.md` for catalog eligibility, package tags, smoke/audit, and global release preparation;
- `.agents/skills/pocok-agentic-workflow/SKILL.md` for plan coordination and the final approval handoff.

Current source and fresh executable evidence outrank this plan when a path has moved. A moved integration point must be mapped to its current equivalent without reopening the selected architecture.

Do not:

- create or push a tag without explicit approval;
- publish packages, use credentials, or change deployment;
- mark a package releasable because documentation or static inspection looks complete;
- weaken a failing test, smoke check, audit, security boundary, or platform gate;
- replace existing Pocok infrastructure with a parallel framework;
- describe validators or child processes as an operating-system sandbox.

A package can remain `state: Experimental` while becoming eligible for alpha publication. Schema-v1 `releasable` is the publication gate in this plan; schema-v2 `publicationPolicy` is deferred to MVP Closure.

## Current baseline to preserve

The current schema-v1 catalog has fifteen non-retired libraries:

| Current state | Packages |
|---|---|
| `releasable: true` | `Pocok.Conversion`, `Pocok.Readiness`, `Pocok.AppDefaults`, `Pocok.AppDefaults.Logging`, `Pocok.AppDefaults.Logging.Serilog`, `Pocok.Scripting`, `Pocok.Licensing`, `Pocok.AppDefaults.Licensing` |
| `state: Experimental`, `releasable: false` | `Pocok.BackgroundWork`, `Pocok.Localization`, `Pocok.Modularity.Contracts`, `Pocok.Modularity`, `Pocok.AppDefaults.Modularity`, `Pocok.Signals`, `Pocok.Subscriptions` |

The seven non-releasable packages already have substantial source, tests, API tracking, samples or real consumers, catalog entries, and installed-package consumers. Their work starts with a gap audit and adds only evidence or fixes that are actually missing.

The existing `Pocok.Scripting` package still contains Jint and exposes an implicit JavaScript runner. Splitting it adds three public adapters while retaining the core package, resulting in exactly eighteen non-retired libraries.

The existing schema-v1 global workflow already provides:

- one `GLOBAL-v*` trigger;
- deterministic dependency-first ordering;
- shared `pocok-publication` concurrency with `cancel-in-progress: false` and `queue: max`;
- exact-version and repository-provenance preflight;
- candidate and state retention;
- draft GitHub Release handling;
- explicit sequential pushes;
- propagation checks;
- scoped PackageSmoke and PublicReleaseAudit;
- provenance-aware equal-version resume.

It rejects more than eighteen targets. The library-only graph created here reaches exactly eighteen, so Release Readiness may use this workflow without redesigning its orchestration. Future Showcase bundles require a capacity and artifact-reuse upgrade in MVP Closure.

## Locked decisions

| Area | Decision |
|---|---|
| Scripting package shape | Engine-neutral `Pocok.Scripting` plus independently packable JavaScript, C#, and Python adapters |
| Public script execution | JavaScript may run in the public Showcase; C# and Python require an explicit trusted/local operator setting |
| Validation language | Every engine validates before execution; validation is a fail-closed guardrail, not a sandbox |
| C# execution | Roslyn-owned validation/compilation in a killable framework-dependent private worker |
| Python execution | External CPython 3.14 invoked with `-I -S`; no Python.NET or CLR bridge |
| Editor | One Showcase-internal, locally served Monaco wrapper with buffered-textarea fallback |
| Remaining Showcase coverage | Logging and Localization are real bounded demonstrations; Readiness, BackgroundWork, Modularity, Signals, and Subscriptions are constrained configuration/usage recipe builders |
| Catalog | Keep schema v1, `releaseTier`, and `releasable` until MVP Closure |
| Global publication | Reuse the merged workflow and its eighteen-package cap for the first all-library prerelease |
| Showcase packaging | All ten plugins remain non-packable local Showcase modules in this plan |

## Target package graph after the Scripting split

Add these public package identities:

| Package | Project | Tag prefix | Version property | Direct internal dependencies |
|---|---|---|---|---|
| `Pocok.Scripting` | `src/Scripting/Pocok.Scripting.csproj` | `scripting-v` | `PocokScriptingPackageVersion` | `Pocok.Conversion` |
| `Pocok.Scripting.JavaScript` | `src/Scripting.JavaScript/Pocok.Scripting.JavaScript.csproj` | `scripting.javascript-v` | `PocokScriptingJavaScriptPackageVersion` | `Pocok.Scripting` |
| `Pocok.Scripting.CSharp` | `src/Scripting.CSharp/Pocok.Scripting.CSharp.csproj` | `scripting.csharp-v` | `PocokScriptingCSharpPackageVersion` | `Pocok.Scripting` |
| `Pocok.Scripting.Python` | `src/Scripting.Python/Pocok.Scripting.Python.csproj` | `scripting.python-v` | `PocokScriptingPythonPackageVersion` | `Pocok.Scripting` |

Also create the private, non-packable worker:

```text
src/Scripting.CSharp.Worker/Pocok.Scripting.CSharp.Worker.csproj
```

Use dedicated catalog consumers named consistently with the current convention:

```text
ScriptingConsumer
ScriptingJavaScriptConsumer
ScriptingCSharpConsumer
ScriptingPythonConsumer
```

The C# worker is not a package, has no tag, and never enters `eng/packages.json`.

## Sequential runbook

| Slice | Depends on | Completion handoff |
|---|---|---|
| RR1. Exact baseline and gap inventory | none | Current evidence matrix and exact failures |
| RR2. Split and harden Scripting | RR1 | Four alpha-candidate Scripting packages |
| RR3. Shared Monaco and three-engine Showcase | RR2 | One local Scripting plugin covering all engines |
| RR4. Close the seven remaining package gates | RR1; may run alongside RR2-RR3 | Every library has sufficient alpha evidence |
| RR5. Add Logging and Localization plugins | RR1 and relevant APIs | Two real bounded Showcase demonstrations |
| RR6. Add five recipe-builder plugins | RR1 and relevant APIs | Lightweight local coverage for seven packages |
| RR7. Reconcile coverage, catalog, tags, and docs | RR2-RR6 | Eighteen releasable library candidates and ten plugins |
| RR8. Rehearse and perform the first global prerelease | RR7 | Publicly verified release or exact resumable partial state |

Independent package slices may continue if an unrelated external prerequisite is unavailable. The blocked package remains non-releasable, the global release remains blocked, and the exact failure is recorded.

# Phase 1 — Scripting family and shared editor

## RR1. Establish the exact baseline and gap inventory

Start from a clean checkout of the candidate commit.

### Inspect

For every non-retired catalog package, record:

- catalog identity, state, eligibility, tier, dependencies, tag prefix, version property, and consumer;
- current project and package references;
- public API snapshot status;
- focused unit/integration tests;
- console sample or real repository consumer;
- installed-package consumer;
- package smoke and public-audit expectations;
- platform-specific gates;
- README security, lifecycle, limitations, and compatibility coverage.

For Scripting, additionally record:

- every public type and current repository consumer that assumes JavaScript;
- Jint-specific code and package references that must move;
- import/module behavior that is engine-neutral versus JavaScript syntax-specific;
- source/output/statement/recursion/memory/time/cancellation behavior;
- current unsafe diagnostic paths, including source excerpts or raw exceptions;
- Showcase source state, sample/reset behavior, code-assist metadata, and editor integration;
- expected package count and topological order after adding adapters.

For the global workflow, verify rather than redesign:

- both publish workflows share `group: pocok-publication`, `cancel-in-progress: false`, and `queue: max`;
- `GLOBAL-v*` target resolution uses only current `releasable: true` entries;
- the package limit is eighteen;
- smoke and audit receive the resolved target IDs;
- the latest tag-commit resolution fix is present;
- equal-version resume checks repository provenance.

### Baseline commands

Run repository-standard commands from the current source. At minimum:

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

Also run the current Showcase build, tests, clean publish, and real-process smoke using the commands documented in `showcase/docs/ADDING_A_SLICE.md`.

Do not treat unavailable NuGet access or an unsuitable chat container as proof of failure. Preserve the exact command and classify it as environment-blocked. Hosted Linux/Windows CI remains authoritative for cross-platform completion.

### Deliverable

Update `docs/current-handoff.md` only with observed evidence:

- exact commit;
- SDK, PowerShell, Python, and OS;
- commands run;
- pass/fail/environment-blocked classification;
- package-specific gaps;
- no inferred eligibility changes.

**Accept when:** all current facts needed by RR2-RR8 are recorded, no package is marked releasable based only on the inventory, and every design-sensitive Scripting integration point is mapped to a concrete current file.

## RR2. Split and harden the Scripting engine family

### RR2.1 Neutral core

Move Jint and JavaScript syntax concerns out of `src/Scripting`.

The core owns contracts under an engine-neutral namespace such as `Pocok.Scripting.Execution`:

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

The implementation may adjust constructor and property syntax to match repository conventions, but it must preserve these semantics:

- `ValidatedScript` is public and immutable so adapters can consume it, but callers cannot construct it;
- `ScriptValidationResult` is immutable and contains safe diagnostics;
- each diagnostic has code, safe message, severity, and optional line/column;
- `ScriptExecutionRequest` contains engine ID, request identifier, source, expected-result flag, and explicit bindings;
- `ScriptExecutionOptions` contains timeout, maximum source characters, maximum output bytes, and nullable engine-specific limits;
- the runner requires an explicit registry;
- duplicate, unknown, and unavailable engines fail safely;
- source and output bounds are common;
- a validator always runs before adapter execution;
- unsupported requested mandatory limits fail as `scripting.limit.unsupported`;
- values crossing a worker boundary normalize to bounded JSON-compatible null, Boolean, string, number, list, and object shapes;
- expected operational failures contain safe code/message/line/column only;
- raw source, source excerpts, environment, absolute paths, raw stderr, worker exceptions, and arbitrary object graphs never appear in public diagnostics or logs.

Keep existing `ScriptResult<T>` conversion behavior through `Pocok.Conversion`.

Make imports engine-aware:

- retain reference/module/source identities and deterministic graph resolution in core;
- associate import resolution with an engine ID;
- move JavaScript `// #import` parsing and injection into the JavaScript adapter;
- add C# and Python parsers only for syntax actually supported by those adapters;
- do not add persistence, file watching, multi-file workspaces, or cross-language imports.

Remove the implicit parameterless-Jint runner. Migrate every repository consumer in the same slice.

### RR2.2 JavaScript adapter

Create `Pocok.Scripting.JavaScript` and move all Jint dependencies and configuration into it.

Preserve:

- strict mode;
- disabled CLR interop;
- `AllowGetType = false`;
- disabled string compilation;
- explicit scalar/function bindings only;
- statement, recursion, memory, timeout, and cancellation limits.

Add parser/AST validation before engine creation. Reject at least:

- direct and trivial aliased `eval`;
- `Function` construction;
- dynamic import;
- dynamic code construction paths exposed by the configured Jint surface;
- attempts to use host capabilities that are not registered.

Do not rely on a raw substring denylist. Include harmless spelling and comment/string fixtures to control false positives.

The adapter must not log or return source text when Jint reports a syntax or runtime error. Return bounded line/column diagnostics and stable failure codes.

### RR2.3 C# adapter and worker

Create:

```text
src/Scripting.CSharp
src/Scripting.CSharp.Worker
tests/Unit/Scripting.CSharp.Tests
```

Use the centrally pinned Roslyn package in the non-packable worker. The public adapter must not expose Roslyn as a transitive compile dependency and must not compile or execute user-authored C# in the host process.

Default imports are exactly:

```text
System
System.Collections.Generic
System.Linq
System.Threading
System.Threading.Tasks
```

Reject by default:

- request-supplied references;
- `#r` and `#load`;
- unsafe code;
- P/Invoke and native loading;
- reflection and dynamic assembly loading;
- process and diagnostic APIs;
- registry APIs;
- filesystem, network, and environment APIs;
- arbitrary service-provider or host-object access.

Optional imports and references are configured only during adapter registration and intersect an explicit host allowlist. Request text cannot widen capabilities.

Use a versioned, bounded JSON stdin/stdout protocol with separate validate and execute operations. Requirements:

- validation performs Roslyn syntax and semantic analysis against a fixed reference set;
- execution compiles only source that passed validation;
- every execution uses a fresh worker process;
- stdout and stderr are captured asynchronously with byte limits;
- timeout and cancellation kill the entire process tree;
- no shell command line is constructed from source;
- the framework-dependent worker and private Roslyn closure are packed under a deterministic private tools path;
- a SHA-256 manifest covers worker assets;
- a `buildTransitive` target copies private worker assets to the consuming build/publish output without leaking compile assets or Roslyn references;
- resolve the .NET host only from configured `DotNetHostPath`, then `DOTNET_HOST_PATH`;
- missing host, missing assets, protocol mismatch, version mismatch, or hash mismatch produces a truthful unavailable descriptor.

The private worker is not catalogued or independently published.

### RR2.4 Python adapter

Create:

```text
src/Scripting.Python
tests/Unit/Scripting.Python.Tests
```

Support CPython `3.14.x` only.

Resolve the executable from:

1. an explicitly configured adapter option;
2. `POCOK_PYTHON_EXECUTABLE`.

Do not search arbitrary shell aliases or install Python.

Probe:

- `sys.implementation.name == "cpython"`;
- `sys.version_info` is compatible with 3.14.

Invoke Python:

- without a shell;
- with `-I -S`;
- in a private worker directory;
- with a sanitized environment;
- with bounded stdin/stdout/stderr;
- with kill-tree cancellation and timeout.

Validate in a separate worker invocation using Python `ast`. Reject at least:

- `eval`, `exec`, `compile`, and `__import__`;
- dynamic import;
- dunder traversal;
- process and environment access;
- filesystem and network access;
- imports outside an explicit host allowlist.

Default to no imports. Do not expose CLR objects and do not add Python.NET.

Missing or incompatible Python makes the descriptor unavailable; it must not prevent unrelated application startup.

### RR2.5 Package integration and proof

Update:

- `Pocok.slnx`;
- `Directory.Packages.props`;
- all four package READMEs;
- `eng/packages.json`;
- package schema fixtures and architecture tests as required by schema v1;
- API snapshots;
- package ownership/CI mapping;
- `samples/Scripting.Console`;
- installed-package consumers;
- existing import/module tests;
- all current repository consumers.

Place the three adapters in a release tier after `Pocok.Scripting`. Keep `Pocok.Scripting` dependent only on `Pocok.Conversion`; adapters depend on the core.

Focused proof must cover:

- registry selection and duplicate rejection;
- unknown and unavailable engines;
- validator-before-execution;
- source/output bounds;
- unsupported limits;
- cancellation and timeout;
- worker kill and orphan prevention;
- protocol/hash/version errors;
- safe diagnostics without source/path/stderr leakage;
- result normalization;
- configured imports and default denial;
- installed-package execution from an isolated feed;
- public API snapshot review.

Suggested focused commands:

```powershell
dotnet test tests/Unit/Scripting.Tests/Pocok.Scripting.Tests.csproj --configuration Release
dotnet test tests/Unit/Scripting.JavaScript.Tests/Pocok.Scripting.JavaScript.Tests.csproj --configuration Release
dotnet test tests/Unit/Scripting.CSharp.Tests/Pocok.Scripting.CSharp.Tests.csproj --configuration Release

$env:POCOK_PYTHON_EXECUTABLE = (Get-Command python).Source
python --version
dotnet test tests/Unit/Scripting.Python.Tests/Pocok.Scripting.Python.Tests.csproj --configuration Release

dotnet run --project samples/Scripting.Console/Pocok.Scripting.Console.csproj --configuration Release
pwsh -File tools/PackageSmoke/Invoke-PackageSmoke.ps1 -Mode LocalClosure -PackageIds Pocok.Scripting,Pocok.Scripting.JavaScript,Pocok.Scripting.CSharp,Pocok.Scripting.Python
pwsh -File tools/PublicReleaseAudit/Invoke-PublicReleaseAudit.ps1 -PackageIds Pocok.Scripting,Pocok.Scripting.JavaScript,Pocok.Scripting.CSharp,Pocok.Scripting.Python
```

CI uses CPython 3.14 on Linux and Windows. If 3.14 is not installed locally, local Python proof is blocked; the adapter still reports unavailable truthfully and hosted CI remains required.

**Accept when:** four independently packable public packages pass their applicable tests, API/consumer/package gates, the core contains no Jint reference, C# and Python never execute in the host process, and no public Production path enables C# or Python.

## RR3. Add the shared Monaco wrapper and three-engine Scripting Showcase

### Shared wrapper

Pin `BlazorMonaco` at the already selected version in `Directory.Packages.props`. Reference it only from:

```text
showcase/src/Pocok.Showcase.Components
```

Add a shared component such as:

```text
ShowcaseMonacoEditor.razor
ShowcaseMonacoEditor.razor.cs
wwwroot/monacoEditor.js
```

The wrapper:

- serves all assets locally; no CDN request is allowed;
- owns one Monaco model per component instance;
- keys model identity by engine language and reset revision;
- uses the existing `BufferedEditorValue` and `DebouncedValueCommitter<T>` contract;
- commits after a 500 ms quiet period;
- flushes the latest browser value before Run, blur, sample reset, engine switch, fallback switch, and disposal;
- does not replace the model for an ordinary buffered commit;
- maps current light/dark theme tokens;
- supports JavaScript, C#, and Python syntax modes;
- accepts small engine-owned completion catalogs;
- does not claim Roslyn or Python language-service semantics;
- bounds source before sending it to the server;
- disposes models, providers, observers, and JS references;
- shows one safe diagnostic and falls back to `ShowcaseBufferedTextArea` with the latest committed value when initialization, assets, or interop fail;
- preserves labels, keyboard focus, caret behavior, and ordinary textarea operation without JavaScript.

Keep this component internal to the Showcase. Do not create `Pocok.Scripting.UI.Blazor` here.

### Scripting plugin

Update `samples/Showcase/Pocok.Showcase.Scripting`:

- reference all four Scripting packages;
- register adapters and derive the language selector from engine descriptors;
- keep separate circuit-local source and reset revision per sample and engine;
- do not switch execution behavior in the Web host;
- keep JavaScript available publicly;
- show C# and Python as unavailable unless one explicit operator-owned trusted/local setting enables them;
- never expose a browser trust toggle;
- never infer trust from environment name, hostname, route, cookie, query string, or client state.

Every conceptual sample has JavaScript, C#, and Python variants. Include at least:

| Scenario | Requirement |
|---|---|
| Arithmetic | Equivalent deterministic scalar result |
| Structured result | Equivalent object/list JSON-compatible shape |
| String result | Equivalent deterministic string |
| Missing result | Expected-result failure |
| Syntax failure | Safe line/column diagnostic |
| Bounded runaway | Timeout or supported execution bound |
| Validator rejection | Engine-specific prohibited construct rejected before execution |

Use engine-aware capability descriptors to show only limits the selected adapter actually enforces.

Carry engine ID, descriptor, capabilities, validation diagnostics, enforced limits, progress, safe failure code, and result through typed plugin models.

### Proof

Add component and plugin tests for:

- engine descriptor tabs;
- public/Production JavaScript-only availability;
- trusted/local C# and Python availability;
- validator-before-worker;
- per-engine source state;
- language switch and flush;
- selecting the same sample resets all fields;
- Monaco caret and model-reset behavior;
- fallback with latest committed source;
- theme updates;
- safe diagnostics;
- disposal and reconnect;
- English/Hungarian resources;
- equivalent sample results.

Publish to a clean directory and run the existing real-process smoke. Browser-level caret/focus proof may be implemented now if existing test infrastructure supports it; otherwise the deterministic component tests are required here and the full Playwright matrix remains in MVP Closure.

**Accept when:** the local Showcase runs all three engines in explicit trusted mode, public mode executes JavaScript only, all equivalent samples are present, and Monaco failure leaves a functional bounded textarea.

# Phase 2 — Remaining packages and complete local Showcase coverage

## RR4. Close every remaining package alpha gate

### Common minimum gate

For each target package:

1. inspect existing source and evidence first;
2. preserve the current public contract unless a demonstrated defect requires correction;
3. review the public API snapshot member by member;
4. add only missing deterministic behavior and expected-failure tests;
5. cover cancellation, disposal, concurrency, ownership, time, and lifecycle where the package owns those concerns;
6. ensure the README states purpose, minimal usage, lifecycle/ownership, limitations, security/trust boundary, and compatibility;
7. keep or improve its console sample or equivalent real repository consumer;
8. run its installed-package consumer from an isolated local feed;
9. run package smoke and public-content audit;
10. obtain Linux/Windows proof where platform-sensitive or explicitly required;
11. verify package metadata, dependencies, symbols, docs, and absence of path/secret leakage.

A real package defect found during proof is fixed in the same slice with focused regression coverage. Do not expand the package into adjacent product or infrastructure responsibilities.

### BackgroundWork

Required evidence:

- guarded task observation and exception reporting;
- `.Observe(...)` usage;
- one-active-plus-one-pending coalescing;
- debounce quiet-period semantics;
- awaited non-overlapping repetition;
- deterministic `TimeProvider`;
- cancellation before/during work;
- disposal and cancellation races;
- no unobserved task failure.

### Localization

Required evidence:

- JSON and RESX providers;
- deterministic provider composition;
- explicit culture resolution;
- parent and invariant fallback;
- missing-key behavior;
- enum translation;
- file reload and watcher behavior;
- BackgroundWork-backed reload ownership;
- cancellation and disposal;
- no global culture mutation or path leakage.

### Modularity family

Packages:

```text
Pocok.Modularity.Contracts
Pocok.Modularity
Pocok.AppDefaults.Modularity
```

Use the resolved `docs/implementation/modularity-spike.md` design. Do not research or introduce another plugin framework.

Required evidence on Linux and Windows:

- private managed dependency loading;
- shared contract identity;
- required versus optional failures;
- duplicate IDs;
- malformed manifests;
- platform and architecture filtering;
- deterministic order;
- multiple `IEnumerable<TContract>` implementations;
- safe diagnostics without paths or secrets;
- AppDefaults configuration;
- `samples/ModularCommunicator/Stage-Plugin.ps1`;
- real host execution;
- clean Showcase staging through the same manifest/load behavior.

### Signals

Required evidence:

- source identity and capabilities;
- uninitialized, null, stale, bad-quality, and failed distinctions;
- timestamps;
- point-in-time reads;
- writes where supported;
- live subscription lifecycle;
- structured conversion failures;
- deterministic reconnect;
- source failure publication;
- cancellation and disposal.

Keep protocol adapters, persistence, caches, and product-specific integrations out of scope.

### Subscriptions

Required evidence:

- keyed ownership;
- typed filtering and mapping;
- concurrent add/remove;
- callback failure isolation;
- explicit removal;
- disposal;
- no transport, retry, persistence, or network lifecycle expansion.

### Focused commands

Run each package's tests and sample first, followed by:

```powershell
pwsh -File tools/PackageSmoke/Invoke-PackageSmoke.ps1 -Mode LocalClosure -PackageIds <package-ids>
pwsh -File tools/PublicReleaseAudit/Invoke-PublicReleaseAudit.ps1 -PackageIds <package-ids>
```

For Modularity, also run the exact commands maintained in `docs/implementation/modularity-spike.md`.

Only after the exact candidate commit passes its full gate:

- keep `state: Experimental`;
- set `releasable: true`;
- update generated package views and current handoff.

**Accept when:** all seven currently non-releasable libraries pass their applicable API/test/sample/consumer/package/platform gates and are eligible for alpha publication without claiming stable API maturity.

## RR5. Add real Logging and Localization Showcase plugins

Follow `showcase/docs/ADDING_A_SLICE.md`. Add both projects only to `showcase/Pocok.Showcase.Samples.slnx`.

Each plugin has:

- README;
- invariant English and Hungarian resources;
- `pocok.module.json`;
- typed input/output;
- immutable descriptor, guide, samples, and completion metadata where useful;
- fresh sample factories;
- exactly one default sample;
- circuit-local state;
- reset revision advanced on every sample selection;
- focused sample and localization tests;
- no Web project reference.

### AppDefaults.Logging plugin

Create:

```text
samples/Showcase/Pocok.Showcase.AppDefaults.Logging
```

It owns Showcase coverage for:

- `Pocok.AppDefaults`;
- `Pocok.AppDefaults.Logging`;
- `Pocok.AppDefaults.Logging.Serilog`.

Use the host's established AppDefaults logging and `ILogger`. Add no independent global logger.

Emit real bounded synthetic structured events. The display model includes only:

- timestamp;
- level;
- event ID;
- shortened category/namespace;
- message template;
- rendered message;
- allowlisted scalar properties.

Show newest entries first using existing level semantics.

Scenarios cover:

- category minimum levels;
- structured properties;
- safe exception rendering;
- namespace shortening.

Never render stack traces, arbitrary scopes, source, secrets, paths, or non-allowlisted values.

Add bounded, non-global probes that apply current:

- `IApplicationConfigurator`;
- `ConfigureWith(...)`;
- `LoggingDefaultsConfigurator`;
- Serilog configurator.

Each probe uses a fresh builder/provider, verifies real registrations/options, disposes temporary providers, and never mutates the host logger.

### Localization plugin

Create:

```text
samples/Showcase/Pocok.Showcase.Localization
```

It owns Showcase coverage for `Pocok.Localization`.

Use current real APIs, including where still present:

- `FileStringLocalizer`;
- `CompositeStringLocalizer`;
- `ResourceCulture`;
- enum translation.

Stage bounded synthetic JSON and RESX resources with the plugin.

Scenarios cover:

- English and Hungarian;
- explicit parent and invariant fallback;
- missing keys;
- JSON/RESX parity;
- one real reload through current BackgroundWork behavior.

Use a safe temporary directory from the execution context, dispose every localizer/watcher, and never expose its path.

**Accept when:** both plugins stage independently, use real current APIs, have complete bilingual resources, reset deterministically, and pass clean published-host smoke.

## RR6. Add five lightweight configuration and usage recipe builders

Create:

```text
samples/Showcase/Pocok.Showcase.Readiness
samples/Showcase/Pocok.Showcase.BackgroundWork
samples/Showcase/Pocok.Showcase.Modularity
samples/Showcase/Pocok.Showcase.Signals
samples/Showcase/Pocok.Showcase.Subscriptions
```

These are constrained recipe builders, not fake runtime demonstrations.

### Shared interaction contract

Each plugin:

- follows `showcase/docs/ADDING_A_SLICE.md`;
- places immutable presets in the left Samples rail;
- offers typed checkboxes, selects, and bounded numeric options;
- generates exact current C# syntax and, where applicable, `appsettings.json` or `pocok.module.json`;
- regenerates output deterministically after each discrete control change;
- resets every field and advances reset revision when any preset is selected, including the already-selected preset;
- generates identifiers and code only from typed constrained options;
- does not execute generated code;
- does not accept arbitrary source or identifier input;
- localizes prose in English and Hungarian while leaving APIs, IDs, code, and JSON keys invariant;
- does not contact the network, mutate host readiness, start persistent background activity, load arbitrary assemblies, or run user-authored code.

Do not add Roslyn solely to compile generated strings.

Prove syntax accuracy through:

- compiled fixtures/helpers that exercise every selectable API branch;
- renderer unit tests;
- reviewed snapshots for every preset;
- sample reset/edit tests;
- resource completeness tests.

Inspect exact current APIs immediately before implementation. Use their actual method, option, and enum names. Do not invent fluent builders that the package does not expose.

### Readiness recipes

Owns `Pocok.Readiness`.

Cover current patterns for:

- creating or registering a readiness source;
- reporting ready;
- reporting failure;
- stopped/shutdown state;
- cancellation ownership;
- host registration and health integration where current public APIs support it.

A full deterministic ready/fail/cancel/restart runtime timeline is deferred to MVP Closure.

### BackgroundWork recipes

Owns `Pocok.BackgroundWork`.

Cover:

- observing a task;
- coalescing;
- debouncing;
- repeated non-overlapping work;
- cancellation-token ownership;
- `TimeProvider`;
- disposal.

Make ownership and expected exception observation visible in the generated explanation.

### Modularity recipes

Owns:

- `Pocok.Modularity.Contracts`;
- `Pocok.Modularity`;
- `Pocok.AppDefaults.Modularity`.

Cover:

- plugin directory and load options;
- required versus optional modules;
- shared contract assemblies;
- platform/architecture filters;
- `AddPocokModules(...)`;
- `ModularityDefaultsConfigurator`;
- a matching `pocok.module.json`.

Generated registration and manifest must remain mutually consistent.

### Signals recipes

Owns `Pocok.Signals`.

Cover current public patterns for:

- source identity and capabilities;
- point-in-time read;
- write where supported;
- live subscription;
- quality and timestamps;
- handling uninitialized/null/stale/bad-quality/failed values;
- reconnect-related configuration only where the current API actually exposes it.

### Subscriptions recipes

Owns `Pocok.Subscriptions`.

Cover:

- registry ownership;
- key selection;
- typed filtering and mapping;
- registration;
- callback behavior;
- removal;
- disposal.

Do not imply network transport or retry behavior.

**Accept when:** all five plugins stage independently, every preset emits source-accurate syntax, every package has honest lightweight Showcase coverage, and no recipe is presented as executed runtime proof.

## RR7. Reconcile complete local coverage, catalog, workflows, and documentation

### Ten-plugin inventory

The local Showcase must contain:

1. `Pocok.Showcase.Conversion`;
2. `Pocok.Showcase.Scripting`;
3. `Pocok.Showcase.Licensing`;
4. `Pocok.Showcase.AppDefaults.Logging`;
5. `Pocok.Showcase.Localization`;
6. `Pocok.Showcase.Readiness`;
7. `Pocok.Showcase.BackgroundWork`;
8. `Pocok.Showcase.Modularity`;
9. `Pocok.Showcase.Signals`;
10. `Pocok.Showcase.Subscriptions`.

Keep all ten non-packable.

### Required coverage map

Each of the eighteen libraries has exactly one primary local Showcase owner:

| Showcase plugin | Covered library packages |
|---|---|
| Conversion | `Pocok.Conversion` |
| Scripting | `Pocok.Scripting`, `Pocok.Scripting.JavaScript`, `Pocok.Scripting.CSharp`, `Pocok.Scripting.Python` |
| Licensing | `Pocok.Licensing`, `Pocok.AppDefaults.Licensing` |
| AppDefaults.Logging | `Pocok.AppDefaults`, `Pocok.AppDefaults.Logging`, `Pocok.AppDefaults.Logging.Serilog` |
| Localization | `Pocok.Localization` |
| Readiness | `Pocok.Readiness` |
| BackgroundWork | `Pocok.BackgroundWork` |
| Modularity | `Pocok.Modularity.Contracts`, `Pocok.Modularity`, `Pocok.AppDefaults.Modularity` |
| Signals | `Pocok.Signals` |
| Subscriptions | `Pocok.Subscriptions` |

Do not add the later typed `showcase/coverage.json` ledger in this plan. Keep this mapping in documentation and test the generated package/slice catalog sufficiently to detect missing or duplicate owners.

### Catalog

Update schema-v1 `eng/packages.json`:

- add the three adapter packages;
- preserve the neutral Scripting entry;
- use unique tag prefixes and version properties;
- use dedicated consumers;
- add exact internal dependencies;
- keep all non-retired libraries in valid dependency-first tiers;
- set `releasable: true` only for packages whose gates passed;
- preserve `state: Experimental` for alpha APIs unless a separate current source-backed maturity decision exists.

The expected final target count is eighteen. Catalog validation must assert it without hard-coding assumptions into ordinary package logic.

### Package-specific workflow

Expand `.github/workflows/publish.yml` explicit tag triggers to every newly releasable package and adapter.

Keep:

```yaml
concurrency:
  group: pocok-publication
  cancel-in-progress: false
  queue: max
```

Do not migrate to a broad schema-v2 resolver in Release Readiness.

Update CI/tooling fixtures so catalog tag prefixes and workflow coverage cannot drift silently.

### Global workflow

Revalidate the current `.github/workflows/publish-global.yml`; do not create another workflow.

Only make fixes demonstrated by the expanded eighteen-library graph. Preserve:

- `GLOBAL-v*`;
- the shared queue;
- eighteen-target safety limit;
- dependency-first order;
- exact synchronized version properties;
- version/provenance preflight;
- target-scoped smoke and audit;
- candidate/state artifacts;
- draft GitHub Release;
- sequential propagation checks;
- provenance-aware resume.

Do not add Showcase packages, schema v2, typed release tooling, immutable rebuild-free recovery, or multi-job capacity expansion here.

### Documentation

Update implemented behavior and operator guidance in the same implementation change:

- root README package tables;
- all affected package READMEs;
- Scripting security and runtime documentation;
- Showcase README and architecture;
- `showcase/docs/ADDING_A_SLICE.md`;
- `PUBLICATION.md`;
- `docs/global-release.md`;
- `docs/current-handoff.md`;
- API snapshots and generated package views.

Document clearly:

- validators are guardrails;
- C# and Python are trusted/local only;
- public Showcase runs JavaScript only;
- recipe builders generate syntax but are not runtime proof;
- the first global release is library-only;
- the eighteen-package cap is fully consumed.

**Accept when:** catalog, workflows, package metadata, consumers, generated Showcase catalog, docs, and the eighteen-to-ten coverage map agree at one commit.

## RR8. Rehearse and perform the first all-library synchronized prerelease

### RR8.1 Zero-push candidate

Choose an unused prerelease version for rehearsal only.

From a clean checkout and isolated NuGet configuration/cache:

1. generate exact synchronized version properties;
2. restore, build, test, and pack the eighteen-library graph;
3. do not rebuild after candidate audit;
4. run API and package tests;
5. run installed-package consumers;
6. run PackageSmoke in local-closure mode for all eighteen IDs;
7. run publication-shaped smoke where current feed prerequisites permit it;
8. run PublicReleaseAudit for all eighteen IDs;
9. verify nupkg/snupkg names, metadata, dependencies, repository commit, symbols, and content;
10. publish the local Showcase with all ten plugins;
11. run real-process Showcase smoke;
12. run Linux and Windows CI;
13. run trusted/local JavaScript, C#, and CPython 3.14 Scripting proof;
14. rerun global resolver/preflight in zero-push form;
15. preserve exact failures and external blocks.

The candidate must not contact NuGet with write credentials and must not create a Git tag or GitHub Release.

Publication-shaped restore against nuget.org cannot prove unpublished dependent candidates. Use the existing isolated local-feed mode for complete closure and reserve public-feed validation for versions that are already visible.

### RR8.2 Approval handoff

Present:

- exact commit;
- proposed `GLOBAL-v<major.minor.patch-prerelease>` tag;
- eighteen target IDs;
- dependency-first order;
- package and symbol artifact inventory;
- zero-push command summary;
- Linux/Windows CI results;
- Showcase ten-plugin smoke result;
- Python runtime evidence;
- current nuget.org preflight;
- known external prerequisites;
- safe recovery behavior.

Do not create or push the tag until explicit approval.

### RR8.3 Approved publication

After approval:

```powershell
git tag -a GLOBAL-v<version> -m "Release synchronized Pocok packages <version>"
git push origin GLOBAL-v<version>
```

Observe the existing queued workflow.

Do not move or recreate the pushed tag.

If a package already exists at the exact version:

- continue only when repository URL and commit provenance match the global tag according to current workflow rules;
- stop on unknown or conflicting provenance.

If the workflow stops:

- record published, verified, failed, blocked, and pending package IDs;
- preserve draft-release and Actions-artifact references;
- rerun the same immutable tag only through the workflow's provenance-safe resume;
- never overwrite, delete, or pretend NuGet rollback exists.

### Completion states

Use these exact distinctions:

- **Implementation ready to release:** RR1-RR7 and zero-push proof pass; no tag was pushed.
- **Approved tag pushed:** explicit approval occurred and the immutable tag was pushed.
- **All packages publicly verified:** all eighteen exact packages and the GitHub Release are public and provenance-checked.
- **Partial immutable publication:** some exact packages are public; the handoff records one safe same-tag resume path.

**Release Readiness is complete when:** all package and Showcase gates pass and either all eighteen packages are publicly verified, or an exact partial-publication state with a safe resume path is recorded. MVP Closure must not begin live bundle publication until this state is resolved.

## External prerequisites and deterministic fallbacks

| Prerequisite | Required behavior | Fallback and completion effect |
|---|---|---|
| .NET 10 SDK and PowerShell 7 | Use repository-pinned versions and current lock files | Record environment block; no package eligibility change |
| CPython 3.14 | Explicit configured executable passes probe on Linux and Windows | Python descriptor is unavailable; Scripting Python alpha gate and global release remain open |
| NuGet read access | Version/provenance preflight and clean restores | Local isolated feed may prove closure; public release remains blocked |
| NuGet Trusted Publishing | Existing repository/environment mapping and username variable | Stop before first push; retain zero-push evidence |
| GitHub Actions | Shared queue and hosted Linux/Windows execution | Local evidence remains partial; cross-platform/global release gate stays open |
| GitHub Releases | Draft/final release operations in current workflow | Stop publication before an unsafe substitute; record external block |
| Monaco local assets | Pinned package assets load without CDN | Buffered textarea remains functional; Monaco acceptance stays open until fixed |
| Docker/Render | Not required by this plan | No effect on Release Readiness; owned by MVP Closure |

## Final acceptance checklist

- [ ] Eighteen non-retired library packages exist in the validated schema-v1 catalog.
- [ ] Every package has current API, test, sample/consumer, package-smoke, audit, and applicable platform evidence.
- [ ] Every eligible alpha package is `releasable: true`; experimental API maturity is not misrepresented.
- [ ] Scripting core is engine-neutral and has no Jint dependency.
- [ ] JavaScript, C#, and Python adapters are independently packable.
- [ ] C# and Python execution is child-process based, bounded, and trusted/local only.
- [ ] Every engine validates before execution and produces safe diagnostics.
- [ ] The Scripting Showcase has equivalent three-language samples and the shared Monaco fallback.
- [ ] Ten local Showcase plugins cover all eighteen libraries according to the fixed map.
- [ ] Recipe builders use current typed APIs and do not claim runtime execution.
- [ ] Package-specific triggers cover every releasable package.
- [ ] Both publication workflows retain the shared non-cancelling `queue: max` concurrency group.
- [ ] The existing global workflow passes an eighteen-library zero-push rehearsal.
- [ ] An explicit approval gate precedes the immutable global tag.
- [ ] Publication is publicly verified or recorded as an exact resumable partial state.
- [ ] `docs/current-handoff.md` distinguishes static, local, CI, package, public-feed, and publication evidence.
