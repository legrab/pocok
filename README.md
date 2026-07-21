# Pocok

[![NuGet](https://img.shields.io/nuget/v/Pocok.Conversion?label=NuGet&logo=nuget)](https://www.nuget.org/packages?q=Pocok)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](LICENSE)

Pocok is a focused .NET package portfolio built from recurring application needs. Its packages provide small runtime
capabilities and transparent application defaults without introducing a private framework.

> ## Explore Pocok in the Showcase
>
> Browse all current package families, edit bounded inputs, run safe demonstrations, and generate source-accurate recipes
> in the deployed application. JavaScript execution is available publicly; trusted-local C# and Python execution remain
> explicitly operator-gated.
>
> **[Open the deployed Pocok Showcase](https://pocok-showcase.onrender.com/)**

## Packages

| Area | Packages |
|---|---|
| Runtime capabilities | [![Pocok.Conversion](https://img.shields.io/nuget/v/Pocok.Conversion?label=Pocok.Conversion&logo=nuget)](https://www.nuget.org/packages/Pocok.Conversion)<br>[![Pocok.Readiness](https://img.shields.io/nuget/v/Pocok.Readiness?label=Pocok.Readiness&logo=nuget)](https://www.nuget.org/packages/Pocok.Readiness)<br>[![Pocok.BackgroundWork](https://img.shields.io/nuget/v/Pocok.BackgroundWork?label=Pocok.BackgroundWork&logo=nuget)](https://www.nuget.org/packages/Pocok.BackgroundWork)<br>[![Pocok.Localization](https://img.shields.io/nuget/v/Pocok.Localization?label=Pocok.Localization&logo=nuget)](https://www.nuget.org/packages/Pocok.Localization)<br>[![Pocok.Signals](https://img.shields.io/nuget/v/Pocok.Signals?label=Pocok.Signals&logo=nuget)](https://www.nuget.org/packages/Pocok.Signals)<br>[![Pocok.Subscriptions](https://img.shields.io/nuget/v/Pocok.Subscriptions?label=Pocok.Subscriptions&logo=nuget)](https://www.nuget.org/packages/Pocok.Subscriptions)<br>[![Pocok.Licensing](https://img.shields.io/nuget/v/Pocok.Licensing?label=Pocok.Licensing&logo=nuget)](https://www.nuget.org/packages/Pocok.Licensing) |
| Scripting | [![Pocok.Scripting](https://img.shields.io/nuget/v/Pocok.Scripting?label=Pocok.Scripting&logo=nuget)](https://www.nuget.org/packages/Pocok.Scripting)<br>[![Pocok.Scripting.JavaScript](https://img.shields.io/nuget/v/Pocok.Scripting.JavaScript?label=Pocok.Scripting.JavaScript&logo=nuget)](https://www.nuget.org/packages/Pocok.Scripting.JavaScript)<br>[![Pocok.Scripting.CSharp](https://img.shields.io/nuget/v/Pocok.Scripting.CSharp?label=Pocok.Scripting.CSharp&logo=nuget)](https://www.nuget.org/packages/Pocok.Scripting.CSharp)<br>[![Pocok.Scripting.Python](https://img.shields.io/nuget/v/Pocok.Scripting.Python?label=Pocok.Scripting.Python&logo=nuget)](https://www.nuget.org/packages/Pocok.Scripting.Python) |
| Modularity | [![Pocok.Modularity.Contracts](https://img.shields.io/nuget/v/Pocok.Modularity.Contracts?label=Pocok.Modularity.Contracts&logo=nuget)](https://www.nuget.org/packages/Pocok.Modularity.Contracts)<br>[![Pocok.Modularity](https://img.shields.io/nuget/v/Pocok.Modularity?label=Pocok.Modularity&logo=nuget)](https://www.nuget.org/packages/Pocok.Modularity) |
| Application defaults | [![Pocok.AppDefaults](https://img.shields.io/nuget/v/Pocok.AppDefaults?label=Pocok.AppDefaults&logo=nuget)](https://www.nuget.org/packages/Pocok.AppDefaults)<br>[![Pocok.AppDefaults.Logging](https://img.shields.io/nuget/v/Pocok.AppDefaults.Logging?label=Pocok.AppDefaults.Logging&logo=nuget)](https://www.nuget.org/packages/Pocok.AppDefaults.Logging)<br>[![Pocok.AppDefaults.Logging.Serilog](https://img.shields.io/nuget/v/Pocok.AppDefaults.Logging.Serilog?label=Pocok.AppDefaults.Logging.Serilog&logo=nuget)](https://www.nuget.org/packages/Pocok.AppDefaults.Logging.Serilog)<br>[![Pocok.AppDefaults.Modularity](https://img.shields.io/nuget/v/Pocok.AppDefaults.Modularity?label=Pocok.AppDefaults.Modularity&logo=nuget)](https://www.nuget.org/packages/Pocok.AppDefaults.Modularity)<br>[![Pocok.AppDefaults.Licensing](https://img.shields.io/nuget/v/Pocok.AppDefaults.Licensing?label=Pocok.AppDefaults.Licensing&logo=nuget)](https://www.nuget.org/packages/Pocok.AppDefaults.Licensing) |

`Active` and `Experimental` describe API maturity in the package catalog. Both states may be alpha-publication eligible
when `eng/packages.json` marks the package releasable. Scripting and Licensing also publish package-specific tags;
Licensing issuer and checker executables remain GitHub Release assets rather than NuGet packages.

<details>
<summary><strong>Unit test coverage</strong></summary>

<!-- pocok-coverage:start -->
### Per-slice coverage

| Slice | Head lines | Base lines | Delta | Head branches | Base branches | Delta |
|---|---:|---:|---:|---:|---:|---:|
| Pocok.AppDefaults | 100.00% | 100.00% | 0.00 pp | 100.00% | 100.00% | 0.00 pp |
| Pocok.AppDefaults.Licensing | 89.69% | 89.69% | 0.00 pp | 90.48% | 90.48% | 0.00 pp |
| Pocok.AppDefaults.Logging | 87.04% | 87.04% | 0.00 pp | 100.00% | 100.00% | 0.00 pp |
| Pocok.AppDefaults.Logging.Serilog | 100.00% | 100.00% | 0.00 pp | 100.00% | 100.00% | 0.00 pp |
| Pocok.AppDefaults.Modularity | 97.37% | 97.37% | 0.00 pp | 100.00% | 100.00% | 0.00 pp |
| Pocok.BackgroundWork | 78.34% | 78.34% | 0.00 pp | 93.59% | 93.59% | 0.00 pp |
| Pocok.Conversion | 83.01% | 83.01% | 0.00 pp | 94.14% | 94.14% | 0.00 pp |
| Pocok.Licensing | 78.71% | 78.71% | 0.00 pp | 92.03% | 92.03% | 0.00 pp |
| Pocok.Localization | 89.42% | 89.42% | 0.00 pp | 99.19% | 99.19% | 0.00 pp |
| Pocok.Modularity | 87.35% | 87.35% | 0.00 pp | 95.31% | 95.31% | 0.00 pp |
| Pocok.Modularity.Contracts | 92.86% | 92.86% | 0.00 pp | 100.00% | 100.00% | 0.00 pp |
| Pocok.Readiness | 93.52% | 93.52% | 0.00 pp | 100.00% | 100.00% | 0.00 pp |
| Pocok.Scripting | 80.12% | 80.12% | 0.00 pp | 87.50% | 87.50% | 0.00 pp |
| Pocok.Scripting.CSharp | 77.18% | 77.18% | 0.00 pp | 92.11% | 92.11% | 0.00 pp |
| Pocok.Scripting.JavaScript | 65.61% | 65.61% | 0.00 pp | 52.86% | 52.86% | 0.00 pp |
| Pocok.Scripting.Python | 79.84% | 79.84% | 0.00 pp | 100.00% | 100.00% | 0.00 pp |
| Pocok.Signals | 80.00% | 80.00% | 0.00 pp | 93.57% | 93.57% | 0.00 pp |
| Pocok.Subscriptions | 100.00% | 100.00% | 0.00 pp | 100.00% | 100.00% | 0.00 pp |

Coverage is refreshed automatically from successful `main` CI. Line coverage is authoritative; branch coverage is shown only when condition identities can be merged safely.
<!-- pocok-coverage:end -->

</details>

## Quick start

Install only the capabilities used by the application. The snippets below mirror the repository console samples and keep
configuration explicit at the composition root.

<details>
<summary><strong>Pocok.Conversion</strong> — strict and policy-driven value conversion</summary>

```bash
dotnet add package Pocok.Conversion
```

```csharp
using Pocok.Conversion;

var converter = new ValueConverter();
ConversionResult<int> result = converter.Convert<int>("42");
if (result.IsFailure)
    throw new InvalidOperationException($"{result.Error!.Code}: {result.Error.Message}");

Console.WriteLine(result.Value);
```

See [`samples/Conversion.Console`](samples/Conversion.Console) and the explicit trimming fixture in
[`samples/Conversion.Trimmed`](samples/Conversion.Trimmed).

</details>

<details>
<summary><strong>Pocok.Readiness</strong> — observable startup and shutdown readiness</summary>

```bash
dotnet add package Pocok.Readiness
```

```csharp
using Pocok.Readiness;

var readiness = new ReadinessSource();
ReadinessCycle cycle = readiness.BeginStartup();

Task waiter = readiness.WaitUntilReadyAsync();
readiness.MarkReady(cycle);
await waiter;

readiness.BeginShutdown();
readiness.MarkStopped();
```

See [`samples/Readiness.Console`](samples/Readiness.Console).

</details>

<details>
<summary><strong>Pocok.AppDefaults + logging integrations</strong></summary>

```bash
dotnet add package Pocok.AppDefaults
dotnet add package Pocok.AppDefaults.Logging
# Alternative provider policy:
dotnet add package Pocok.AppDefaults.Logging.Serilog
```

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pocok.AppDefaults;
using Pocok.AppDefaults.Logging;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.ConfigureWith(new LoggingDefaultsConfigurator(options =>
{
    options.MinimumLevel = LogLevel.Information;
    options.CategoryMinimumLevels["Microsoft.Hosting.Lifetime"] = LogLevel.Warning;
}));
```

Use Serilog instead of the provider-neutral policy when the application owns Serilog configuration:

```csharp
using Pocok.AppDefaults.Logging.Serilog;

builder.AddPocokSerilogDefaults();
```

See [`samples/AppDefaults.Console`](samples/AppDefaults.Console).

</details>

<details>
<summary><strong>Pocok.BackgroundWork</strong> — observation, coalescing, debounce, and repetition</summary>

```bash
dotnet add package Pocok.BackgroundWork
```

```csharp
using Pocok.BackgroundWork.Observation;

TaskObservation observation = Task.FromResult(42).Observe(
    exception => Console.Error.WriteLine(exception),
    options => options.OnSuccess(value => value == 42, value => Console.WriteLine(value)));

TaskObservationResult result = await observation.Completion;
```

The full sample also demonstrates `CoalescingTaskRunner`, `DebouncedTaskRunner`, and `TaskRepeater` in
[`samples/BackgroundWork.Console`](samples/BackgroundWork.Console).

</details>

<details>
<summary><strong>Pocok.Scripting + JavaScript, C#, and Python adapters</strong></summary>

```bash
dotnet add package Pocok.Scripting
dotnet add package Pocok.Scripting.JavaScript
dotnet add package Pocok.Scripting.CSharp
dotnet add package Pocok.Scripting.Python
```

```csharp
using Pocok.Scripting.CSharp;
using Pocok.Scripting.Execution;
using Pocok.Scripting.JavaScript;
using Pocok.Scripting.Python;

IScriptEngineAdapter[] adapters =
[
    new JavaScriptScriptEngineAdapter(),
    new CSharpScriptEngineAdapter(),
    new PythonScriptEngineAdapter()
];

var runner = new ScriptRunner(new ScriptEngineRegistry(adapters));
ScriptResult<int> result = await runner.ExecuteAsync<int>(
    new ScriptExecutionRequest(ScriptEngineId.JavaScript, "quick-start", "21 * 2;")
    {
        ExpectResult = true
    },
    new ScriptExecutionOptions { Timeout = TimeSpan.FromSeconds(2) });
```

The core package contains no language runtime. C# and Python require their documented worker/runtime paths and are
trusted-local guardrails, not operating-system sandboxes. See [`samples/Scripting.Console`](samples/Scripting.Console).

</details>

<details>
<summary><strong>Pocok.Localization</strong> — deterministic JSON/RESX composition</summary>

```bash
dotnet add package Pocok.Localization
```

```csharp
using Pocok.Localization.Composition;
using Pocok.Localization.FileResources;

await using var files = new FileStringLocalizer(new FileStringLocalizerOptions
{
    RootDirectory = resourceDirectory,
    BaseName = "Messages"
});

var localizer = new CompositeStringLocalizer([files]);
Console.WriteLine(localizer["Greeting", "Pocok"].Value);
```

See [`samples/Localization.Console`](samples/Localization.Console).

</details>

<details>
<summary><strong>Pocok.Modularity.Contracts + Pocok.Modularity + AppDefaults policy</strong></summary>

```bash
dotnet add package Pocok.Modularity.Contracts
dotnet add package Pocok.Modularity
# Optional conventional host policy:
dotnet add package Pocok.AppDefaults.Modularity
```

```csharp
using Pocok.Modularity;

builder.Services.AddPocokModules(builder.Configuration, options =>
{
    options.AddDirectory(pluginDirectory);
    // Share application-owned contracts used by both the host and plugins.
    options.ShareAssemblyContaining<ICommunicator>();
});
```

Plugin assemblies implement `IServiceModule` from `Pocok.Modularity.Contracts`. Applications preferring conventional
`<content-root>/plugins` policy may call `builder.AddPocokModularityDefaults(...)`. See the complete staged-plugin example
in [`samples/ModularCommunicator`](samples/ModularCommunicator).

</details>

<details>
<summary><strong>Pocok.Signals</strong> — quality-aware live values</summary>

```bash
dotnet add package Pocok.Signals
```

```csharp
using Pocok.Signals.Runtime;
using Pocok.Signals.Sources;

var address = new SignalAddress(new SourceId("demo"), "temperature/outlet");
var sample = new SignalSample<double>(
    21.5,
    true,
    DateTimeOffset.UtcNow,
    DateTimeOffset.UtcNow,
    SignalQuality.Good,
    1);
```

See [`samples/Signals.Console`](samples/Signals.Console).

</details>

<details>
<summary><strong>Pocok.Subscriptions</strong> — keyed in-process subscriptions</summary>

```bash
dotnet add package Pocok.Subscriptions
```

```csharp
using Pocok.Subscriptions;

using KeyedSubscriptionHub<string> hub = new();
using IDisposable registration = hub.Subscribe<int>(
    "temperature",
    (_, value) => Console.WriteLine(value));

hub.Publish("temperature", 21);
```

See [`samples/Subscriptions.Console`](samples/Subscriptions.Console).

</details>

<details>
<summary><strong>Pocok.Licensing + AppDefaults enforcement</strong></summary>

```bash
dotnet add package Pocok.Licensing
dotnet add package Pocok.AppDefaults.Licensing
```

```csharp
using Pocok.AppDefaults.Licensing;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.AddPocokLicensingDefaults(options =>
{
    options.LicensePath = "license.pocok";
    options.TrustedPublicKeyFiles["production"] = "keys/license-public.pem";
    options.RequiredModules = ["Reporting"];
});
```

Production issuers keep private signing keys outside application deployments. See
[`samples/Licensing.Console`](samples/Licensing.Console) and the full [licensing guide](docs/licensing.md).

</details>

<details>
<summary><strong>Combined worker composition</strong></summary>

[`samples/Operations.Worker`](samples/Operations.Worker) combines AppDefaults logging, Conversion, and Readiness in one
hosted-service workflow. It is the reference sample for composing multiple Pocok packages without introducing a shared
framework layer.

</details>

More package-specific examples and boundaries are documented in each package README under [`src`](src). The deployable
Showcase and its ten plugins are documented in [showcase/README.md](showcase/README.md).

<details>
<summary><strong>Detailed package status</strong></summary>

| Package | Catalog state | Purpose |
|---|---|---|
| `Pocok.Conversion` | Active, releasable | Strict, serializer-free, policy-driven runtime value conversion |
| `Pocok.Readiness` | Active, releasable | Observable and restartable readiness lifecycle coordination |
| `Pocok.AppDefaults` | Active, releasable | Explicit ordered application configurators |
| `Pocok.AppDefaults.Logging` | Active, releasable | Conservative provider-neutral logging defaults |
| `Pocok.AppDefaults.Logging.Serilog` | Active, releasable | Configuration-driven Serilog hosting defaults |
| `Pocok.Modularity.Contracts` | Experimental, releasable | Startup plugin contracts shared by hosts and plugins |
| `Pocok.Modularity` | Experimental, releasable | Trusted startup-time plugin discovery and DI registration |
| `Pocok.AppDefaults.Modularity` | Experimental, releasable | Conventional host policy for modularity |
| `Pocok.BackgroundWork` | Experimental, releasable | Guarded observation, coalescing, debounce, and non-overlapping repetition |
| `Pocok.Scripting` | Active, releasable | Engine-neutral bounded script execution contracts and orchestration |
| `Pocok.Scripting.JavaScript` | Experimental, releasable | Jint-backed JavaScript adapter with parser-backed guardrails |
| `Pocok.Scripting.CSharp` | Experimental, releasable | Trusted-local, worker-isolated C# adapter |
| `Pocok.Scripting.Python` | Experimental, releasable | Trusted-local, process-isolated CPython 3.14 adapter |
| `Pocok.Localization` | Experimental, releasable | Deterministic localization composition and external resources |
| `Pocok.Signals` | Experimental, releasable | Quality-aware live values and shared connection runtime |
| `Pocok.Subscriptions` | Experimental, releasable | Thread-safe keyed subscriptions with typed filtering and mapping |
| `Pocok.Licensing` | Active, releasable | Signed offline licenses with module, time, runtime, machine, and PSK constraints |
| `Pocok.AppDefaults.Licensing` | Active, releasable | Startup and periodic host enforcement for Pocok licensing |

`Pocok.Primitives`, `Pocok.Hosting`, and `Pocok.Conversion.Abstractions` are retired without forwarding packages. See the
[migration guides](docs/migrations).

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
[PUBLICATION.md](PUBLICATION.md), [the synchronized release guide](docs/global-release.md), and the
[current handoff](docs/current-handoff.md).

</details>

<details>
<summary><strong>Architecture and repository records</strong></summary>

Capability packages own runtime behavior. Maintainer-default packages configure standard .NET or explicitly selected
third-party infrastructure. There is no public `Common`, `Utils`, `Foundation`, or generic `Primitives` package.

- [Stable repository rules](AGENTS.md)
- [Agentic workflow](docs/agentic-workflow.md)
- [Learning the repository and its harness](docs/agentic-learning.md)
- [Current handoff](docs/current-handoff.md)
- [Release-readiness plan and retained execution record](docs/plans/release-readiness.md)
- [Architectural decisions](docs/decisions)

</details>

## License and stewardship

Pocok is licensed under [Apache-2.0](LICENSE). Security reports, contributions, and project stewardship are described in
[SECURITY.md](SECURITY.md), [CONTRIBUTING.md](CONTRIBUTING.md), and [STEWARDSHIP.md](STEWARDSHIP.md).
