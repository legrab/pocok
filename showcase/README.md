# Pocok Showcase

A deployable .NET 10 Blazor Web App for browsing Pocok packages and running bounded examples against their public APIs. Implemented packages provide trusted startup plugins with documentation, editable inputs, and structured results. The host remains package-agnostic, while packages without an installed example remain visible in the catalog.


The public deployment is available at [pocok-showcase.onrender.com](https://pocok-showcase.onrender.com/).

Installed examples:

- `Pocok.Conversion`: policy-driven conversion through the package API, including collections, enums, temporal values, and structured failures;
- `Pocok.Scripting`: complete JavaScript execution through the bounded `ScriptRunner`, including returned values, limits, and structured failures;
- `Pocok.Licensing`: in-memory claim validation for time windows, runtime limits, modules, machine fingerprints, and pre-shared keys.

## Solution boundaries

- `showcase/Pocok.Showcase.slnx` contains only the reusable Showcase framework, Web host, framework tests, and publication tool.
- `showcase/Pocok.Showcase.Samples.slnx` contains the package-owned sample plugins and their tests.
- Neither solution is included in the repository package solutions. The dedicated Showcase workflow validates both.

Building the sample solution stages each plugin under `showcase/src/Pocok.Showcase.Web/plugins/<module-id>`. The Web host discovers that directory at startup without referencing a concrete sample project. The directory is generated and ignored by Git and Docker.

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
docker run --rm -p 8080:8080 pocok-showcase
```

## Documentation

- [Architecture](docs/ARCHITECTURE.md)
- [Adding a slice](docs/ADDING_A_SLICE.md)
- [Planned slices](docs/PLANNED_SLICES.md)
- [Security](docs/SECURITY.md)
- [Render](docs/DEPLOY_RENDER.md)
- [Azure Container Apps](docs/DEPLOY_AZURE_CONTAINER_APPS.md)
