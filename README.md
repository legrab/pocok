# Pocok

> **Current status:** Consolidated repository. Initial formatting and build fixes applied. Ready for stabilization and verification of tests. See the [consolidation plan](docs/plans/repository-consolidation.md).

Pocok is a deliberately small .NET package portfolio extracted from repeated application needs. It contains focused runtime capabilities and transparent application-default configurators. The repository is also maintained as a reference for package boundaries, compatibility, testing, plugin isolation, and release engineering.

## Current package state

| Package | Family | State | Purpose |
|---|---|---|---|
| `Pocok.Conversion` | Capability | Intended initial release | Strict, serializer-free, policy-driven runtime value conversion |
| `Pocok.Readiness` | Capability | Intended initial release | Observable and restartable readiness lifecycle coordination |
| `Pocok.AppDefaults` | Maintainer defaults | Intended initial release | Explicit ordered application configurators |
| `Pocok.AppDefaults.Logging` | Maintainer defaults | Intended initial release | Conservative provider-neutral logging defaults |
| `Pocok.AppDefaults.Logging.Serilog` | Maintainer defaults | Intended initial release | Configuration-driven Serilog hosting defaults |
| `Pocok.Modularity.Contracts` | Capability | Experimental | Stable startup module contracts shared by host and plugin |
| `Pocok.Modularity` | Capability | Experimental | Trusted startup-time plugin discovery and DI registration |
| `Pocok.AppDefaults.Modularity` | Maintainer defaults | Experimental | Conventional host policy for `Pocok.Modularity` |

Experimental packages remain packable and tested but have no publication tag trigger. Their catalog entries must be changed explicitly after the documented release gate passes on Linux and Windows.

## Package identity

Capability packages own runtime behavior. Maintainer-default packages configure standard .NET or explicitly selected third-party infrastructure into a repeatable application baseline. They do not replace dependency injection, configuration, logging, hosting, or plugin loading with a private framework.

There is no public `Common`, `Utils`, `Foundation`, or generic `Primitives` package. Small internal helpers remain package-local or are linked as explicitly selected internal source only after demonstrated repository-wide reuse.

`Pocok.Primitives` was published during the initial extraction and is intentionally retired without a forwarding package. Its useful behavior is now owned by Conversion and Readiness. See [the migration guide](docs/migrations/primitives-retirement.md).

## Quick start

```csharp
using Microsoft.Extensions.Hosting;
using Pocok.AppDefaults;
using Pocok.AppDefaults.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.ConfigureWith(new LoggingDefaultsConfigurator());
```

Capability packages do not depend on AppDefaults:

```csharp
using Pocok.Conversion;

var result = ValueConverter.Default.Convert<int>("42");
```

See the projects under [`samples`](samples) for Conversion, Readiness, AppDefaults, trimming, and independently deployed modules.

## Build and verify

The repository targets .NET 10 and uses PowerShell 7 for release tooling.

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

Publication uses package-specific tags, generated release-version overrides, complete local-closure restoration, and a candidate-plus-nuget.org rehearsal. See [PUBLICATION.md](PUBLICATION.md).

## Design and implementation record

- [Repository evaluation and consolidation plan](docs/plans/repository-consolidation.md)
- [Implementation ledger](docs/implementation/repository-consolidation-ledger.md)
- [Implementation report](docs/implementation/repository-consolidation-report.md)
- [Architectural decisions](docs/decisions)

## License and stewardship

Pocok is licensed under Apache-2.0. See [CONTRIBUTING.md](CONTRIBUTING.md), [STEWARDSHIP.md](STEWARDSHIP.md), and [SECURITY.md](SECURITY.md).
