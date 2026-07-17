# Pocok

> **Current status:** Consolidated implementation candidate. The baseline passed its .NET 10 and PowerShell 7 acceptance run with formatting, a Release build, 236 tests, samples, package catalog validation, local-closure smoke, and public release audit. The licensing additions are experimental and have static review only until the documented local and CI acceptance run succeeds. See the [current handoff](docs/current-handoff.md), [licensing guide](docs/licensing.md), and [consolidation plan](docs/plans/repository-consolidation.md).

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
| `Pocok.BackgroundWork` | Capability | Experimental, alpha | Guarded task observation, coalesced and debounced work, and non-overlapping repetition |
| `Pocok.Scripting` | Capability | Experimental, alpha | Bounded JavaScript execution with explicit bindings and deterministic imports |
| `Pocok.Signals` | Capability | Experimental, alpha | Quality-aware live-value contracts and shared subscription runtime |
| `Pocok.Localization` | Capability | Experimental, alpha | Deterministic composition plus external JSON and string-only RESX resources |
| `Pocok.Subscriptions` | Capability | Experimental, alpha | Thread-safe keyed subscriptions with typed filtering and mapping |
| `Pocok.Licensing` | Capability | Experimental, alpha | Signed offline licenses with module, time, runtime, machine and pre-shared-key constraints |
| `Pocok.AppDefaults.Licensing` | Maintainer defaults | Experimental, alpha | Startup and periodic host enforcement for Pocok licensing |

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

See [the licensing guide](docs/licensing.md) for key generation, checking, host integration, threat model and hardening guidance.

See the projects under [`samples`](samples) for Conversion, Readiness, AppDefaults, Licensing host enforcement, owned BackgroundWork coordination, bounded Scripting, Signals contracts and runtime, deterministic Localization composition, external JSON and RESX resources, enum translation and resource culture resolution, keyed Subscriptions, an explicit trimmed-array smoke test, and independently deployed modules.

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

## Agentic collaboration

- [Stable repository rules](AGENTS.md)
- [Agentic workflow](docs/agentic-workflow.md)
- [Learning the repository and its harness](docs/agentic-learning.md)
- [Current handoff](docs/current-handoff.md)
- [Prompt compatibility entry points](prompts/README.md)
- [Session record policy](sessions/README.md)

## Design and implementation record

- [Repository evaluation and consolidation plan](docs/plans/repository-consolidation.md)
- [Implementation ledger](docs/implementation/repository-consolidation-ledger.md)
- [Implementation report](docs/implementation/repository-consolidation-report.md)
- [Architectural decisions](docs/decisions)
- [Agentic workflow documentation change log](docs/agentic-workflow-change-log.md)

## License and stewardship

Pocok is licensed under Apache-2.0. See [CONTRIBUTING.md](CONTRIBUTING.md), [STEWARDSHIP.md](STEWARDSHIP.md), and [SECURITY.md](SECURITY.md).
