[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Tag,
    [string]$CatalogPath = 'eng/packages.json',
    [string]$OutputPath = 'artifacts/global-release/plan.json',
    [string]$GitHubOutput,
    [string]$Commit
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module (Join-Path $PSScriptRoot 'Pocok.GlobalRelease.psm1') -Force
$root = Get-PocokRepositoryRoot
$tagInfo = ConvertFrom-PocokGlobalTag -Tag $Tag
if (-not $Commit) { $Commit = (& git -C $root rev-parse HEAD).Trim() }
$graph = @(Get-PocokGlobalReleaseGraph -CatalogPath $CatalogPath)
Test-PocokReleaseTags -GlobalTag $Tag -Commit $Commit -Graph $graph
$plan = [ordered]@{ tag=$Tag; version=$tagInfo.Version; commit=$Commit; generatedAt=[DateTimeOffset]::UtcNow.ToString('O'); packages=$graph }
$resolved = Write-PocokJson -Value $plan -Path $OutputPath
if ($GitHubOutput) {
    Add-Content -LiteralPath $GitHubOutput -Value "version=$($tagInfo.Version)"
    Add-Content -LiteralPath $GitHubOutput -Value "commit=$Commit"
    Add-Content -LiteralPath $GitHubOutput -Value "package-count=$($graph.Count)"
    Add-Content -LiteralPath $GitHubOutput -Value "plan=$resolved"
}
Write-Host "Resolved $($graph.Count) packages for $Tag at $Commit."
