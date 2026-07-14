[CmdletBinding()]
param(
    [string]$PackageDirectory = 'artifacts/packages'
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$requiredFiles = @('LICENSE', 'NOTICE', 'README.md', 'STEWARDSHIP.md', 'SECURITY.md')

foreach ($requiredFile in $requiredFiles) {
    $path = Join-Path $repositoryRoot $requiredFile
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required public file is missing: $requiredFile"
    }
}

$sourceRoot = Join-Path $repositoryRoot 'src'
if (Test-Path -LiteralPath $sourceRoot -PathType Container) {
    $expectedHeader = "// SPDX-License-Identifier: Apache-2.0`n// Copyright 2026 Pocok contributors"
    $invalidSources = Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter '*.cs' |
        Where-Object { $_.FullName -notmatch '[\\/]obj[\\/]' -and $_.Name -notlike '*.g.cs' } |
        Where-Object {
            ([System.IO.File]::ReadAllText($_.FullName) -replace "`r`n", "`n").StartsWith($expectedHeader) -eq $false
        }

    if ($invalidSources) {
        throw "Source files without the required header:`n$($invalidSources.FullName -join "`n")"
    }
}

$secretPatterns = @(
    '-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----',
    'gh[pousr]_[A-Za-z0-9_]{30,}',
    'DefaultEndpointsProtocol=https;AccountName='
)

$textFiles = Get-ChildItem -LiteralPath $repositoryRoot -Recurse -File |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj|artifacts|\.git)[\\/]' } |
    Where-Object { $_.FullName -ne $PSCommandPath } |
    Where-Object { $_.Extension -in @('.cs', '.md', '.json', '.xml', '.yml', '.yaml', '.props', '.targets', '.ps1') }

foreach ($pattern in $secretPatterns) {
    $matches = $textFiles | Select-String -Pattern $pattern
    if ($matches) {
        throw "Potential secret pattern found: $pattern"
    }
}

$resolvedPackageDirectory = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $PackageDirectory))
if (Test-Path -LiteralPath $resolvedPackageDirectory -PathType Container) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    foreach ($package in Get-ChildItem -LiteralPath $resolvedPackageDirectory -File -Filter '*.nupkg') {
        $archive = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)
        try {
            $entryNames = $archive.Entries.FullName
            foreach ($requiredEntry in @('LICENSE', 'NOTICE', 'README.md')) {
                if ($entryNames -notcontains $requiredEntry) {
                    throw "$($package.Name) does not contain $requiredEntry."
                }
            }
        }
        finally {
            $archive.Dispose()
        }
    }
}

Write-Host 'Public release audit passed.'
