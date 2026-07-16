# Current repository handoff

Status revision: `cdfb8bfaec5dcc74c62e5f6d5d401222c82dc45a`

Refresh owner: any change that alters release eligibility, acceptance evidence, package closure, or the Modularity gate must update this file or remove obsolete claims.

## Current state

- Waves C and D are structurally implemented.
- A previous .NET 10.0.102 and PowerShell 7 run recorded 182 passing tests plus successful formatting, packing, smoke, audit, and samples for the earlier baseline.
- The latest package-closure and AppDefaults policy edits require a fresh executable acceptance run on the exact current HEAD.
- Modularity remains experimental and non-releasable until its separate Linux and Windows proof gate passes.
- Do not create release tags until candidate-scoped closure, publication-shaped restore, audit, and CI evidence are current.

## Next action

Run the acceptance matrix on the exact HEAD, fix only factual failures found by that evidence, and update the implementation ledger and this handoff with the results.

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
