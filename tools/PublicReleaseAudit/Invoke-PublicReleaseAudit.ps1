[CmdletBinding()]
param(
    [string]$PackageDirectory = 'artifacts/packages',
    [string[]]$PackageIds = @()
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$requiredFiles = @('LICENSE', 'NOTICE', 'README.md', 'STEWARDSHIP.md', 'SECURITY.md')
$catalogPath = Join-Path $repositoryRoot 'eng/packages.json'
$catalog = Get-Content -LiteralPath $catalogPath -Raw | ConvertFrom-Json
$closureResolver = Join-Path $repositoryRoot 'tools/PackageCatalog/Resolve-PackageClosure.ps1'
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

$expectedPackageIds = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
if ($PackageIds.Count -eq 0) {
    foreach ($package in $activePackages) {
        $expectedPackageIds.Add([string]$package.id) | Out-Null
    }
}
else {
    foreach ($packageId in $PackageIds) {
        foreach ($closureId in @(& $closureResolver -CandidatePackageId $packageId | ForEach-Object { [string]$_.id })) {
            $expectedPackageIds.Add($closureId) | Out-Null
        }
    }
}

$resolvedPackageDirectory = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $PackageDirectory))
if (-not (Test-Path -LiteralPath $resolvedPackageDirectory -PathType Container)) {
    throw "Package directory does not exist: $resolvedPackageDirectory"
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$artifacts = @(Get-ChildItem -LiteralPath $resolvedPackageDirectory -File -Filter '*.nupkg' |
    Where-Object { $_.Name -notlike '*.snupkg' })
$artifactIds = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$artifactVersions = @{}

foreach ($package in $artifacts) {
    $matchingId = @($expectedPackageIds | Where-Object { $package.Name.StartsWith("$_.", [StringComparison]::Ordinal) }) |
        Sort-Object Length -Descending |
        Select-Object -First 1
    if (-not $matchingId) {
        throw "Unexpected package artifact: $($package.Name)"
    }

    $escapedId = [regex]::Escape($matchingId)
    $match = [regex]::Match($package.Name, "^$escapedId\.(?<version>.+)\.nupkg$")
    if (-not $match.Success) {
        throw "Package file name does not match its expected ID: $($package.Name)"
    }

    if (-not $artifactIds.Add($matchingId)) {
        throw "More than one package artifact was produced for $matchingId."
    }
    $artifactVersions[$matchingId] = $match.Groups['version'].Value
}

$missingArtifacts = @($expectedPackageIds | Where-Object { -not $artifactIds.Contains($_) })
if ($missingArtifacts.Count -gt 0) {
    throw "Expected package artifacts are missing: $($missingArtifacts -join ', ')"
}

foreach ($package in $artifacts) {
    $archive = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)
    try {
        $entryNames = @($archive.Entries.FullName)
        foreach ($requiredEntry in @('LICENSE', 'NOTICE', 'README.md')) {
            if ($entryNames -notcontains $requiredEntry) {
                throw "$($package.Name) does not contain $requiredEntry."
            }
        }

        $prohibitedEntries = @($entryNames | Where-Object {
            $_ -match '^(docs|tests|tools|sessions|prompts)/' -or
            $_ -in @('origin.zip', 'build.log') -or
            $_ -like '.git/*'
        })
        if ($prohibitedEntries.Count -gt 0) {
            throw "$($package.Name) contains repository-only entries: $($prohibitedEntries -join ', ')"
        }

        $nuspecEntries = @($archive.Entries | Where-Object { $_.FullName -like '*.nuspec' })
        if ($nuspecEntries.Count -ne 1) {
            throw "$($package.Name) must contain exactly one nuspec."
        }

        $reader = [System.IO.StreamReader]::new($nuspecEntries[0].Open())
        try {
            [xml]$nuspec = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        $metadata = $nuspec.package.metadata
        $packageId = [string]$metadata.id
        $packageVersion = [string]$metadata.version
        if (-not $packagePolicies.ContainsKey($packageId) -or -not $expectedPackageIds.Contains($packageId)) {
            throw "$($package.Name) has no expected active package catalog policy."
        }
        if ($package.Name -ne "$packageId.$packageVersion.nupkg") {
            throw "$($package.Name) does not match nuspec identity $packageId $packageVersion."
        }

        if ($metadata.license.type -ne 'expression' -or $metadata.license.'#text' -ne 'Apache-2.0') {
            throw "$($package.Name) does not declare Apache-2.0 as a license expression."
        }
        if ($metadata.projectUrl -ne 'https://github.com/legrab/pocok') {
            throw "$($package.Name) does not declare the canonical project URL."
        }
        if ($metadata.repository.url -ne 'https://github.com/legrab/pocok' -or $metadata.repository.type -ne 'git') {
            throw "$($package.Name) does not declare the canonical git repository."
        }
        if ([string]::IsNullOrWhiteSpace([string]$metadata.repository.commit)) {
            throw "$($package.Name) does not record the source commit."
        }
        if ($metadata.readme -ne 'README.md') {
            throw "$($package.Name) does not declare its packaged README."
        }

        foreach ($requiredLibraryEntry in @("lib/net10.0/$packageId.dll", "lib/net10.0/$packageId.xml")) {
            if ($entryNames -notcontains $requiredLibraryEntry) {
                throw "$($package.Name) does not contain $requiredLibraryEntry."
            }
        }

        $readmeEntry = $archive.Entries | Where-Object FullName -eq 'README.md' | Select-Object -First 1
        $readmeReader = [System.IO.StreamReader]::new($readmeEntry.Open())
        try {
            $readme = $readmeReader.ReadToEnd()
        }
        finally {
            $readmeReader.Dispose()
        }
        foreach ($link in [regex]::Matches($readme, '!?\[[^\]]*\]\((?<target>[^)]+)\)')) {
            $target = $link.Groups['target'].Value.Trim()
            if ($target -and
                -not $target.StartsWith('#', [StringComparison]::Ordinal) -and
                -not $target.StartsWith('https://', [StringComparison]::OrdinalIgnoreCase) -and
                -not $target.StartsWith('http://', [StringComparison]::OrdinalIgnoreCase) -and
                -not $target.StartsWith('mailto:', [StringComparison]::OrdinalIgnoreCase)) {
                throw "$($package.Name) README contains a package-external relative link: $target"
            }
        }

        $packagePolicy = $packagePolicies[$packageId]
        $dependencies = @(@($metadata.dependencies.group.dependency) + @($metadata.dependencies.dependency) |
            Where-Object { $null -ne $_ } |
            Sort-Object { [string]$_.id })
        $dependencyIds = @($dependencies | ForEach-Object { [string]$_.id })
        $expectedDependencies = @(@($packagePolicy.internalDependencies) + @($packagePolicy.allowedExternalDependencies)) |
            Sort-Object

        if (($dependencyIds -join "`n") -ne ($expectedDependencies -join "`n")) {
            throw "$($package.Name) dependencies differ from its catalog allowlist. Actual: $($dependencyIds -join ', ')"
        }

        foreach ($dependency in $dependencies) {
            $dependencyId = [string]$dependency.id
            $dependencyVersion = [string]$dependency.version
            if ([string]::IsNullOrWhiteSpace($dependencyVersion) -or $dependencyVersion.Contains('*')) {
                throw "$($package.Name) has an invalid dependency version for $dependencyId: '$dependencyVersion'."
            }

            if ($artifactVersions.ContainsKey($dependencyId)) {
                $expectedVersion = [string]$artifactVersions[$dependencyId]
                if (-not $dependencyVersion.Contains($expectedVersion, [StringComparison]::Ordinal)) {
                    throw "$($package.Name) does not reference local dependency $dependencyId version $expectedVersion. Actual: $dependencyVersion"
                }
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

Write-Host "Public release audit passed for: $($expectedPackageIds -join ', ')"
