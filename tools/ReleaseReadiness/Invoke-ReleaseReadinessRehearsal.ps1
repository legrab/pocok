# SPDX-License-Identifier: Apache-2.0
# Copyright 2026 Pocok contributors

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+-[0-9A-Za-z.-]+$')]
    [string]$Version,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$IncludePublicationShape
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Set-Location $repositoryRoot

$catalog = Get-Content eng/packages.json -Raw | ConvertFrom-Json
$targets = @($catalog.packages | Where-Object { $_.state -ne 'Retired' -and $_.releasable })
if ($targets.Count -ne 18) { throw "Expected exactly 18 releasable libraries, found $($targets.Count)." }

$artifactRoot = Join-Path $repositoryRoot 'artifacts/release-readiness'
$packageRoot = Join-Path $repositoryRoot 'artifacts/packages'
Remove-Item $artifactRoot, $packageRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $artifactRoot, $packageRoot -Force | Out-Null

./tools/PackageCatalog/Test-PackageCatalog.ps1
./tools/PackageCatalog/Test-GlobalRelease.ps1
./tools/PackageCatalog/New-GlobalReleaseVersionsProps.ps1 -Version $Version -OutputPath artifacts/release-readiness/release-versions.props

$env:DOTNET_HOST_PATH = (Get-Command dotnet).Source
$env:POCOK_CSHARP_WORKER_DIRECTORY = Join-Path $repositoryRoot "src/Scripting.CSharp.Worker/bin/$Configuration/net10.0"
$env:POCOK_PYTHON_EXECUTABLE = (Get-Command python).Source
$env:POCOK_PYTHON_WORKER_PATH = Join-Path $repositoryRoot 'src/Scripting.Python/Worker/pocok_worker.py'

./tools/Ci/Invoke-PublicReleaseValidation.ps1 -Configuration $Configuration -ReleaseVersionsFile artifacts/release-readiness/release-versions.props

$releaseProps = Join-Path $repositoryRoot 'artifacts/release-readiness/release-versions.props'
foreach ($package in $targets | Sort-Object releaseTier, id) {
    dotnet pack $package.project --configuration $Configuration --no-build --no-restore --output $packageRoot -p:PocokReleaseVersionsFile=$releaseProps -p:IncludeExperimental=true
    if ($LASTEXITCODE -ne 0) { throw "Packing $($package.id) failed with exit code $LASTEXITCODE." }
}

$targetIds = @($targets.id)
./tools/PackageSmoke/Invoke-PackageSmoke.ps1 -NoPack -Mode LocalClosure -PackageIds $targetIds
if ($IncludePublicationShape) {
    ./tools/PackageSmoke/Invoke-PackageSmoke.ps1 -NoPack -Mode Publication -PackageIds $targetIds
}
./tools/PublicReleaseAudit/Invoke-PublicReleaseAudit.ps1 -PackageIds $targetIds

Remove-Item Env:PLATFORM -ErrorAction SilentlyContinue
dotnet restore showcase/Pocok.Showcase.slnx --verbosity minimal
dotnet restore showcase/Pocok.Showcase.Samples.slnx --verbosity minimal
dotnet format showcase/Pocok.Showcase.slnx --verify-no-changes --no-restore
dotnet format showcase/Pocok.Showcase.Samples.slnx --verify-no-changes --no-restore
dotnet build showcase/Pocok.Showcase.slnx --configuration $Configuration --no-restore
dotnet build showcase/Pocok.Showcase.Samples.slnx --configuration $Configuration --no-restore
dotnet test showcase/tests/Pocok.Showcase.Tests/Pocok.Showcase.Tests.csproj --configuration $Configuration --no-build
dotnet test samples/Showcase/Pocok.Showcase.Samples.Tests/Pocok.Showcase.Samples.Tests.csproj --configuration $Configuration --no-build
./showcase/scripts/publish-showcase.ps1 -OutputPath artifacts/release-readiness/showcase -NoRestore -RequireComplete
python showcase/scripts/smoke-showcase.py artifacts/release-readiness/showcase

$artifacts = Get-ChildItem $packageRoot -File | Sort-Object Name | ForEach-Object {
    [pscustomobject]@{
        name = $_.Name
        length = $_.Length
        sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}
$manifest = [ordered]@{
    schemaVersion = 1
    version = $Version
    commit = (& git rev-parse HEAD).Trim()
    generatedAt = [DateTimeOffset]::UtcNow.ToString('O')
    packageIds = $targetIds
    artifacts = @($artifacts)
    publicationShapeIncluded = [bool]$IncludePublicationShape
    tagCreated = $false
    packagesPublished = $false
}
$manifest | ConvertTo-Json -Depth 8 | Set-Content artifacts/release-readiness/candidate-manifest.json -Encoding utf8NoBOM
Write-Host "Release-readiness rehearsal completed without creating a tag or publishing packages."
