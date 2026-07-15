[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$CandidatePackageId,
    [string]$CatalogPath = 'eng/packages.json',
    [string]$OutputPath,
    [string]$GitHubOutput
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$catalogFile = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $CatalogPath))
$catalog = Get-Content -LiteralPath $catalogFile -Raw | ConvertFrom-Json
$byId = @{}
foreach ($package in $catalog.packages) {
    $byId[[string]$package.id] = $package
}

if (-not $byId.ContainsKey($CandidatePackageId)) {
    throw "Unknown package '$CandidatePackageId'."
}

$candidate = $byId[$CandidatePackageId]
if ($candidate.state -eq 'Retired') {
    throw "Retired package '$CandidatePackageId' has no active package closure."
}

$visiting = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$visited = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$ordered = [System.Collections.Generic.List[object]]::new()

function Visit-Package {
    param([Parameter(Mandatory)] [string]$PackageId)

    if ($visited.Contains($PackageId)) {
        return
    }
    if (-not $visiting.Add($PackageId)) {
        throw "Package dependency cycle detected at '$PackageId'."
    }
    if (-not $byId.ContainsKey($PackageId)) {
        throw "Package closure references unknown package '$PackageId'."
    }

    $package = $byId[$PackageId]
    if ($package.state -eq 'Retired') {
        throw "Active package closure references retired package '$PackageId'."
    }

    foreach ($dependency in $package.internalDependencies) {
        Visit-Package -PackageId ([string]$dependency)
    }

    $visiting.Remove($PackageId) | Out-Null
    $visited.Add($PackageId) | Out-Null
    $ordered.Add([pscustomobject]@{
        id = [string]$package.id
        project = [string]$package.project
        versionProperty = [string]$package.versionProperty
    })
}

Visit-Package -PackageId $CandidatePackageId
$result = @($ordered)

if ($OutputPath) {
    $resolvedOutput = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputPath))
    New-Item -ItemType Directory -Path (Split-Path -Parent $resolvedOutput) -Force | Out-Null
    [System.IO.File]::WriteAllText(
        $resolvedOutput,
        (ConvertTo-Json -InputObject $result -Depth 4),
        [System.Text.UTF8Encoding]::new($false))
}

if ($GitHubOutput) {
    Add-Content -LiteralPath $GitHubOutput -Value "package-ids=$($result.id -join ',')"
    Add-Content -LiteralPath $GitHubOutput -Value "projects=$($result.project -join ',')"
}

if (-not $OutputPath -and -not $GitHubOutput) {
    $result
}
