[CmdletBinding()]
param([string]$RepositoryRoot = (Get-Location).Path)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = [System.IO.Path]::GetFullPath($RepositoryRoot)
if (-not (Test-Path -LiteralPath (Join-Path $root 'Pocok.slnx') -PathType Leaf)) {
    throw "Repository root does not contain Pocok.slnx: $root"
}

$deletions = Join-Path $PSScriptRoot 'DELETIONS.txt'
foreach ($line in Get-Content -LiteralPath $deletions) {
    $relative = $line.Trim()
    if (-not $relative -or $relative.StartsWith('#')) { continue }
    $target = [System.IO.Path]::GetFullPath((Join-Path $root $relative))
    if (-not $target.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Deletion escapes the repository root: $relative"
    }
    if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Recurse -Force }
}

Write-Host 'Patch deletions applied. Copy the remaining archive content over the repository root, then restore and test.'
