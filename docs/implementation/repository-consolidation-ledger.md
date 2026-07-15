# Repository consolidation implementation ledger

## Frozen target package graph

```text
Pocok.Conversion
Pocok.Readiness
Pocok.AppDefaults
Pocok.AppDefaults.Logging -> Pocok.AppDefaults
Pocok.AppDefaults.Logging.Serilog -> Pocok.AppDefaults.Logging
Pocok.Modularity.Contracts
Pocok.Modularity -> Pocok.Modularity.Contracts
Pocok.AppDefaults.Modularity -> Pocok.AppDefaults + Pocok.Modularity
```

## Accepted decisions

- Preserve the organized Git history in the delivered repository.
- Keep Modularity packages non-releasable until their real plugin fixture matrix passes.
- Retire the already-published `Pocok.Primitives` package without a forwarding package.
- Keep logging defaults conservative, additive by default, configuration-driven, and overridable by the application.

## Validation environment

The implementation environment did not contain .NET or PowerShell and could not retrieve executable toolchain archives. Static repository checks are run here. The final executable validation commands are recorded below and must be run before publication.

## Phase checklist

- [x] Import current repository baseline
- [ ] Repair release staging and package catalog
- [ ] Retire Primitives
- [ ] Consolidate Conversion
- [ ] Rename and stabilize Readiness
- [ ] Add AppDefaults and logging
- [ ] Add Modularity and fixtures
- [ ] Complete package documentation and release gates
- [ ] Run executable validation on .NET 10 and PowerShell 7

## Required final validation

```pwsh
dotnet restore
dotnet format --verify-no-changes --no-restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
dotnet pack --configuration Release --no-build --output artifacts/packages
./tools/PackageCatalog/Test-PackageCatalog.ps1
./tools/PackageSmoke/Invoke-PackageSmoke.ps1 -Mode LocalClosure
./tools/PublicReleaseAudit/Invoke-PublicReleaseAudit.ps1
```

Run publication-mode smoke tests in package dependency order after the required internal dependencies exist on nuget.org.
