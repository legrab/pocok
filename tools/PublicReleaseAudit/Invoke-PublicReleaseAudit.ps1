[CmdletBinding()]
param(
    [string]$PackageDirectory = 'artifacts/packages'
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$requiredFiles = @('LICENSE', 'NOTICE', 'README.md', 'STEWARDSHIP.md', 'SECURITY.md')
$catalog = Get-Content -LiteralPath (Join-Path $repositoryRoot 'eng/packages.json') -Raw | ConvertFrom-Json
$activePackages = @($catalog.packages | Where-Object { $_.state -ne 'Retired' })
$packagePolicies = @{}
foreach ($catalogPackage in $activePackages) {
    $packagePolicies[[string]$catalogPackage.id] = $catalogPackage
}


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

            $nuspecEntry = $archive.Entries | Where-Object { $_.FullName -like '*.nuspec' } | Select-Object -First 1
            $reader = [System.IO.StreamReader]::new($nuspecEntry.Open())
            try {
                [xml]$nuspec = $reader.ReadToEnd()
            }
            finally {
                $reader.Dispose()
            }

            $metadata = $nuspec.package.metadata
            if ($metadata.license.type -ne 'expression' -or $metadata.license.'#text' -ne 'Apache-2.0') {
                throw "$($package.Name) does not declare Apache-2.0 as a license expression."
            }

            if ($metadata.repository.url -ne 'https://github.com/legrab/pocok') {
                throw "$($package.Name) does not declare the canonical repository URL."
            }

            if ($metadata.readme -ne 'README.md') {
                throw "$($package.Name) does not declare its packaged README."
            }

            $packageId = [string]$metadata.id
            $packageVersion = [string]$metadata.version
            if (-not $packagePolicies.ContainsKey($packageId)) {
                throw "$($package.Name) has no active package catalog policy."
            }
            $packagePolicy = $packagePolicies[$packageId]

            foreach ($requiredLibraryEntry in @("lib/net10.0/$packageId.dll", "lib/net10.0/$packageId.xml")) {
                if ($entryNames -notcontains $requiredLibraryEntry) {
                    throw "$($package.Name) does not contain $requiredLibraryEntry."
                }
            }

            $dependencies = @(@($metadata.dependencies.group.dependency) + @($metadata.dependencies.dependency) |
                Where-Object { $null -ne $_ } |
                Sort-Object { [string]$_.id })
            $dependencyIds = @($dependencies | ForEach-Object { [string]$_.id })
            $expectedDependencies = @(@($packagePolicy.internalDependencies) + @($packagePolicy.allowedExternalDependencies)) | Sort-Object

            if (($dependencyIds -join "`n") -ne ($expectedDependencies -join "`n")) {
                throw "$($package.Name) dependencies differ from its catalog allowlist. Actual: $($dependencyIds -join ', ')"
            }

            foreach ($dependency in $dependencies) {
                $dependencyId = [string]$dependency.id
                $dependencyVersion = [string]$dependency.version
                if ([string]::IsNullOrWhiteSpace($dependencyVersion)) {
                    throw "$($package.Name) has an unversioned dependency on $dependencyId."
                }
            }

            $symbolsPath = Join-Path $resolvedPackageDirectory "$packageId.$packageVersion.snupkg"
            if (-not (Test-Path -LiteralPath $symbolsPath -PathType Leaf)) {
                throw "$($package.Name) has no matching symbols package."
            }

            $symbolsArchive = [System.IO.Compression.ZipFile]::OpenRead($symbolsPath)
            try {
                if ($symbolsArchive.Entries.FullName -notcontains "lib/net10.0/$packageId.pdb") {
                    throw "$([System.IO.Path]::GetFileName($symbolsPath)) does not contain the portable PDB."
                }
            }
            finally {
                $symbolsArchive.Dispose()
            }
        }
        finally {
            $archive.Dispose()
        }
    }
}

Write-Host 'Public release audit passed.'
