# SPDX-License-Identifier: Apache-2.0
# Copyright 2026 Pocok contributors

[CmdletBinding()]
param(
    [string]$PlanPath = 'artifacts/ci/impact.json',
    [string]$HeadRoot = 'artifacts/coverage/head',
    [string]$BaseRoot = 'artifacts/coverage/base',
    [string]$OutputJson = 'artifacts/coverage/coverage-summary.json',
    [string]$OutputMarkdown = 'artifacts/coverage/coverage-summary.md',
    [double]$FailureThreshold = [double]::NaN
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module (Join-Path $PSScriptRoot 'Pocok.Ci.psm1') -Force

$repositoryRoot = Get-PocokRepositoryRoot -StartPath $PSScriptRoot
$plan = Get-Content -LiteralPath (Resolve-PocokPath -RepositoryRoot $repositoryRoot -Path $PlanPath) -Raw | ConvertFrom-Json
$headRootPath = Resolve-PocokPath -RepositoryRoot $repositoryRoot -Path $HeadRoot
$baseRootPath = Resolve-PocokPath -RepositoryRoot $repositoryRoot -Path $BaseRoot
$outputJsonPath = Resolve-PocokPath -RepositoryRoot $repositoryRoot -Path $OutputJson
$outputMarkdownPath = Resolve-PocokPath -RepositoryRoot $repositoryRoot -Path $OutputMarkdown

function Get-SafeProjectName {
    param([string]$ProjectPath)
    return ([System.IO.Path]::GetFileNameWithoutExtension($ProjectPath) -replace '[^A-Za-z0-9.-]', '-')
}

function ConvertTo-RepositoryCoveragePath {
    param([string]$FileName, [string]$SourceRoot)

    $path = $FileName -replace '\\', '/'
    if ([System.IO.Path]::IsPathRooted($FileName)) {
        try {
            $path = ConvertTo-PocokPath -Path $FileName -RepositoryRoot $repositoryRoot
        }
        catch {
            $path = $path.TrimStart('/')
        }
    }
    $sourceIndex = $path.IndexOf($SourceRoot, [StringComparison]::OrdinalIgnoreCase)
    if ($sourceIndex -ge 0) {
        return $path.Substring($sourceIndex)
    }
    return $path.TrimStart([char[]]'./')
}

function Get-CoverageMetrics {
    param(
        [Parameter(Mandatory)] [string]$CoverageRoot,
        [Parameter(Mandatory)] $Slice,
        [switch]$AllowUnavailable
    )

    if (-not (Test-Path -LiteralPath $CoverageRoot -PathType Container)) {
        if ($AllowUnavailable) { return $null }
        throw "Coverage root does not exist: $CoverageRoot"
    }

    $reportFiles = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
    foreach ($testProject in @($Slice.testProjects)) {
        $testRoot = Join-Path $CoverageRoot (Get-SafeProjectName -ProjectPath ([string]$testProject))
        if (Test-Path -LiteralPath $testRoot -PathType Container) {
            foreach ($report in Get-ChildItem -LiteralPath $testRoot -Recurse -File -Filter 'coverage.cobertura.xml') {
                $reportFiles.Add($report)
            }
        }
    }
    if ($reportFiles.Count -eq 0) {
        if ($AllowUnavailable) { return $null }
        throw "No Cobertura report was produced for $($Slice.packageId)."
    }

    $lines = @{}
    $branches = @{}
    $branchDataReliable = $true
    foreach ($report in $reportFiles) {
        try {
            [xml]$document = Get-Content -LiteralPath $report.FullName -Raw
        }
        catch {
            throw "Malformed Cobertura report '$($report.FullName)': $($_.Exception.Message)"
        }
        $classNodes = $document.SelectNodes('//*[local-name()="class"]')
        foreach ($classNode in $classNodes) {
            $fileName = [string]$classNode.GetAttribute('filename')
            if ([string]::IsNullOrWhiteSpace($fileName)) { continue }
            $normalizedFile = ConvertTo-RepositoryCoveragePath -FileName $fileName -SourceRoot ([string]$Slice.sourceRoot)
            if (-not ($normalizedFile -eq [string]$Slice.sourceRoot -or $normalizedFile.StartsWith("$($Slice.sourceRoot)/", [StringComparison]::OrdinalIgnoreCase))) {
                continue
            }
            foreach ($lineNode in $classNode.SelectNodes('.//*[local-name()="line"]')) {
                $lineNumber = [string]$lineNode.GetAttribute('number')
                if ([string]::IsNullOrWhiteSpace($lineNumber)) { continue }
                $lineKey = "$normalizedFile|$lineNumber"
                $covered = [int64]([string]$lineNode.GetAttribute('hits')) -gt 0
                if (-not $lines.ContainsKey($lineKey)) { $lines[$lineKey] = $false }
                $lines[$lineKey] = [bool]$lines[$lineKey] -or $covered

                if ([string]$lineNode.GetAttribute('branch') -ieq 'true') {
                    $conditions = @($lineNode.SelectNodes('./*[local-name()="conditions"]/*[local-name()="condition"]'))
                    if ($conditions.Count -eq 0) {
                        $branchDataReliable = $false
                        continue
                    }
                    foreach ($condition in $conditions) {
                        $number = [string]$condition.GetAttribute('number')
                        $type = [string]$condition.GetAttribute('type')
                        $coverageText = [string]$condition.GetAttribute('coverage')
                        if ([string]::IsNullOrWhiteSpace($number) -or [string]::IsNullOrWhiteSpace($type) -or $coverageText -notmatch '^(?<value>\d+(?:\.\d+)?)%$') {
                            $branchDataReliable = $false
                            continue
                        }
                        $branchKey = "$lineKey|$number|$type"
                        $conditionCovered = [double]$Matches['value'] -gt 0
                        if (-not $branches.ContainsKey($branchKey)) { $branches[$branchKey] = $false }
                        $branches[$branchKey] = [bool]$branches[$branchKey] -or $conditionCovered
                    }
                }
            }
        }
    }

    if ($lines.Count -eq 0) {
        if ($AllowUnavailable) { return $null }
        throw "Coverage reports for $($Slice.packageId) contained no files under $($Slice.sourceRoot)."
    }

    $coveredLines = @($lines.Values | Where-Object { $_ }).Count
    $linePercent = [Math]::Round(($coveredLines * 100.0) / $lines.Count, 2)
    $branchTotal = if ($branchDataReliable) { $branches.Count } else { $null }
    $coveredBranches = if ($branchDataReliable) { @($branches.Values | Where-Object { $_ }).Count } else { $null }
    $branchPercent = if ($branchDataReliable -and $branches.Count -gt 0) {
        [Math]::Round(($coveredBranches * 100.0) / $branches.Count, 2)
    }
    else {
        $null
    }

    return [pscustomobject]@{
        linesCovered = $coveredLines
        linesTotal = $lines.Count
        linePercent = $linePercent
        branchesCovered = $coveredBranches
        branchesTotal = $branchTotal
        branchPercent = $branchPercent
        reports = @($reportFiles.FullName | ForEach-Object { ConvertTo-PocokPath -Path $_ -RepositoryRoot $repositoryRoot } | Sort-Object)
    }
}

function Format-Percent {
    param($Value)
    if ($null -eq $Value) { return 'N/A' }
    return ('{0:N2}%' -f [double]$Value)
}

function Format-Delta {
    param($Value)
    if ($null -eq $Value) { return 'N/A' }
    return ('{0:+0.00;-0.00;0.00} pp' -f [double]$Value)
}

$rows = [System.Collections.Generic.List[object]]::new()
foreach ($slice in @($plan.coverageSlices | Sort-Object packageId)) {
    $head = Get-CoverageMetrics -CoverageRoot $headRootPath -Slice $slice
    $base = Get-CoverageMetrics -CoverageRoot $baseRootPath -Slice $slice -AllowUnavailable
    $lineDelta = if ($null -eq $base) { $null } else { [Math]::Round($head.linePercent - $base.linePercent, 2) }
    $branchDelta = if ($null -eq $base -or $null -eq $head.branchPercent -or $null -eq $base.branchPercent) {
        $null
    }
    else {
        [Math]::Round($head.branchPercent - $base.branchPercent, 2)
    }
    if ($null -ne $lineDelta -and $lineDelta -lt 0) {
        Write-Output "::warning title=Coverage regression::$($slice.packageId) line coverage changed by $(Format-Delta $lineDelta)."
    }
    if (-not [double]::IsNaN($FailureThreshold) -and $null -ne $lineDelta -and $lineDelta -lt -$FailureThreshold) {
        throw "$($slice.packageId) line coverage regression $lineDelta pp exceeds threshold $FailureThreshold pp."
    }
    $rows.Add([pscustomobject]@{
        slice = [string]$slice.packageId
        head = $head
        base = $base
        lineDeltaPercentagePoints = $lineDelta
        branchDeltaPercentagePoints = $branchDelta
    })
}

$summary = [pscustomobject]@{
    schemaVersion = 1
    baseSha = [string]$plan.baseSha
    headSha = [string]$plan.headSha
    generatedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    slices = @($rows)
}
Write-PocokJson -InputObject $summary -Path $outputJsonPath

$markdown = [System.Collections.Generic.List[string]]::new()
$markdown.Add('# Per-slice coverage')
$markdown.Add('')
$markdown.Add('| Slice | Head lines | Base lines | Delta | Head branches | Base branches | Delta |')
$markdown.Add('|---|---:|---:|---:|---:|---:|---:|')
foreach ($row in $rows) {
    $baseLinePercent = if ($null -eq $row.base) { $null } else { $row.base.linePercent }
    $baseBranchPercent = if ($null -eq $row.base) { $null } else { $row.base.branchPercent }
    $markdown.Add("| $($row.slice) | $(Format-Percent $row.head.linePercent) | $(Format-Percent $baseLinePercent) | $(Format-Delta $row.lineDeltaPercentagePoints) | $(Format-Percent $row.head.branchPercent) | $(Format-Percent $baseBranchPercent) | $(Format-Delta $row.branchDeltaPercentagePoints) |")
}
$markdown.Add('')
$markdown.Add('Coverage changes are advisory. Line coverage is authoritative; branch coverage is shown only when condition identities can be merged safely.')
New-Item -ItemType Directory -Path (Split-Path -Parent $outputMarkdownPath) -Force | Out-Null
[System.IO.File]::WriteAllLines($outputMarkdownPath, $markdown, [System.Text.UTF8Encoding]::new($false))
if ($env:GITHUB_STEP_SUMMARY) {
    Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value (Get-Content -LiteralPath $outputMarkdownPath -Raw)
}
Get-Content -LiteralPath $outputMarkdownPath
