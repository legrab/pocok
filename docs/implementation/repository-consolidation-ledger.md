# Repository consolidation implementation ledger

> **Historical ledger:** Checked items and status statements describe the consolidation phase. Current release eligibility
> and package shape are authoritative in `eng/packages.json`, `PUBLICATION.md`, and `docs/current-handoff.md`.

> Checked implementation items mean the source change exists. Executable validation is recorded separately and never inferred from a checked design task.

## Frozen package graph

```text
Pocok.Conversion
├── Pocok.Scripting
└── Pocok.Signals
Pocok.Readiness
Pocok.Localization

Pocok.AppDefaults
├── Pocok.AppDefaults.Logging
├── Pocok.AppDefaults.Logging.Serilog
└── Pocok.AppDefaults.Modularity

Pocok.Modularity.Contracts
└── Pocok.Modularity
    └── Pocok.AppDefaults.Modularity
```

Arrows point from a dependency to its consumers. `Pocok.AppDefaults.Modularity` intentionally joins the AppDefaults and Modularity branches. Serilog defaults and provider-neutral logging defaults are separate policies.

## Accepted decisions

- Preserve organized Git history in delivered repository snapshots.
- Keep Modularity packages non-releasable until the real plugin fixture matrix passes on Linux and Windows.
- Retire the already-published `Pocok.Primitives` package without a forwarding package.
- Keep logging defaults conservative, configuration-driven, additive by default, and overridable by the application.
- Publish capability and maintainer-default packages through nuget.org.
- Keep tiny repository reuse package-local or as explicit internal linked source, never as a hidden runtime assembly.
- Reject duplicate application of Pocok concern configurators rather than silently discarding later options.
- Expose resolved configurator policy through standard `IOptions<T>` startup snapshots.
- Keep simple-console registration opt-in so a standard host does not receive a duplicate console provider.

## Applied consolidation

- [x] Retire Primitives and remove it from active package dependencies.
- [x] Merge Conversion abstractions into Conversion.
- [x] Rename Hosting to Readiness and preserve package-owned failures.
- [x] Add bounded Conversion work, explicit strategies, tests, an explicit trim opt-in smoke fixture, and benchmark project.
- [x] Mark general Conversion APIs with `RequiresUnreferencedCode`; the trim fixture validates only a known array path.
- [x] Add restartable Readiness lifecycle and concurrency-oriented tests.
- [x] Add AppDefaults composition, provider-neutral logging, and Serilog policy packages.
- [x] Add experimental Modularity contracts, loader, diagnostics, fixtures, and defaults.
- [x] Keep Modularity publication-gated.
- [x] Extract the neutral, bounded Scripting execution and import vertical slice as experimental alpha.
- [x] Extract the neutral Signals identity, quality-aware sample, and source-operation contract slice as experimental alpha.
- [x] Extract the Signals shared runtime with replay, bounded buffering, reconnect, staleness, point-in-time reads, typed conversion, writes, and lease cleanup as experimental alpha.
- [x] Extract the neutral Localization compositor, deterministic provider precedence, missing-key fallback, enum translation fallback, and explicit resource culture resolution as experimental alpha.
- [x] Extract the neutral keyed subscription registry with typed filtering and mapping as experimental alpha.
- [x] Add package catalog, API snapshots, samples, migration guides, and release-version propagation.
- [x] Isolate the five initial packages in `Pocok.Core.slnx`.

## V2 stabilization work

### Pending WIP finalized

- [x] Remove redundant machine-layout-dependent Modularity unit fixtures.
- [x] Restore the documented required-versus-optional duplicate module failure policy.
- [x] Keep optional duplicate module IDs diagnostic unless strict optional failure mode is enabled.
- [x] Remove stale generated build output from version control.

### Wave C: package semantics

- [x] Add one catalog-driven, dependency-first package-closure resolver.
- [x] Make local smoke packing candidate-scoped instead of packing the whole solution.
- [x] Build local-closure feeds from only the exact transitive Pocok closure.
- [x] Add package source mapping so missing Pocok dependencies cannot fall back to nuget.org in local-closure mode.
- [x] Keep only the candidate local in publication mode and map exact internal dependency IDs to nuget.org.
- [x] Pack only the release candidate closure in the publication workflow.
- [x] Make the public release audit candidate-scoped and reject unrelated or stale artifacts.
- [x] Verify dependency IDs and local dependency versions against the package catalog.
- [x] Add core sample execution and an explicit trimmed-array Conversion publish/run smoke check to CI.

