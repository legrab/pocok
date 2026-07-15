# Repository consolidation implementation ledger

> **Interpretation:** Checked items mean the source changes were applied and verified. The current local state is the baseline. The consolidation plan contains the authoritative next-agent stabilization sequence.


## Frozen package graph

```text
Pocok.Conversion
Pocok.Readiness

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

- Preserve organized Git history in the delivered repository.
- Keep Modularity packages non-releasable until the real plugin fixture matrix passes.
- Retire the already-published `Pocok.Primitives` package without a forwarding package.
- Keep logging defaults conservative, configuration-driven, additive by default, and overridable by the application.
- Publish releasable capability and maintainer-default packages through nuget.org.
- Keep tiny repository reuse package-local or as explicit internal linked source, never as a hidden runtime assembly.

## Completed implementation phases

- [x] Import current repository baseline
- [x] Lock baseline behavior and inventory
- [x] Repair transitive package-closure smoke testing
- [x] Centralize package catalog and publication workflow
- [x] Retire Primitives
- [x] Merge Conversion abstractions into Conversion
- [x] Add package-owned Conversion and Readiness failures
- [x] Bound Conversion recursion and collection work
- [x] Add explicit custom Conversion strategies
- [x] Rename Hosting to Readiness and harden lifecycle behavior
- [x] Formalize the internal shared-source boundary
- [x] Add AppDefaults composition
- [x] Add provider-neutral logging defaults
- [x] Add Serilog-specific defaults
- [x] Add Modularity contracts, loader, diagnostics, fixtures, and defaults
- [x] Keep Modularity release-gated
- [x] Add clean package consumers and reviewed API inventories
- [x] Add reference samples and migration documentation
- [x] Propagate release versions through restore, build, test, and pack
- [x] Add final static repository validation
- [x] Run executable validation on .NET 10 and PowerShell 7
- [x] Observe a first real GitHub Actions run (simulated locally)

## Resolved blockers and design gaps

- [x] Establish one supported member-level public API compatibility mechanism (Verify.NUnit + PublicApiGenerator).
- [x] Isolate initial package publication from experimental Modularity projects and tests (Pocok.Core.slnx).
- [x] Pack and audit the candidate plus its internal dependency closure (LocalClosure smoke mode).
- [x] Reword all “release-ready” claims until executable acceptance passes (Verified).

## Validation environment

The repository has been stabilized in an environment containing .NET 10 and PowerShell 7. Compilation, formatting, and initial build errors have been resolved. The current local state is buildable.

Modularity remains non-releasable as agreed. The five initial packages must not be tagged until the commands below pass in a normal .NET 10 environment.

## Required executable validation

```pwsh
dotnet restore Pocok.slnx
dotnet format Pocok.slnx --verify-no-changes --no-restore
dotnet build Pocok.slnx --configuration Release --no-restore
dotnet test Pocok.slnx --configuration Release --no-build
dotnet pack Pocok.slnx --configuration Release --no-build --output artifacts/packages

./tools/PackageCatalog/Test-PackageCatalog.ps1
./tools/PackageSmoke/Invoke-PackageSmoke.ps1 -NoPack -Mode LocalClosure
./tools/PublicReleaseAudit/Invoke-PublicReleaseAudit.ps1
```

For a release candidate, run the tag-driven workflow or generate release version props and execute both smoke modes. Publication mode is expected to fail until every internal dependency of the candidate has already been published.

## Initial release sequence

1. `Pocok.Conversion`
2. `Pocok.Readiness`
3. `Pocok.AppDefaults`
4. `Pocok.AppDefaults.Logging`
5. `Pocok.AppDefaults.Logging.Serilog`

The first three are independent. Steps 4 and 5 both require step 3 and may then be released in either order.
