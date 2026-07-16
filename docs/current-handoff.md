# Current repository handoff

Status revision: `a60c731` plus the Signals-read and Localization extraction slices

Refresh owner: any change that alters release eligibility, acceptance evidence, package closure, or the Modularity gate must update this file or remove obsolete claims.

## Current state

- Waves C and D are structurally implemented.
- A Windows .NET 10.0.102 and PowerShell 7.3.6 acceptance run passed formatting, solution build, 226 tests, core samples, package catalog validation, local-closure smoke, and public release audit on the current working tree. Scripting has a tracked public API snapshot, ten focused behavior tests, source-size and memory bounds, and an installed-package consumer check; Signals has a tracked public API snapshot, twenty-one focused contract/runtime tests, shared subscription lifecycle and point-in-time read behavior, and an installed-package consumer check; Localization has a tracked public API snapshot, four focused composition tests, a sample, and an installed-package consumer check; Subscriptions has a tracked public API snapshot, four focused registry tests, a sample, and an installed-package consumer check.
- Packing succeeded and produced the expected packages and symbols; local MinVer emitted `MINVER1001` warnings because the sandbox Git identity cannot treat the parent repository as a valid working directory.
- The Operations worker sample explicitly owns a console-only logging provider set and does not cancel an already-ready startup cycle during shutdown.
- Modularity remains experimental and non-releasable until its separate Linux and Windows proof gate passes.
- Pocok.Scripting is now an experimental, non-releasable alpha package containing the neutral bounded execution/import slice; product-specific providers and UI/persistence integrations remain outside the extraction.
- Pocok.Signals is now an experimental, non-releasable alpha package containing the neutral live-value contracts and shared runtime, including point-in-time reads; protocol adapters, persistence, caching backends, and product-specific integrations remain outside this extraction.
- Pocok.Localization is now an experimental, non-releasable alpha package containing only deterministic composition over standard .NET string-localizer providers; database, filesystem, resource-assembly discovery, caching, and application-specific integrations remain outside this extraction.
- Pocok.Subscriptions is now an experimental, non-releasable alpha package containing a thread-safe keyed listener registry with typed filtering and mapping; transport connections, retry timers, logging, persistence, and network lifecycle remain outside this extraction.
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
