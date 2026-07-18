# SPDX-License-Identifier: Apache-2.0
# Copyright 2026 Pocok contributors

[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$Solution = 'Pocok.Core.slnx',
    [string]$ReleaseVersionsFile
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module (Join-Path $PSScriptRoot 'Pocok.Ci.psm1') -Force

$repositoryRoot = Get-PocokRepositoryRoot -StartPath $PSScriptRoot
$solutionPath = Resolve-PocokPath -RepositoryRoot $repositoryRoot -Path $Solution
if (-not (Test-Path -LiteralPath $solutionPath -PathType Leaf)) {
    throw "Solution '$Solution' does not exist."
}

$previousIncludeExperimental = [Environment]::GetEnvironmentVariable('IncludeExperimental', 'Process')
$previousReleaseVersionsFile = [Environment]::GetEnvironmentVariable('PocokReleaseVersionsFile', 'Process')

try {
    $env:IncludeExperimental = 'false'

    if ([string]::IsNullOrWhiteSpace($ReleaseVersionsFile)) {
        Remove-Item Env:PocokReleaseVersionsFile -ErrorAction SilentlyContinue
    }
    else {
        $resolvedReleaseVersionsFile = Resolve-PocokPath -RepositoryRoot $repositoryRoot -Path $ReleaseVersionsFile
        if (-not (Test-Path -LiteralPath $resolvedReleaseVersionsFile -PathType Leaf)) {
            throw "Release versions file '$ReleaseVersionsFile' does not exist."
        }
        $env:PocokReleaseVersionsFile = $resolvedReleaseVersionsFile
    }

    Invoke-PocokCommand -FilePath 'dotnet' -Arguments @(
        'restore', $Solution,
        '--locked-mode'
    ) -WorkingDirectory $repositoryRoot

    Invoke-PocokCommand -FilePath 'dotnet' -Arguments @(
        'format', $Solution,
        '--verify-no-changes',
        '--no-restore'
    ) -WorkingDirectory $repositoryRoot

    Invoke-PocokCommand -FilePath 'dotnet' -Arguments @(
        'build', $Solution,
        '--configuration', $Configuration,
        '--no-restore'
    ) -WorkingDirectory $repositoryRoot

    Invoke-PocokCommand -FilePath 'dotnet' -Arguments @(
        'test', $Solution,
        '--configuration', $Configuration,
        '--no-build'
    ) -WorkingDirectory $repositoryRoot
}
finally {
    if ($null -eq $previousIncludeExperimental) {
        Remove-Item Env:IncludeExperimental -ErrorAction SilentlyContinue
    }
    else {
        $env:IncludeExperimental = $previousIncludeExperimental
    }

    if ($null -eq $previousReleaseVersionsFile) {
        Remove-Item Env:PocokReleaseVersionsFile -ErrorAction SilentlyContinue
    }
    else {
        $env:PocokReleaseVersionsFile = $previousReleaseVersionsFile
    }
}
