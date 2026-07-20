[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module (Join-Path $PSScriptRoot 'Pocok.GlobalRelease.psm1') -Force

$valid = ConvertFrom-PocokGlobalTag 'GLOBAL-v1.2.3-alpha.4'
if ($valid.Version -ne '1.2.3-alpha.4') { throw 'Valid global tag parsing failed.' }
foreach ($invalid in @('global-v1.2.3','GLOBAL-v1.2','GLOBAL-v1.2.3+meta','conversion-v1.2.3','GLOBAL-vx.y.z')) {
    try { $null = ConvertFrom-PocokGlobalTag $invalid; throw "Expected '$invalid' to fail." } catch { if ($_.Exception.Message -like 'Expected*') { throw } }
}
$graph = @(Get-PocokGlobalReleaseGraph)
if ($graph.Count -eq 0) { throw 'Global graph is empty.' }
if (@($graph.id | Group-Object | Where-Object Count -gt 1).Count -gt 0) { throw 'Global graph contains duplicates.' }
$positions = @{}; foreach ($node in $graph) { $positions[$node.id] = $node.order }
foreach ($node in $graph) { foreach ($dependency in $node.internalDependencies) { if ($positions[$dependency] -ge $positions[$node.id]) { throw "Graph is not dependency-first at $dependency -> $($node.id)." } } }
Write-Host "Global release static tests passed for $($graph.Count) packages."
