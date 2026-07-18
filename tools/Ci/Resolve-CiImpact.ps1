# SPDX-License-Identifier: Apache-2.0
# Copyright 2026 Pocok contributors

[CmdletBinding()]
param(
    [string]$BaseSha,
    [string]$HeadSha = 'HEAD',
    [string]$OutputPath = 'artifacts/ci/impact.json',
    [string]$GitHubOutput,
    [ValidateSet('pull_request', 'push', 'workflow_dispatch', 'local')]
    [string]$EventName = 'local',
    [switch]$ForceFull,
    [object[]]$ChangedFiles
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module (Join-Path $PSScriptRoot 'Pocok.Ci.psm1') -Force

$repositoryRoot = Get-PocokRepositoryRoot -StartPath $PSScriptRoot
$resolvedOutput = Resolve-PocokPath -RepositoryRoot $repositoryRoot -Path $OutputPath

function Get-GitChanges {
    param([string]$Base, [string]$Head)

    if ([string]::IsNullOrWhiteSpace($Base) -or [string]::IsNullOrWhiteSpace($Head)) {
        throw 'Base and head SHAs are required when changed files are not supplied.'
    }

    & git -C $repositoryRoot cat-file -e "$Base^{commit}" 2>$null
    if ($LASTEXITCODE -ne 0) { throw "Base commit '$Base' is unavailable." }
    & git -C $repositoryRoot cat-file -e "$Head^{commit}" 2>$null
    if ($LASTEXITCODE -ne 0) { throw "Head commit '$Head' is unavailable." }

    $lines = @(& git -C $repositoryRoot diff --name-status --find-renames --find-copies $Base $Head)
    if ($LASTEXITCODE -ne 0) { throw 'git diff failed.' }
    $changes = [System.Collections.Generic.List[object]]::new()
    foreach ($line in $lines) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $parts = $line -split "`t"
        $status = $parts[0]
        if (($status.StartsWith('R', [StringComparison]::Ordinal) -or $status.StartsWith('C', [StringComparison]::Ordinal)) -and $parts.Count -ge 3) {
            $changes.Add([pscustomobject]@{ Status = $status; OldPath = $parts[1]; Path = $parts[2] })
        }
        elseif ($parts.Count -ge 2) {
            $changes.Add([pscustomobject]@{ Status = $status; OldPath = $null; Path = $parts[1] })
        }
    }
    return @($changes)
}

$changes = if ($PSBoundParameters.ContainsKey('ChangedFiles')) {
    @($ChangedFiles | ForEach-Object {
        if ($_ -is [string]) {
            [pscustomobject]@{ Status = 'M'; OldPath = $null; Path = $_ }
        }
        else {
            $_
        }
    })
}
else {
    try {
        Get-GitChanges -Base $BaseSha -Head $HeadSha
    }
    catch {
        $ForceFull = $true
        Write-Warning $_.Exception.Message
        @([pscustomobject]@{ Status = 'M'; OldPath = $null; Path = 'tools/Ci/Resolve-CiImpact.ps1' })
    }
}

$plan = New-PocokCiPlan `
    -Changes $changes `
    -RepositoryRoot $repositoryRoot `
    -EventName $EventName `
    -ForceFull:$ForceFull `
    -BaseSha $BaseSha `
    -HeadSha $HeadSha

Write-PocokJson -InputObject $plan -Path $resolvedOutput

$runValidation = ($plan.mode -ne 'DocumentationOnly').ToString().ToLowerInvariant()
$runCoverage = (($plan.mode -ne 'DocumentationOnly') -and $plan.coverageSlices.Count -gt 0).ToString().ToLowerInvariant()

function Write-List {
    param([string]$Title, [object[]]$Values)
    Add-Content -LiteralPath $summaryPath -Value "### $Title"
    if ($Values.Count -eq 0) {
        Add-Content -LiteralPath $summaryPath -Value '- None'
    }
    else {
        foreach ($value in $Values) { Add-Content -LiteralPath $summaryPath -Value "- ``$value``" }
    }
    Add-Content -LiteralPath $summaryPath -Value ''
}

$summaryPath = if ($env:GITHUB_STEP_SUMMARY) { $env:GITHUB_STEP_SUMMARY } else { Join-Path (Split-Path -Parent $resolvedOutput) 'impact-summary.md' }
New-Item -ItemType Directory -Path (Split-Path -Parent $summaryPath) -Force | Out-Null
Add-Content -LiteralPath $summaryPath -Value '# CI impact plan'
Add-Content -LiteralPath $summaryPath -Value ''
Add-Content -LiteralPath $summaryPath -Value "- Mode: **$($plan.mode)**"
Add-Content -LiteralPath $summaryPath -Value "- Base: ``$($plan.baseSha)``"
Add-Content -LiteralPath $summaryPath -Value "- Head: ``$($plan.headSha)``"
Add-Content -LiteralPath $summaryPath -Value "- Pack: **$($plan.runPack)**"
Add-Content -LiteralPath $summaryPath -Value "- Public audit: **$($plan.runPublicAudit)**"
Add-Content -LiteralPath $summaryPath -Value "- Public release validation: **$runValidation**"
Add-Content -LiteralPath $summaryPath -Value ''
Write-List -Title 'Reasons' -Values @($plan.reasons)
Write-List -Title 'Changed files' -Values @($plan.changedFiles)
Write-List -Title 'Affected packages' -Values @($plan.affectedPackageIds)
Write-List -Title 'Selected tests' -Values @($plan.affectedTestProjects)
Write-List -Title 'Selected samples' -Values @($plan.affectedSampleProjects)
Write-List -Title 'Packages to pack' -Values @($plan.packageIdsToPack)

if ($GitHubOutput) {
    Add-Content -LiteralPath $GitHubOutput -Value "mode=$($plan.mode)"
    Add-Content -LiteralPath $GitHubOutput -Value "run-linux=$runValidation"
    Add-Content -LiteralPath $GitHubOutput -Value "run-windows=$runValidation"
    Add-Content -LiteralPath $GitHubOutput -Value "run-public-release=$runValidation"
    Add-Content -LiteralPath $GitHubOutput -Value "run-coverage=$runCoverage"
    Add-Content -LiteralPath $GitHubOutput -Value "plan-path=$OutputPath"
}

Write-Host "CI impact mode: $($plan.mode)"
Write-Host "Plan written to: $resolvedOutput"
