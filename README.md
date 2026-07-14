# Pocok

Pocok is a collection of small, independently packaged .NET libraries for recurring infrastructure problems. Packages stay narrow, explicit, and usable without adopting an application framework or a large convenience assembly.

## Planned packages

| Package | Purpose | Status |
|---|---|---|
| [`Pocok.Primitives`](src/Primitives/README.md) | Result and structured error contracts | Public candidate |
| [`Pocok.Conversion.Abstractions`](src/Conversion.Abstractions/README.md) | Explicit runtime conversion policies and contracts | Experimental alpha |
| [`Pocok.Conversion`](src/Conversion/README.md) | Strict serializer-free runtime value conversion | Experimental alpha |
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
pwsh ./tools/PackageSmoke/Invoke-PackageSmoke.ps1
```

The repository uses central package management and committed dependency lock files. Project references may not escape this repository.

## Publishing

Package versions are derived by MinVer from package-specific Git tags. The
first package uses tags such as `primitives-v0.1.0-alpha.1` and is published by
`.github/workflows/publish-primitives.yml` after validation. A local build
without a release tag uses MinVer's development version and must not be
published.

To enable publishing, create a `NUGET_USERNAME` repository variable containing
the nuget.org profile name and configure a NuGet trusted-publishing policy for
`legrab/pocok` and `publish-primitives.yml`. The workflow requests a
short-lived publishing credential through GitHub Actions OIDC.

## License

Pocok is licensed under the [Apache License 2.0](LICENSE). Commercial, educational, private, and noncommercial use are permitted under its terms. See [NOTICE](NOTICE) for attribution and [STEWARDSHIP.md](STEWARDSHIP.md) for a nonbinding community request.
