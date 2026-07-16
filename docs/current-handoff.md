# Current repository handoff

Status revision: `5b7fe8b` plus the current uncommitted acceptance fixes

Refresh owner: any change that alters release eligibility, acceptance evidence, package closure, or the Modularity gate must update this file or remove obsolete claims.

## Current state

- Waves C and D are structurally implemented.
- A Windows .NET 10.0.102 and PowerShell 7.3.6 acceptance run passed formatting, solution build, 192 tests, core samples, package catalog validation, local-closure smoke, and public release audit on the current working tree. Scripting now also has a tracked public API snapshot, ten focused behavior tests, source-size and memory bounds, and an installed-package consumer check.
- Packing succeeded and produced the expected packages and symbols; local MinVer emitted `MINVER1001` warnings because the sandbox Git identity cannot treat the parent repository as a valid working directory.
- The Operations worker sample explicitly owns a console-only logging provider set and does not cancel an already-ready startup cycle during shutdown.
- Modularity remains experimental and non-releasable until its separate Linux and Windows proof gate passes.
- Pocok.Scripting is now an experimental, non-releasable alpha package containing the neutral bounded execution/import slice; product-specific providers and UI/persistence integrations remain outside the extraction.
- Do not create release tags until Linux CI, candidate-scoped publication-shaped restore, audit, and debugger Source Link evidence are current.

## Next action

Obtain Linux CI evidence, verify real debugger Source Link behavior from an installed candidate package, and run candidate-scoped publication smoke before release eligibility changes.

<details>
<summary>Acceptance matrix</summary>

```pwsh
dotnet --info
$PSVersionTable

dotnet restore Pocok.slnx
dotnet format Pocok.slnx --verify-no-changes --no-restore
dotnet build Pocok.slnx --configuration Release --no-restore
dotnet test Pocok.slnx --configuration Release --no-build
dotnet pack Pocok.slnx --configuration Release --no-build --output artifacts/packages

./tools/PackageCatalog/Test-PackageCatalog.ps1
./tools/PackageSmoke/Invoke-PackageSmoke.ps1 -NoPack -Mode LocalClosure
./tools/PublicReleaseAudit/Invoke-PublicReleaseAudit.ps1
```

For each release candidate, generate release-version props and run candidate-scoped local-closure, publication, and audit modes. Publication mode requires internal dependencies to exist on nuget.org.

</details>