### Wave D: AppDefaults semantics

- [x] Reject duplicate Logging, Serilog, and Modularity configurator application explicitly.
- [x] Bind configuration before code delegates.
- [x] Register resolved startup policy through `IOptions<T>` rather than raw option objects.
- [x] Preserve existing logging providers unless clearing is explicitly requested.
- [x] Keep application registrations after defaults authoritative under standard builder ordering.
- [x] Make simple-console registration opt-in to avoid duplicate providers in standard hosts.
- [x] Document startup-snapshot, override, provider, and duplicate-call behavior.

## Validation evidence

Windows acceptance evidence on 2026-07-16, using .NET SDK 10.0.102 and PowerShell 7.3.6, passed formatting, solution build, 236 tests, core sample execution, package catalog validation, local-closure smoke, and public release audit on the current working tree. The Operations worker sample also passed both `dotnet run` and direct built-executable launch after its Windows provider and readiness-shutdown fixes.

The Scripting alpha slice was subsequently restored and validated in isolation: the package builds with zero warnings, ten focused tests pass, its public API snapshot passes, and the console sample returns script-result=42. Source-size and engine-memory bounds are explicit, and its package smoke consumer is wired into the catalog-driven local closure and passes from the installed package. It remains experimental and non-releasable while the extraction boundary is evaluated.

The Signals slice is independently packageable, has twenty-two focused contract and runtime tests and a public API snapshot, and contains no protocol adapter, persistence, cache backend, UI, logging, serializer, or service-provider dependency. Its runtime shares source subscriptions by address, replays the latest sample, bounds slow consumers, handles reconnect/staleness transitions, exposes point-in-time reads, typed conversion, write evidence, and structured typed-read conversion failures.

The Localization alpha slice is independently packageable, has twelve focused composition, resource-culture, and enum-localization tests and a public API snapshot, and contains only deterministic composition over caller-owned `IStringLocalizer` providers, enum translation fallback, and explicit culture resolution from resource-file names. Missing entries do not shadow later found resources during enumeration. Database, filesystem, resource-assembly discovery, caching, logging, global culture mutation, and application-specific registration remain outside the package.

The Subscriptions alpha slice is independently packageable, has five focused tests and a public API snapshot, and contains only a thread-safe keyed listener registry with typed filtering, exact-type default mapping, explicit custom mapping, synchronous delivery, and deterministic disposal. Transport connections, retry timers, logging, dependency injection, persistence, and network lifecycle remain outside the package.

Packing succeeded and produced packages and symbols. It emitted `MINVER1001` warnings locally because the sandbox Git identity cannot treat the parent repository as a valid working directory; this environment-specific warning must not be confused with a build error.

## Required executable validation

```pwsh
dotnet --info
$PSVersionTable

dotnet restore Pocok.slnx
dotnet format Pocok.slnx --verify-no-changes --no-restore
dotnet build Pocok.slnx --configuration Release --no-restore
dotnet test Pocok.slnx --configuration Release --no-build
dotnet pack Pocok.slnx --configuration Release --no-build --output artifacts/packages

./tools/PackageCatalog/Test-PackageCatalog.ps1
./tools/PackageSmoke/Invoke-PackageSmoke.ps1 -NoPack -Mode LocalClosure
./tools/PublicReleaseAudit/Invoke-PublicReleaseAudit.ps1
```

For each release candidate, generate release-version props and execute candidate-scoped local-closure, publication, and audit modes. Publication mode requires all internal dependencies to already exist on nuget.org.

## Initial release sequence

1. `Pocok.Conversion`
2. `Pocok.Readiness`
3. `Pocok.AppDefaults`
4. `Pocok.AppDefaults.Logging`
5. `Pocok.AppDefaults.Logging.Serilog`

The first three are independent. Steps 4 and 5 require step 3 and may then be released in either order.

## Remaining gate

- [ ] Run the complete executable acceptance matrix on Linux and Windows.
- [x] Fix only factual failures found by that run.
- [ ] Verify real debugger Source Link behavior from an installed candidate package.
- [ ] Complete Wave E before changing any Modularity package to releasable.
