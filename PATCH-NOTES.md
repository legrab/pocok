# Pocok hardening patch

Baseline: `legrab/pocok` main at `cdfb8bfaec5dcc74c62e5f6d5d401222c82dc45a`.

Changes:

- adds direct tests for namespace-safe NuSpec dependency parsing;
- centralizes package metadata parsing for the public release audit;
- adds CI timeout, locked restore, and package-tooling tests;
- adds an application-shaped Operations worker sample using four released package areas;
- expands Modularity deployment contracts for custom configuration, architecture filtering, and optional-failure escalation;
- updates repository status and sample guidance.

Validation after applying:

```pwsh
./tools/PackageMetadata/Test-PackageMetadata.ps1
dotnet restore Pocok.slnx --locked-mode
dotnet format Pocok.slnx --verify-no-changes --no-restore
dotnet build Pocok.slnx -c Release --no-restore
dotnet test Pocok.slnx -c Release --no-build
```

The patch was assembled from the public GitHub main branch through the GitHub API. The execution environment could not run .NET 10 or PowerShell 7, so the receiving repository must run the commands above before merge.
