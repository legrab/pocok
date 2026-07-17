# SPDX-License-Identifier: Apache-2.0
# Copyright 2026 Pocok contributors

[CmdletBinding()]
param(
    [string]$PlanPath = 'artifacts/ci/impact.json',
    [string]$HeadRoot = 'artifacts/coverage/head',
    [string]$BaseRoot = 'artifacts/coverage/base',
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module (Join-Path $PSScriptRoot 'Pocok.Ci.psm1') -Force

$repositoryRoot = Get-PocokRepositoryRoot -StartPath $PSScriptRoot
$resolvedPlan = Resolve-PocokPath -RepositoryRoot $repositoryRoot -Path $PlanPath
$plan = Get-Content -LiteralPath $resolvedPlan -Raw | ConvertFrom-Json
$baseRootPath = Resolve-PocokPath -RepositoryRoot $repositoryRoot -Path $BaseRoot
$worktreePath = Resolve-PocokPath -RepositoryRoot $repositoryRoot -Path 'artifacts/coverage/base-worktree'
Remove-Item -LiteralPath $baseRootPath -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $baseRootPath -Force | Out-Null

function Get-SafeProjectName {
    param([string]$ProjectPath)
    return ([System.IO.Path]::GetFileNameWithoutExtension($ProjectPath) -replace '[^A-Za-z0-9.-]', '-')
}

function Invoke-BaseDotNet {
    param([string[]]$Arguments)
    Invoke-PocokCommand -FilePath 'dotnet' -Arguments $Arguments -WorkingDirectory $worktreePath
}

function New-BaseSliceRunSettings {
    param(
        [Parameter(Mandatory)] [string]$AssemblyName,
        [Parameter(Mandatory)] [string]$DestinationPath
    )

    $templatePath = Join-Path $worktreePath 'eng/coverage.runsettings'
    $content = Get-Content -LiteralPath $templatePath -Raw
    $replacement = "<Include>[$AssemblyName]*</Include>"
    $content = [regex]::Replace($content, '<Include>.*?</Include>', $replacement, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    [System.IO.File]::WriteAllText($DestinationPath, $content, [System.Text.UTF8Encoding]::new($false))
}

$baseAvailable = -not [string]::IsNullOrWhiteSpace([string]$plan.baseSha)
if ($baseAvailable) {
    & git -C $repositoryRoot cat-file -e "$($plan.baseSha)^{commit}" 2>$null
    $baseAvailable = $LASTEXITCODE -eq 0
}

try {
    if ($baseAvailable) {
        & git -C $repositoryRoot worktree prune
        Remove-Item -LiteralPath $worktreePath -Recurse -Force -ErrorAction SilentlyContinue
        & git -C $repositoryRoot worktree add --detach $worktreePath ([string]$plan.baseSha)
        if ($LASTEXITCODE -ne 0) { throw "Could not create base worktree for $($plan.baseSha)." }

        $collectorReady = (Test-Path -LiteralPath (Join-Path $worktreePath 'eng/coverage.runsettings') -PathType Leaf) -and
            (Test-Path -LiteralPath (Join-Path $worktreePath 'tests/Directory.Build.props') -PathType Leaf)
        if (-not $collectorReady) {
            Write-Warning 'The base commit predates repository coverage tooling. Base coverage will be reported as N/A.'
        }
        else {
            foreach ($slice in @($plan.coverageSlices | Sort-Object packageId)) {
                $sourceProjectPath = Join-Path $worktreePath (([string]$slice.sourceProject) -replace '/', [System.IO.Path]::DirectorySeparatorChar)
                $testProjects = @($slice.testProjects | ForEach-Object { [string]$_ })
                $allProjectsExist = (Test-Path -LiteralPath $sourceProjectPath -PathType Leaf) -and
                    @($testProjects | Where-Object {
                        -not (Test-Path -LiteralPath (Join-Path $worktreePath ($_ -replace '/', [System.IO.Path]::DirectorySeparatorChar)) -PathType Leaf)
                    }).Count -eq 0
                if (-not $allProjectsExist) {
                    Write-Warning "Base coverage is unavailable for $($slice.packageId) because the slice or an owning test project did not exist."
                    continue
                }

                $sliceSucceeded = $true
                foreach ($testProject in $testProjects) {
                    $safeName = Get-SafeProjectName -ProjectPath $testProject
                    $resultDirectory = Join-Path $baseRootPath $safeName
                    Remove-Item -LiteralPath $resultDirectory -Recurse -Force -ErrorAction SilentlyContinue
                    New-Item -ItemType Directory -Path $resultDirectory -Force | Out-Null
                    try {
                        Invoke-BaseDotNet -Arguments @('restore', $testProject, '--locked-mode')
                        Invoke-BaseDotNet -Arguments @('build', $testProject, '--configuration', $Configuration, '--no-restore')
                        $sliceSettings = Join-Path $resultDirectory 'coverage.runsettings'
                        New-BaseSliceRunSettings -AssemblyName ([string]$slice.assemblyName) -DestinationPath $sliceSettings
                        Invoke-BaseDotNet -Arguments @(
                            'test', $testProject,
                            '--configuration', $Configuration,
                            '--no-build',
                            '--logger', "trx;LogFileName=$safeName-base.trx",
                            '--results-directory', $resultDirectory,
                            '--collect:XPlat Code Coverage',
                            '--settings', $sliceSettings
                        )
                    }
                    catch {
                        Write-Warning "Base coverage is unavailable for $($slice.packageId): $($_.Exception.Message)"
                        $sliceSucceeded = $false
                        break
                    }
                }
                if (-not $sliceSucceeded) {
                    foreach ($testProject in $testProjects) {
                        Remove-Item -LiteralPath (Join-Path $baseRootPath (Get-SafeProjectName -ProjectPath $testProject)) -Recurse -Force -ErrorAction SilentlyContinue
                    }
                }
            }
        }
    }
    else {
        Write-Warning 'The exact PR base commit is unavailable. Base coverage will be reported as N/A.'
    }
}
finally {
    if (Test-Path -LiteralPath $worktreePath -PathType Container) {
        & git -C $repositoryRoot worktree remove --force $worktreePath 2>$null
        if ($LASTEXITCODE -ne 0) {
            Remove-Item -LiteralPath $worktreePath -Recurse -Force -ErrorAction SilentlyContinue
            & git -C $repositoryRoot worktree prune 2>$null
        }
    }
}

& (Join-Path $PSScriptRoot 'Write-CoverageSummary.ps1') -PlanPath $PlanPath -HeadRoot $HeadRoot -BaseRoot $BaseRoot
