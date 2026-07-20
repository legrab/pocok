[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$OutputPath,
    [string]$CatalogPath = 'eng/packages.json'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module (Join-Path $PSScriptRoot 'Pocok.GlobalRelease.psm1') -Force
$null = ConvertFrom-PocokGlobalTag -Tag "GLOBAL-v$Version"
$graph = @(Get-PocokGlobalReleaseGraph -CatalogPath $CatalogPath)
$root = Get-PocokRepositoryRoot
$resolved = [System.IO.Path]::GetFullPath((Join-Path $root $OutputPath))
New-Item -ItemType Directory -Path (Split-Path -Parent $resolved) -Force | Out-Null
$settings = [System.Xml.XmlWriterSettings]::new()
$settings.Indent = $true
$settings.Encoding = [System.Text.UTF8Encoding]::new($false)
$writer = [System.Xml.XmlWriter]::Create($resolved, $settings)
try {
    $writer.WriteStartDocument(); $writer.WriteStartElement('Project'); $writer.WriteStartElement('PropertyGroup')
    foreach ($package in $graph) { $writer.WriteElementString($package.versionProperty, $Version) }
    $writer.WriteEndElement(); $writer.WriteEndElement(); $writer.WriteEndDocument()
} finally { $writer.Dispose() }
Write-Host "Wrote synchronized release versions for $($graph.Count) packages to $resolved."
