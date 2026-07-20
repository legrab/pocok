# Third-Party Notices

Pocok uses third-party NuGet packages for runtime integrations, private workers, the Showcase, build tooling, and tests.
`Directory.Packages.props` is authoritative for exact centrally pinned versions; package project files determine whether a
dependency is distributed to consumers or used only by repository tooling.

This inventory records direct dependencies. Transitive dependencies remain governed by their own package metadata and
licenses and are reviewed by package-content and dependency audits before release.

## Runtime and distributed package dependencies

| Component | Version | Purpose and distribution | License |
|---|---:|---|---|
| [Acornima](https://www.nuget.org/packages/Acornima/1.6.2) | 1.6.2 | JavaScript parsing and validation; dependency of `Pocok.Scripting.JavaScript` | BSD-3-Clause |
| [Jint](https://www.nuget.org/packages/Jint/4.13.0) | 4.13.0 | Bounded JavaScript engine; dependency of `Pocok.Scripting.JavaScript` | BSD-2-Clause |
| Microsoft.Extensions packages | 10.0.10 | Hosting, configuration, DI, logging, localization, and options abstractions used by public packages | MIT |
| [Serilog](https://www.nuget.org/packages/Serilog/4.4.0) | 4.4.0 | Structured logging integration in `Pocok.AppDefaults.Logging.Serilog` | Apache-2.0 |
| [Serilog.Extensions.Hosting](https://www.nuget.org/packages/Serilog.Extensions.Hosting/10.0.0) | 10.0.0 | Serilog host integration | Apache-2.0 |
| [Serilog.Settings.Configuration](https://www.nuget.org/packages/Serilog.Settings.Configuration/10.0.1) | 10.0.1 | Configuration-driven Serilog setup | Apache-2.0 |

## Private worker and Showcase dependencies

| Component | Version | Purpose and distribution | License |
|---|---:|---|---|
| [Microsoft.CodeAnalysis.CSharp.Scripting](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp.Scripting/5.6.0) | 5.6.0 | Roslyn scripting inside the non-packable C# worker; worker assets are integrity-checked and distributed through `buildTransitive` | MIT |
| [BlazorMonaco](https://www.nuget.org/packages/BlazorMonaco/3.5.0) | 3.5.0 | Monaco editor bridge and static web assets in the non-packable Showcase | MIT |

## Build and test dependencies

| Component | Version | Purpose | License |
|---|---:|---|---|
| [MinVer](https://www.nuget.org/packages/MinVer/7.0.0) | 7.0.0 | Git-tag-derived development and package versions | Apache-2.0 |
| [Microsoft.NET.Test.Sdk](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.8.1) | 18.8.1 | Test host and discovery | MIT |
| [NUnit](https://www.nuget.org/packages/NUnit/4.6.1) | 4.6.1 | Test framework | MIT |
| [NUnit3TestAdapter](https://www.nuget.org/packages/NUnit3TestAdapter/6.2.0) | 6.2.0 | NUnit test adapter | MIT |
| [PublicApiGenerator](https://www.nuget.org/packages/PublicApiGenerator/11.5.4) | 11.5.4 | Public API snapshot generation | MIT |
| [Shouldly](https://www.nuget.org/packages/Shouldly/4.3.0) | 4.3.0 | Test assertions | BSD-3-Clause |
| [Verify.NUnit](https://www.nuget.org/packages/Verify.NUnit/31.25.0) | 31.25.0 | Snapshot verification | MIT |
| [coverlet.collector](https://www.nuget.org/packages/coverlet.collector/10.0.1) | 10.0.1 | Test coverage collection | MIT |

## Review and maintenance

- Review date: 2026-07-20.
- Update this file whenever `Directory.Packages.props` adds, removes, or materially changes a direct dependency.
- Preserve upstream copyright and license notices when a dependency's license or redistribution model requires them.
- Do not infer redistribution permission from this summary alone; the exact package license metadata and upstream license
  text remain authoritative.
- Vendor SDKs, native libraries, standards-derived implementations, and copied assets require an explicit separate entry
  before they enter source, packages, workers, or the Showcase image.
