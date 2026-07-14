# Pocok

Pocok is a collection of small, independently packaged .NET libraries for recurring infrastructure problems. Packages stay narrow, explicit, and usable without adopting an application framework or a large convenience assembly.

## Planned packages

| Package | Purpose | Status |
|---|---|---|
| `Pocok.Primitives` | Result and structured error contracts | Planned first |
| `Pocok.Conversion.*` | Explicit, policy-driven runtime value conversion | Planned |
| `Pocok.Contracts.*` | Deterministic allowlisted contract metadata | Planned |
| `Pocok.Hosting` | Observable host readiness and lifecycle | Planned |
| `Pocok.Numerics` | Carefully specified generic numeric operations | Planned |
| `Pocok.Logging.*` | Opinionated logging composition with optional sinks | Planned |
| `Pocok.Localization.*` | Composite localization and resource adapters | Planned |

No package is published until it has a cohesive public contract, focused tests, a synthetic sample, deterministic package output, and an external local-feed consumer test.

## Development

Requires the .NET 10 SDK.

```shell
dotnet restore --locked-mode
dotnet build --no-restore
dotnet test --no-build
dotnet pack --no-build --output artifacts/packages
```

The repository uses central package management and committed dependency lock files. Project references may not escape this repository.

## License

Pocok is licensed under the [Apache License 2.0](LICENSE). Commercial, educational, private, and noncommercial use are permitted under its terms. See [NOTICE](NOTICE) for attribution and [STEWARDSHIP.md](STEWARDSHIP.md) for a nonbinding community request.
