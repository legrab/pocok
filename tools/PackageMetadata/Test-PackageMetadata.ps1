[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module (Join-Path $PSScriptRoot 'Pocok.PackageMetadata.psm1') -Force

function Assert-Equal($Expected, $Actual, [string]$Message) {
    if ($Expected -ne $Actual) { throw "$Message Expected '$Expected', actual '$Actual'." }
}

$cases = @(
    @{ Name = 'none'; Xml = '<package><metadata><id>A</id><version>1.0.0</version></metadata></package>'; Count = 0 },
    @{ Name = 'direct'; Xml = '<package><metadata><id>A</id><version>1.0.0</version><dependencies><dependency id="B" version="[1.0.0]" /></dependencies></metadata></package>'; Count = 1 },
    @{ Name = 'grouped'; Xml = '<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"><metadata><id>A</id><version>1.0.0</version><dependencies><group targetFramework="net10.0"><dependency id="B" version="[1.0.0]" /></group><group targetFramework="net9.0"><dependency id="C" version="[2.0.0]" /></group></dependencies></metadata></package>'; Count = 2 }
)

foreach ($case in $cases) {
    try {
        [xml]$xml = $case.Xml
        $result = Get-PocokNuspecMetadata -Nuspec $xml

        Assert-Equal 'A' $result.Id "$($case.Name) id mismatch."
        Assert-Equal '1.0.0' $result.Version "$($case.Name) version mismatch."
        Assert-Equal $case.Count $result.Dependencies.Count "$($case.Name) dependency count mismatch."
    }
    catch {
        throw "Package metadata test case '$($case.Name)' failed: $($_.Exception.Message)"
    }
}

[xml]$grouped = $cases[2].Xml
$parsed = Get-PocokNuspecMetadata -Nuspec $grouped
Assert-Equal 'net10.0' $parsed.Dependencies[0].TargetFramework 'Grouped dependency framework mismatch.'
Assert-Equal 'net9.0' $parsed.Dependencies[1].TargetFramework 'Grouped dependency framework mismatch.'

Write-Host 'Package metadata tooling tests passed.'
