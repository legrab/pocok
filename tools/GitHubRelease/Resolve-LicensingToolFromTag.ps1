# SPDX-License-Identifier: Apache-2.0
# Copyright 2026 Pocok contributors

[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$Tag,
    [string]$GitHubOutput
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$tools = @(
    [pscustomobject]@{
        Id = 'Pocok.Licensing.Keygen'
        Project = 'src/Licensing.Keygen/Pocok.Licensing.Keygen.csproj'
        TagPrefix = 'licensing.keygen-v'
        Executable = 'Pocok.Licensing.Keygen'
        Title = 'Pocok Licensing Keygen'
    },
    [pscustomobject]@{
        Id = 'Pocok.Licensing.LicenseChecker'
        Project = 'src/Licensing.LicenseChecker/Pocok.Licensing.LicenseChecker.csproj'
        TagPrefix = 'licensing.licensechecker-v'
        Executable = 'Pocok.Licensing.LicenseChecker'
        Title = 'Pocok Licensing License Checker'
    }
)

$matches = @($tools | Where-Object { $Tag.StartsWith($_.TagPrefix, [StringComparison]::Ordinal) })
if ($matches.Count -ne 1) {
    throw "Tag '$Tag' matched $($matches.Count) licensing tool definitions."
}

$tool = $matches[0]
$version = $Tag.Substring($tool.TagPrefix.Length)
if ($version -notmatch '^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$') {
    throw "Tag '$Tag' does not contain a valid semantic version after '$($tool.TagPrefix)'."
}

$output = [ordered]@{
    'tool-id' = $tool.Id
    'project' = $tool.Project
    'version' = $version
    'tag-prefix' = $tool.TagPrefix
    'executable' = $tool.Executable
    'title' = $tool.Title
}

if ($GitHubOutput) {
    foreach ($entry in $output.GetEnumerator()) {
        Add-Content -LiteralPath $GitHubOutput -Value "$($entry.Key)=$($entry.Value)"
    }
}
else {
    [pscustomobject]$output
}
