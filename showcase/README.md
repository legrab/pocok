# Pocok Showcase

A deployable .NET 10 Blazor Web App that acts as the repository-wide package sandbox. Each implemented library owns a trusted startup plugin with documentation, guided samples, editable inputs, bounded execution, and structured result visualization. The host remains package-agnostic while unimplemented current packages stay visible as planned entries.

Implemented slices:

- `Pocok.Conversion`: constrained conversion builder and parser over the real package API;
- `Pocok.Scripting`: complete JavaScript execution through the real bounded `ScriptRunner`, including return values and structured failures.

## Run locally

```bash
bash showcase/scripts/run-showcase.sh
```

```powershell
./showcase/scripts/run-showcase.ps1
```

The application listens on `PORT` when supplied and otherwise uses `8080`. Publication is discovery-based:

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
dotnet build showcase/Pocok.Showcase.slnx -c Release --no-restore
dotnet test showcase/tests/Pocok.Showcase.Tests/Pocok.Showcase.Tests.csproj -c Release --no-build
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
- [Koyeb](docs/DEPLOY_KOYEB.md)
- [Azure Container Apps](docs/DEPLOY_AZURE_CONTAINER_APPS.md)
