[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$Tag,
    [string]$CatalogPath = 'eng/packages.json',
    [string]$GitHubOutput
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$catalogFile = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $CatalogPath))
$catalog = Get-Content -LiteralPath $catalogFile -Raw | ConvertFrom-Json
$matches = @($catalog.packages | Where-Object { $Tag.StartsWith($_.tagPrefix, [StringComparison]::Ordinal) })
if ($matches.Count -ne 1) {
    throw "Tag '$Tag' matched $($matches.Count) package catalog entries."
}

$package = $matches[0]
if (-not $package.releasable) {
    throw "Package $($package.id) is not currently releasable."
}

$version = $Tag.Substring($package.tagPrefix.Length)
if ($version -notmatch '^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$') {
    throw "Tag '$Tag' does not contain a valid semantic version after '$($package.tagPrefix)'."
}

$output = [ordered]@{
    'package-id' = [string]$package.id
    'project' = [string]$package.project
    'version' = $version
    'tag-prefix' = [string]$package.tagPrefix
}

if ($GitHubOutput) {
    foreach ($entry in $output.GetEnumerator()) {
        Add-Content -LiteralPath $GitHubOutput -Value "$($entry.Key)=$($entry.Value)"
    }
}
else {
    [pscustomobject]$output
}
