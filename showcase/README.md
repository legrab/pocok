# Pocok Showcase

A deployable .NET 10 Blazor Web App for browsing Pocok packages, running bounded examples, and generating constrained
usage recipes against their public APIs. The host remains package-agnostic: trusted startup plugins own package-specific
pages, inputs, localization, and results.

The public deployment is available at [pocok-showcase.onrender.com](https://pocok-showcase.onrender.com/).

## Installed plugins

The ten current plugins cover all eighteen non-retired library packages:

| Plugin | Covered packages | Interaction |
|---|---|---|
| AppDefaults and Logging | `Pocok.AppDefaults`, `Pocok.AppDefaults.Logging`, `Pocok.AppDefaults.Logging.Serilog` | Bounded real logging demonstration and fresh-builder configuration probe |
| BackgroundWork | `Pocok.BackgroundWork` | Typed source-accurate recipe builder |
| Conversion | `Pocok.Conversion` | Bounded conversion inputs, policies, results, and structured failures |
| Licensing | `Pocok.Licensing`, `Pocok.AppDefaults.Licensing` | In-memory claim validation and safe host-policy guidance without exposing signing keys |
| Localization | `Pocok.Localization` | Bounded real JSON/RESX, fallback, composition, enum, and reload demonstration |
| Modularity | `Pocok.Modularity.Contracts`, `Pocok.Modularity`, `Pocok.AppDefaults.Modularity` | Typed source-accurate recipe builder |
| Readiness | `Pocok.Readiness` | Typed lifecycle recipe builder |
| Scripting | `Pocok.Scripting`, `Pocok.Scripting.JavaScript`, `Pocok.Scripting.CSharp`, `Pocok.Scripting.Python` | Engine-neutral runner; JavaScript public, C# and Python explicitly trusted-local |
| Signals | `Pocok.Signals` | Typed source-accurate recipe builder |
| Subscriptions | `Pocok.Subscriptions` | Typed source-accurate recipe builder |

Recipe plugins generate source from typed options and do not compile or execute the generated source. Validation,
process separation, timeouts, and resource bounds are guardrails rather than an operating-system sandbox.

## Solution boundaries

- `showcase/Pocok.Showcase.slnx` contains only the reusable Showcase framework, Web host, framework tests, and publication
  tool.
- `showcase/Pocok.Showcase.Samples.slnx` contains the package-owned sample plugins and their tests.
- Neither solution is included in the repository package solutions. The dedicated Showcase workflow validates both.

Building the sample solution stages each plugin under `showcase/src/Pocok.Showcase.Web/plugins/<module-id>`. The Web host
discovers that directory at startup without referencing a concrete sample project. The directory is generated and ignored
by Git and Docker.

## Run locally

For direct IDE or `dotnet run` development, build the plugins once before starting the host:

```text
dotnet build showcase/Pocok.Showcase.Samples.slnx
dotnet run --project showcase/src/Pocok.Showcase.Web/Pocok.Showcase.Web.csproj
```

The publication-backed runners build and run the complete application in an isolated directory:

```bash
bash showcase/scripts/run-showcase.sh
```

```powershell
./showcase/scripts/run-showcase.ps1
```

The application listens on `PORT` when supplied and otherwise uses `8080`. Publication is manifest-based:

```bash
bash showcase/scripts/publish-showcase.sh /absolute/output --no-restore
python showcase/scripts/smoke-showcase.py /absolute/output
```

```powershell
./showcase/scripts/publish-showcase.ps1 -OutputPath C:\temp\pocok-showcase -NoRestore
python showcase/scripts/smoke-showcase.py C:\temp\pocok-showcase
```

Set `Showcase__TrustedScriptEnginesEnabled=true` only in a controlled local/private deployment that also supplies the
runtime and worker paths documented in
[`samples/Showcase/Pocok.Showcase.Scripting/README.md`](../samples/Showcase/Pocok.Showcase.Scripting/README.md).

## Validate

```text
dotnet restore showcase/Pocok.Showcase.slnx
dotnet restore showcase/Pocok.Showcase.Samples.slnx
dotnet build showcase/Pocok.Showcase.slnx -c Release --no-restore
dotnet build showcase/Pocok.Showcase.Samples.slnx -c Release --no-restore
dotnet test showcase/tests/Pocok.Showcase.Tests/Pocok.Showcase.Tests.csproj -c Release --no-build
dotnet test samples/Showcase/Pocok.Showcase.Samples.Tests/Pocok.Showcase.Samples.Tests.csproj -c Release --no-build
```

Build the canonical container from the repository root:

```text
docker build -f showcase/Dockerfile -t pocok-showcase .
docker run --rm -p 8080:8080 -e Showcase__RequireCompleteCatalog=true pocok-showcase
```

## Documentation

- [Architecture](docs/ARCHITECTURE.md)
- [Adding a slice](docs/ADDING_A_SLICE.md)
- [Package coverage](docs/PLANNED_SLICES.md)
- [Security](docs/SECURITY.md)
- [Render](docs/DEPLOY_RENDER.md)
- [Azure Container Apps](docs/DEPLOY_AZURE_CONTAINER_APPS.md)
