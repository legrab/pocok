[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Tag,
    [Parameter(Mandatory)][string]$Commit,
    [Parameter(Mandatory)][string]$PackagesPath,
    [Parameter(Mandatory)][string]$ApiKey,
    [string]$CatalogPath = 'eng/packages.json',
    [string]$Source = 'https://api.nuget.org/v3/index.json',
    [TimeSpan]$PropagationTimeout = ([TimeSpan]::FromMinutes(15)),
    [string]$StatePath = 'artifacts/global-release/final-state.json'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module (Join-Path $PSScriptRoot 'Pocok.GlobalRelease.psm1') -Force
$root = Get-PocokRepositoryRoot
$version = (ConvertFrom-PocokGlobalTag -Tag $Tag).Version
$graph = @(Get-PocokGlobalReleaseGraph -CatalogPath $CatalogPath)
Test-PocokReleaseTags -GlobalTag $Tag -Commit $Commit -Graph $graph
$resolvedPackages = [System.IO.Path]::GetFullPath((Join-Path $root $PackagesPath))
$records = [System.Collections.Generic.List[object]]::new()
$byId = @{}
foreach ($package in $graph) {
    $state = Get-PocokNuGetPackageState -PackageId $package.id -Version $version -ExpectedCommit $Commit -Source $Source
    $record = [pscustomobject]@{ order=$package.order; packageId=$package.id; dependencies=$package.internalDependencies; state=$state.State; detail=$null }
    $records.Add($record); $byId[$package.id] = $record
}

function Save-State([string]$Failure = $null) {
    $payload = [ordered]@{ tag=$Tag; version=$version; commit=$Commit; updatedAt=[DateTimeOffset]::UtcNow.ToString('O'); failure=$Failure; packages=@($records) }
    Write-PocokJson -Value $payload -Path $StatePath | Out-Null
    if ($env:GITHUB_STEP_SUMMARY) {
        Add-Content $env:GITHUB_STEP_SUMMARY "## Global release $Tag`n"
        Add-Content $env:GITHUB_STEP_SUMMARY "| Order | Package | State | Detail |`n|---:|---|---|---|"
        foreach ($item in $records) { Add-Content $env:GITHUB_STEP_SUMMARY "| $($item.order) | $($item.packageId) | $($item.state) | $($item.detail) |" }
        if ($Failure) { Add-Content $env:GITHUB_STEP_SUMMARY "`n**Failure:** $Failure" }
    }
}

try {
    foreach ($package in $graph) {
        $record = $byId[$package.id]
        if ($record.state -eq 'AlreadyPublishedMatching') { continue }
        foreach ($dependency in $package.internalDependencies) {
            if ($byId[$dependency].state -notin @('AlreadyPublishedMatching','PublishedAndVerified')) {
                throw "$($package.id) is blocked because dependency $dependency is not verified."
            }
        }
        $nupkg = Join-Path $resolvedPackages "$($package.id).$version.nupkg"
        $snupkg = Join-Path $resolvedPackages "$($package.id).$version.snupkg"
        if (-not (Test-Path $nupkg -PathType Leaf) -or -not (Test-Path $snupkg -PathType Leaf)) { throw "Missing exact artifacts for $($package.id) $version." }
        & dotnet nuget push $nupkg --api-key $ApiKey --source $Source --skip-duplicate
        if ($LASTEXITCODE -ne 0) { throw "NuGet push failed for $($package.id) with exit code $LASTEXITCODE." }
        $null = Wait-PocokNuGetPackage -PackageId $package.id -Version $version -ExpectedCommit $Commit -Source $Source -Timeout $PropagationTimeout
        & (Join-Path $root 'tools/PackageSmoke/Invoke-PackageSmoke.ps1') -NoPack -Mode Publication -PackageIds $package.id
        if ($LASTEXITCODE -ne 0) { throw "Publication-shaped smoke failed for $($package.id)." }
        $record.state = 'PublishedAndVerified'
        Save-State
    }
    Save-State
}
catch {
    $failedMessage = $_.Exception.Message
    $failed = @($records | Where-Object state -eq 'PendingPublish' | Select-Object -First 1)
    if ($failed.Count -gt 0) { $failed[0].state = 'Failed'; $failed[0].detail = $failedMessage }
    foreach ($record in $records | Where-Object state -eq 'PendingPublish') { $record.state = 'Blocked'; $record.detail = 'A prior package failed.' }
    Save-State -Failure $failedMessage
    throw
}
