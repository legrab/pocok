[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Tag,
    [Parameter(Mandatory)][string]$Commit,
    [string]$CatalogPath = 'eng/packages.json',
    [string]$Source = 'https://api.nuget.org/v3/index.json',
    [string]$OutputPath = 'artifacts/global-release/state.json'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module (Join-Path $PSScriptRoot 'Pocok.GlobalRelease.psm1') -Force
$version = (ConvertFrom-PocokGlobalTag -Tag $Tag).Version
$graph = @(Get-PocokGlobalReleaseGraph -CatalogPath $CatalogPath)
Test-PocokReleaseTags -GlobalTag $Tag -Commit $Commit -Graph $graph
$states = [System.Collections.Generic.List[object]]::new()
foreach ($package in $graph) {
    $state = Get-PocokNuGetPackageState -PackageId $package.id -Version $version -ExpectedCommit $Commit -Source $Source
    $states.Add([pscustomobject]@{ order=$package.order; packageId=$package.id; dependencies=$package.internalDependencies; state=$state.State; repositoryCommit=$state.RepositoryCommit })
}
$result = [ordered]@{ tag=$Tag; version=$version; commit=$Commit; checkedAt=[DateTimeOffset]::UtcNow.ToString('O'); packages=@($states) }
Write-PocokJson -Value $result -Path $OutputPath | Out-Null
$result
