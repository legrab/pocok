# Pocok

[![NuGet](https://img.shields.io/nuget/v/Pocok.Conversion?label=NuGet&logo=nuget)](https://www.nuget.org/packages?q=Pocok)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](LICENSE)

Pocok is a focused .NET package portfolio built from recurring application needs. Its packages provide small runtime
capabilities and transparent application defaults without introducing a private framework.

> ## Explore Pocok in the Showcase
>
> Browse the package portfolio, edit runnable examples, and execute bounded Conversion, Scripting, and Licensing
> sandboxes in the deployed application.
>
> **[Open the deployed Pocok Showcase](https://pocok-showcase.onrender.com/)**

## Packages

| Area | Packages |
|---|---|
| Runtime capabilities | `Pocok.Conversion`, `Pocok.Readiness`, `Pocok.Scripting`, `Pocok.Licensing` |
| Application defaults | `Pocok.AppDefaults`, `Pocok.AppDefaults.Logging`, `Pocok.AppDefaults.Logging.Serilog`, `Pocok.AppDefaults.Licensing` |
| Experimental capabilities | Background work, localization, modularity, signals, and subscriptions |

Scripting and Licensing use package-specific tags for NuGet publication. Licensing issuer and checker executables remain
separate from NuGet and are published as self-contained GitHub Release assets from their own tags.

## Quick start

Configure conservative hosting defaults explicitly:

```csharp
using Microsoft.Extensions.Hosting;
using Pocok.AppDefaults;
using Pocok.AppDefaults.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.ConfigureWith(new LoggingDefaultsConfigurator());
```

Use capability packages directly, without depending on AppDefaults:

```csharp
using Pocok.Conversion;

var result = ValueConverter.Default.Convert<int>("42");
```

See the [licensing guide](docs/licensing.md) for key generation, license issuance, runtime validation, host integration,
and security boundaries. More executable examples live under [`samples`](samples), and the deployable Showcase is
documented in [showcase/README.md](showcase/README.md).

<details>
<summary><strong>Detailed package status</strong></summary>

| Package | State | Purpose |
|---|---|---|
| `Pocok.Conversion` | Release-enabled | Strict, serializer-free, policy-driven runtime value conversion |
| `Pocok.Readiness` | Release-enabled | Observable and restartable readiness lifecycle coordination |
| `Pocok.AppDefaults` | Release-enabled | Explicit ordered application configurators |
| `Pocok.AppDefaults.Logging` | Release-enabled | Conservative provider-neutral logging defaults |
| `Pocok.AppDefaults.Logging.Serilog` | Release-enabled | Configuration-driven Serilog hosting defaults |
| `Pocok.Scripting` | Release-enabled alpha | Bounded JavaScript execution with explicit bindings and deterministic imports |
| `Pocok.Licensing` | Release-enabled alpha | Signed offline licenses with module, time, runtime, machine, and pre-shared-key constraints |
| `Pocok.AppDefaults.Licensing` | Release-enabled alpha | Startup and periodic host enforcement for Pocok licensing |
| `Pocok.Modularity.Contracts` | Experimental | Startup module contracts shared by hosts and plugins |
| `Pocok.Modularity` | Experimental | Trusted startup-time plugin discovery and DI registration |
| `Pocok.AppDefaults.Modularity` | Experimental | Conventional host policy for modularity |
| `Pocok.BackgroundWork` | Experimental alpha | Guarded observation, coalesced and debounced work, and non-overlapping repetition |
| `Pocok.Signals` | Experimental alpha | Quality-aware live values and shared subscription runtime |
| `Pocok.Localization` | Experimental alpha | Deterministic localization composition and external resources |
| `Pocok.Subscriptions` | Experimental alpha | Thread-safe keyed subscriptions with typed filtering and mapping |

`Pocok.Primitives` is retired without a forwarding package. Its useful behavior moved to Conversion and Readiness; see
the [migration guide](docs/migrations/primitives-retirement.md).

</details>

<details>
<summary><strong>Build and verification</strong></summary>

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

Publication is tag-driven and validates an exact dependency closure before pushing one immutable candidate. See
[PUBLICATION.md](PUBLICATION.md) and the [current handoff](docs/current-handoff.md).

</details>

<details>
<summary><strong>Architecture and repository records</strong></summary>

Capability packages own runtime behavior. Maintainer-default packages configure standard .NET or explicitly selected
third-party infrastructure. There is no public `Common`, `Utils`, `Foundation`, or generic `Primitives` package.

- [Stable repository rules](AGENTS.md)
- [Agentic workflow](docs/agentic-workflow.md)
- [Learning the repository and its harness](docs/agentic-learning.md)
- [Current handoff](docs/current-handoff.md)
- [Consolidation plan](docs/plans/repository-consolidation.md)
- [Implementation report](docs/implementation/repository-consolidation-report.md)
- [Architectural decisions](docs/decisions)

</details>

## License and stewardship

Pocok is licensed under [Apache-2.0](LICENSE). Security reports, contributions, and project stewardship are described in
[SECURITY.md](SECURITY.md), [CONTRIBUTING.md](CONTRIBUTING.md), and [STEWARDSHIP.md](STEWARDSHIP.md).
