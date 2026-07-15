[CmdletBinding()]
param([string]$CatalogPath = 'eng/packages.json')

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$catalog = Get-Content -LiteralPath (Join-Path $repositoryRoot $CatalogPath) -Raw | ConvertFrom-Json
$ids = @($catalog.packages.id)
$projects = @($catalog.packages.project)
$prefixes = @($catalog.packages.tagPrefix)
$versionProperties = @($catalog.packages.versionProperty)

foreach ($set in @(@{ Name = 'package ID'; Values = $ids }, @{ Name = 'project'; Values = $projects }, @{ Name = 'tag prefix'; Values = $prefixes }, @{ Name = 'version property'; Values = $versionProperties })) {
    $duplicates = @($set.Values | Group-Object | Where-Object Count -gt 1)
    if ($duplicates.Count -gt 0) {
        throw "Duplicate $($set.Name): $($duplicates.Name -join ', ')"
    }
}

foreach ($package in $catalog.packages) {
    $projectPath = Join-Path $repositoryRoot $package.project
    if ($package.state -notin @('Retired') -and -not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
        throw "Catalog project does not exist for $($package.id): $($package.project)"
    }



    if ($package.state -ne 'Retired') {
        [xml]$projectDocument = Get-Content -LiteralPath $projectPath -Raw
        $packageId = [string]($projectDocument.Project.PropertyGroup.PackageId | Select-Object -First 1)
        $tagPrefix = [string]($projectDocument.Project.PropertyGroup.MinVerTagPrefix | Select-Object -First 1)
        if ($packageId -ne $package.id) {
            throw "Catalog ID $($package.id) does not match project PackageId $packageId."
        }
        if ($tagPrefix -ne $package.tagPrefix) {
            throw "Catalog prefix $($package.tagPrefix) does not match project MinVerTagPrefix $tagPrefix."
        }
        if ((Get-Content -LiteralPath $projectPath -Raw) -notmatch [regex]::Escape("$($package.versionProperty)")) {
            throw "$($package.id) does not expose release version property $($package.versionProperty)."
        }
    }

    foreach ($dependency in $package.internalDependencies) {
        if ($ids -notcontains $dependency) {
            throw "$($package.id) references unknown internal dependency $dependency."
        }

        $dependencyPackage = $catalog.packages | Where-Object id -eq $dependency | Select-Object -First 1
        if ($dependencyPackage.releaseTier -ge $package.releaseTier) {
            throw "$($package.id) release tier must be greater than dependency $dependency."
        }
    }
}

Write-Host 'Package catalog validation passed.'
