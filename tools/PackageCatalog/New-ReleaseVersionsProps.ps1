[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$CandidatePackageId,
    [Parameter(Mandatory)] [string]$CandidateVersion,
    [Parameter(Mandatory)] [string]$OutputPath,
    [string]$CatalogPath = 'eng/packages.json'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$catalog = Get-Content -LiteralPath (Join-Path $repositoryRoot $CatalogPath) -Raw | ConvertFrom-Json
$candidate = @($catalog.packages | Where-Object id -eq $CandidatePackageId)
if ($candidate.Count -ne 1 -or -not $candidate[0].releasable) {
    throw "Candidate package '$CandidatePackageId' is unknown or not releasable."
}

$candidateSemanticVersion = $null
if (-not [System.Management.Automation.SemanticVersion]::TryParse($CandidateVersion, [ref]$candidateSemanticVersion)) {
    throw "Candidate version '$CandidateVersion' is not valid SemVer."
}

function Get-LatestTaggedVersion {
    param([Parameter(Mandatory)] $Package)

    $tags = @(& git -C $repositoryRoot tag --list "$($Package.tagPrefix)*")
    if ($LASTEXITCODE -ne 0) {
        throw "Could not read git tags for $($Package.id)."
    }

    $prefix = [string]$Package.tagPrefix
    $versions = foreach ($tag in $tags) {
        $text = $tag.Substring($prefix.Length)
        try {
            $parsed = [System.Management.Automation.SemanticVersion]::Parse($text)
            [pscustomobject]@{ Text = $text; Parsed = $parsed }
        }
        catch {
            Write-Warning "Ignoring malformed release tag '$tag' for $($Package.id)."
        }
    }

    return $versions | Sort-Object Parsed -Descending | Select-Object -First 1
}

$byId = @{}
foreach ($package in $catalog.packages) {
    $byId[[string]$package.id] = $package
}

$requiredDependencies = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$pending = [System.Collections.Generic.Stack[string]]::new()
foreach ($dependency in $candidate[0].internalDependencies) {
    $pending.Push([string]$dependency)
}
while ($pending.Count -gt 0) {
    $id = $pending.Pop()
    if (-not $requiredDependencies.Add($id)) {
        continue
    }

    if (-not $byId.ContainsKey($id)) {
        throw "$CandidatePackageId references unknown internal dependency $id."
    }

    foreach ($dependency in $byId[$id].internalDependencies) {
        $pending.Push([string]$dependency)
    }
}

$properties = [ordered]@{}
foreach ($package in $catalog.packages | Where-Object { $_.state -ne 'Retired' }) {
    $version = $null
    if ($package.id -eq $CandidatePackageId) {
        $version = $CandidateVersion
    }
    else {
        $tagged = Get-LatestTaggedVersion -Package $package
        if ($tagged) {
            $version = $tagged.Text
        }
        elseif ($requiredDependencies.Contains([string]$package.id)) {
            throw "Internal dependency $($package.id) has no release tag. Publish it before $CandidatePackageId."
        }
    }

    if ($version) {
        $properties[[string]$package.versionProperty] = $version
    }
}

$resolvedOutput = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputPath))
New-Item -ItemType Directory -Path (Split-Path -Parent $resolvedOutput) -Force | Out-Null
$settings = [System.Xml.XmlWriterSettings]::new()
$settings.Indent = $true
$settings.Encoding = [System.Text.UTF8Encoding]::new($false)
$writer = [System.Xml.XmlWriter]::Create($resolvedOutput, $settings)
try {
    $writer.WriteStartDocument()
    $writer.WriteStartElement('Project')
    $writer.WriteStartElement('PropertyGroup')
    foreach ($entry in $properties.GetEnumerator()) {
        $writer.WriteElementString($entry.Key, [string]$entry.Value)
    }
    $writer.WriteEndElement()
    $writer.WriteEndElement()
    $writer.WriteEndDocument()
}
finally {
    $writer.Dispose()
}

Write-Host "Wrote release versions to $resolvedOutput"
