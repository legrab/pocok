# Pocok

Pocok is a deliberately small .NET package portfolio extracted from repeated application needs. It contains focused runtime capabilities and transparent application-default configurators. It is also maintained as a reference repository for package design, compatibility, testing, and release engineering.

## Package families

### Capability packages

- **Pocok.Conversion**: strict, serializer-free, policy-driven runtime value conversion.
- **Pocok.Readiness**: observable and restartable readiness lifecycle coordination.
- **Pocok.Modularity.Contracts**: stable startup module contracts.
- **Pocok.Modularity**: trusted startup-time plugin discovery and dependency registration.

### Maintainer defaults

- **Pocok.AppDefaults**: explicit ordered application configurators.
- **Pocok.AppDefaults.Logging**: conservative provider-neutral logging defaults.
- **Pocok.AppDefaults.Logging.Serilog**: focused Serilog defaults.
- **Pocok.AppDefaults.Modularity**: opinionated host defaults for Pocok.Modularity.

Maintainer-default packages configure standard or selected third-party infrastructure. They do not replace dependency injection, configuration, logging, hosting, or plugin loading with a private framework.

## Package policy

A package is kept only when it represents a stable, reusable capability or repeated cross-application policy. Small internal helpers remain package-local or are linked as explicitly selected internal source. There is no public `Common`, `Utils`, `Foundation`, or generic `Primitives` package.

`Pocok.Primitives` was published during the repository's initial extraction and is intentionally retired. Its useful behavior is owned by the packages that need it. See [the migration guide](docs/migrations/primitives-retirement.md).

## Build

```pwsh
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
dotnet pack --configuration Release --no-build --output artifacts/packages
./tools/PackageSmoke/Invoke-PackageSmoke.ps1 -NoPack -Mode LocalClosure
./tools/PublicReleaseAudit/Invoke-PublicReleaseAudit.ps1
```

The repository targets .NET 10. Publication uses package-specific tags and a catalog-driven workflow. See [PUBLICATION.md](PUBLICATION.md).

## Design record

The consolidation rationale and implementation plan are retained in [docs/plans/repository-consolidation.md](docs/plans/repository-consolidation.md). Architectural decisions live under [docs/decisions](docs/decisions).

## License and stewardship

Pocok is licensed under Apache-2.0. See [CONTRIBUTING.md](CONTRIBUTING.md), [STEWARDSHIP.md](STEWARDSHIP.md), and [SECURITY.md](SECURITY.md).
