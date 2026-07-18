# SPDX-License-Identifier: Apache-2.0
# Copyright 2026 Pocok contributors

[CmdletBinding()]
param(
    [string]$PlanPath = 'artifacts/ci/impact.json',
    [string]$CoverageRoot = 'artifacts/coverage/head',
    [string]$ReadmePath = 'README.md',
    [string]$OutputJson = 'artifacts/coverage/readme-coverage-summary.json',
    [string]$OutputMarkdown = 'artifacts/coverage/readme-coverage-summary.md'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module (Join-Path $PSScriptRoot 'Pocok.Ci.psm1') -Force

$repositoryRoot = Get-PocokRepositoryRoot -StartPath $PSScriptRoot
$resolvedReadme = Resolve-PocokPath -RepositoryRoot $repositoryRoot -Path $ReadmePath
$resolvedSummary = Resolve-PocokPath -RepositoryRoot $repositoryRoot -Path $OutputMarkdown

if (-not (Test-Path -LiteralPath $resolvedReadme -PathType Leaf)) {
    throw "README does not exist: $ReadmePath"
}

& (Join-Path $PSScriptRoot 'Write-CoverageSummary.ps1') `
    -PlanPath $PlanPath `
    -HeadRoot $CoverageRoot `
    -BaseRoot $CoverageRoot `
    -OutputJson $OutputJson `
    -OutputMarkdown $OutputMarkdown | Out-Host

$summary = (Get-Content -LiteralPath $resolvedSummary -Raw) -replace "`r`n", "`n"
if ($summary -notmatch '(?m)^\| Pocok\.[^|]+ \|') {
    throw 'Generated coverage summary contains no Pocok slice rows.'
}

$summary = [regex]::Replace(
    $summary,
    '(?m)^# Per-slice coverage$',
    '### Per-slice coverage',
    1)

$summary = $summary.Replace(
    'Coverage changes are advisory. Line coverage is authoritative; branch coverage is shown only when condition identities can be merged safely.',
    'Coverage is refreshed automatically from successful `main` CI. Line coverage is authoritative; branch coverage is shown only when condition identities can be merged safely.')

$summary = $summary.Trim()
$startMarker = '<!-- pocok-coverage:start -->'
$endMarker = '<!-- pocok-coverage:end -->'
$readme = (Get-Content -LiteralPath $resolvedReadme -Raw) -replace "`r`n", "`n"

$pattern = '(?s)' + [regex]::Escape($startMarker) + '.*?' + [regex]::Escape($endMarker)
$regionMatches = [regex]::Matches($readme, $pattern)
if ($regionMatches.Count -ne 1) {
    throw "Expected exactly one README coverage marker region, found $($regionMatches.Count)."
}

$replacement = "$startMarker`n$summary`n$endMarker"
$regionRegex = [regex]::new($pattern)
$updated = $regionRegex.Replace(
    $readme,
    [System.Text.RegularExpressions.MatchEvaluator] {
        param($match)
        return $replacement
    },
    1)

if ($updated -ceq $readme) {
    Write-Host 'README coverage block is already current.'
    return
}

[System.IO.File]::WriteAllText(
    $resolvedReadme,
    $updated,
    [System.Text.UTF8Encoding]::new($false))

Write-Host "Updated README coverage block from '$OutputMarkdown'."
