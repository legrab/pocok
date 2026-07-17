# SPDX-License-Identifier: Apache-2.0
# Copyright 2026 Pocok contributors

[CmdletBinding()]
param(
    [string]$PlanPath = 'artifacts/ci/impact.json',
    [ValidateSet('Linux', 'Windows')]
    [string]$Platform = 'Linux',
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$CoverageRoot = 'artifacts/coverage/head',
    [string]$TestResultsRoot = 'artifacts/test-results'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module (Join-Path $PSScriptRoot 'Pocok.Ci.psm1') -Force

$repositoryRoot = Get-PocokRepositoryRoot -StartPath $PSScriptRoot
$resolvedPlan = Resolve-PocokPath -RepositoryRoot $repositoryRoot -Path $PlanPath
$plan = Get-Content -LiteralPath $resolvedPlan -Raw | ConvertFrom-Json

if ($plan.mode -eq 'DocumentationOnly') {
    Write-Host 'Documentation-only plan has no .NET validation work.'
    return
}

$coverageRootPath = Resolve-PocokPath -RepositoryRoot $repositoryRoot -Path $CoverageRoot
$testResultsRootPath = Resolve-PocokPath -RepositoryRoot $repositoryRoot -Path $TestResultsRoot
New-Item -ItemType Directory -Path $coverageRootPath -Force | Out-Null
New-Item -ItemType Directory -Path $testResultsRootPath -Force | Out-Null

function Invoke-DotNet {
    param([Parameter(Mandatory)] [string[]]$Arguments)
    Invoke-PocokCommand -FilePath 'dotnet' -Arguments $Arguments -WorkingDirectory $repositoryRoot
}

if ($plan.runPackageMetadataTests) {
    Invoke-PocokCommand -FilePath 'pwsh' -Arguments @('-NoProfile', '-File', 'tools/PackageMetadata/Test-PackageMetadata.ps1') -WorkingDirectory $repositoryRoot
    Invoke-PocokCommand -FilePath 'pwsh' -Arguments @('-NoProfile', '-File', 'tools/PackageCatalog/Test-PackageCatalog.ps1') -WorkingDirectory $repositoryRoot
}

$validationProjects = @($plan.validationProjects | ForEach-Object { [string]$_ } | Sort-Object -Unique)
if ($plan.mode -eq 'Full') {
    Invoke-DotNet -Arguments @('restore', 'Pocok.slnx', '--locked-mode')
    Invoke-DotNet -Arguments @('format', 'Pocok.slnx', '--verify-no-changes', '--no-restore')
    Invoke-DotNet -Arguments @('build', 'Pocok.slnx', '--configuration', $Configuration, '--no-restore')
}
else {
    foreach ($project in $validationProjects) {
        Invoke-DotNet -Arguments @('restore', $project, '--locked-mode')
    }
    foreach ($project in $validationProjects) {
        Invoke-DotNet -Arguments @('format', $project, '--verify-no-changes', '--no-restore')
    }
    foreach ($project in $validationProjects) {
        Invoke-DotNet -Arguments @('build', $project, '--configuration', $Configuration, '--no-restore')
    }
}

$coverageAssemblies = @{}
foreach ($slice in @($plan.coverageSlices)) {
    foreach ($testProject in @($slice.testProjects)) {
        $coverageAssemblies[[string]$testProject] = [string]$slice.assemblyName
    }
}

function New-SliceRunSettings {
    param(
        [Parameter(Mandatory)] [string]$AssemblyName,
        [Parameter(Mandatory)] [string]$DestinationPath
    )

    $templatePath = Join-Path $repositoryRoot 'eng/coverage.runsettings'
    $content = Get-Content -LiteralPath $templatePath -Raw
    $replacement = "<Include>[$AssemblyName]*</Include>"
    $content = [regex]::Replace($content, '<Include>.*?</Include>', $replacement, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    [System.IO.File]::WriteAllText($DestinationPath, $content, [System.Text.UTF8Encoding]::new($false))
}

foreach ($testProject in @($plan.affectedTestProjects | ForEach-Object { [string]$_ } | Sort-Object -Unique)) {
    $safeName = ([System.IO.Path]::GetFileNameWithoutExtension($testProject) -replace '[^A-Za-z0-9.-]', '-')
    $resultDirectory = Join-Path $testResultsRootPath $safeName
    if ($Platform -eq 'Linux' -and $coverageAssemblies.ContainsKey($testProject)) {
        $resultDirectory = Join-Path $coverageRootPath $safeName
    }
    Remove-Item -LiteralPath $resultDirectory -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $resultDirectory -Force | Out-Null
    $arguments = @(
        'test', $testProject,
        '--configuration', $Configuration,
        '--no-build',
        '--logger', "trx;LogFileName=$safeName.trx",
        '--results-directory', $resultDirectory
    )
    if ($Platform -eq 'Linux' -and $coverageAssemblies.ContainsKey($testProject)) {
        $sliceSettings = Join-Path $resultDirectory 'coverage.runsettings'
        New-SliceRunSettings -AssemblyName $coverageAssemblies[$testProject] -DestinationPath $sliceSettings
        $arguments += @(
            '--collect:XPlat Code Coverage',
            '--settings', $sliceSettings
        )
    }
    Invoke-DotNet -Arguments $arguments
}

foreach ($sampleProject in @($plan.affectedRunnableSampleProjects | ForEach-Object { [string]$_ } | Sort-Object -Unique)) {
    if ($sampleProject -eq 'samples/ModularCommunicator.Host/Pocok.ModularCommunicator.Host.csproj') {
        Invoke-PocokCommand -FilePath 'pwsh' -Arguments @('-NoProfile', '-File', 'samples/ModularCommunicator/Stage-Plugin.ps1', '-Configuration', $Configuration) -WorkingDirectory $repositoryRoot
        $pluginRoot = Resolve-PocokPath -RepositoryRoot $repositoryRoot -Path "samples/ModularCommunicator.Host/bin/$Configuration/net10.0/plugins"
        Invoke-DotNet -Arguments @('run', '--project', $sampleProject, '--configuration', $Configuration, '--no-build', '--', $pluginRoot)
    }
    else {
        Invoke-DotNet -Arguments @('run', '--project', $sampleProject, '--configuration', $Configuration, '--no-build')
    }
}

if ([bool]$plan.runTrimmedConversion) {
    $trimmedOutput = Resolve-PocokPath -RepositoryRoot $repositoryRoot -Path "artifacts/trimmed-conversion/$Platform"
    Remove-Item -LiteralPath $trimmedOutput -Recurse -Force -ErrorAction SilentlyContinue
    Invoke-DotNet -Arguments @(
        'publish',
        'samples/Conversion.Trimmed/Pocok.Conversion.Trimmed.csproj',
        '--configuration', $Configuration,
        '--output', $trimmedOutput
    )
    Invoke-DotNet -Arguments @((Join-Path $trimmedOutput 'Pocok.Conversion.Trimmed.dll'))
}

if ($Platform -eq 'Linux' -and [bool]$plan.runPack) {
    $packageDirectory = Resolve-PocokPath -RepositoryRoot $repositoryRoot -Path 'artifacts/packages'
    Remove-Item -LiteralPath $packageDirectory -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $packageDirectory -Force | Out-Null
    foreach ($project in @($plan.packageProjectsToPack | ForEach-Object { [string]$_ } | Sort-Object -Unique)) {
        Invoke-DotNet -Arguments @('pack', $project, '--configuration', $Configuration, '--no-build', '--output', $packageDirectory)
    }

    $smokePackageIds = @($plan.affectedSmokePackageIds | ForEach-Object { [string]$_ })
    if ($smokePackageIds.Count -gt 0) {
        & (Join-Path $repositoryRoot 'tools/PackageSmoke/Invoke-PackageSmoke.ps1') -NoPack -PackageIds $smokePackageIds
    }

    if ([bool]$plan.runPublicAudit) {
        $auditPackageIds = @($plan.affectedAuditPackageIds | ForEach-Object { [string]$_ })
        & (Join-Path $repositoryRoot 'tools/PublicReleaseAudit/Invoke-PublicReleaseAudit.ps1') -PackageIds $auditPackageIds
    }
}
