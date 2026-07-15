[CmdletBinding()]
param(
    [ValidateSet('LocalClosure', 'Publication', 'Both')]
    [string]$Mode = 'LocalClosure',
    [switch]$NoPack,
    [string[]]$PackageIds = @(),
    [string]$PackageDirectory = 'artifacts/packages'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$packageDirectoryPath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $PackageDirectory))
$closureFeed = Join-Path $repositoryRoot 'artifacts/package-smoke/closure-feed'
$candidateFeedRoot = Join-Path $repositoryRoot 'artifacts/package-smoke/candidate-feeds'

$consumerSpecs = @{
    'Pocok.AppDefaults.Modularity' = @{ Template = 'AppDefaultsModularityConsumer/Pocok.AppDefaults.Modularity.Consumer.csproj.template'; Program = 'AppDefaultsModularityConsumer/Program.cs' }
    'Pocok.Modularity' = @{ Template = 'ModularityConsumer/Pocok.Modularity.Consumer.csproj.template'; Program = 'ModularityConsumer/Program.cs' }
    'Pocok.Modularity.Contracts' = @{ Template = 'ModularityContractsConsumer/Pocok.Modularity.Contracts.Consumer.csproj.template'; Program = 'ModularityContractsConsumer/Program.cs' }
    'Pocok.AppDefaults.Logging.Serilog' = @{ Template = 'AppDefaultsLoggingSerilogConsumer/Pocok.AppDefaults.Logging.Serilog.Consumer.csproj.template'; Program = 'AppDefaultsLoggingSerilogConsumer/Program.cs' }
    'Pocok.AppDefaults.Logging' = @{ Template = 'AppDefaultsLoggingConsumer/Pocok.AppDefaults.Logging.Consumer.csproj.template'; Program = 'AppDefaultsLoggingConsumer/Program.cs' }
    'Pocok.AppDefaults' = @{ Template = 'AppDefaultsConsumer/Pocok.AppDefaults.Consumer.csproj.template'; Program = 'AppDefaultsConsumer/Program.cs' }
    'Pocok.Conversion' = @{ Template = 'ConversionConsumer/Pocok.Conversion.Consumer.csproj.template'; Program = 'ConversionConsumer/Program.cs' }
    'Pocok.Readiness' = @{ Template = 'ReadinessConsumer/Pocok.Readiness.Consumer.csproj.template'; Program = 'ReadinessConsumer/Program.cs' }
}

function Get-PackageArtifact {
    param(
        [Parameter(Mandatory)] [string]$PackageId,
        [Parameter(Mandatory)] [string]$Directory
    )

    $escapedPackageId = [regex]::Escape($PackageId)
    $matches = @(Get-ChildItem -LiteralPath $Directory -File -Filter "$PackageId.*.nupkg" |
        Where-Object { $_.Name -notlike '*.snupkg' } |
        Where-Object { $_.Name -match "^$escapedPackageId\.(?<version>[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?)\.nupkg$" })

    if ($matches.Count -ne 1) {
        $found = if ($matches.Count -eq 0) { 'none' } else { $matches.Name -join ', ' }
        throw "Expected exactly one $PackageId package in $Directory, found: $found"
    }

    return $matches[0]
}

function Invoke-Consumer {
    param(
        [Parameter(Mandatory)] [string]$PackageId,
        [Parameter(Mandatory)] [System.IO.FileInfo]$Package,
        [Parameter(Mandatory)] [string]$FeedMode,
        [Parameter(Mandatory)] [string[]]$Sources,
        [Parameter(Mandatory)] [string]$WorkRoot
    )

    $escapedPackageId = [regex]::Escape($PackageId)
    $version = [regex]::Match($Package.Name, "^$escapedPackageId\.(?<version>.+)\.nupkg$").Groups['version'].Value
    $consumerRoot = Join-Path $WorkRoot "$PackageId/$FeedMode"
    $packagesRoot = Join-Path $consumerRoot '.packages'
    New-Item -ItemType Directory -Path $consumerRoot -Force | Out-Null

    $spec = $consumerSpecs[$PackageId]
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $spec.Program) -Destination (Join-Path $consumerRoot 'Program.cs')
    $template = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot $spec.Template))
    [System.IO.File]::WriteAllText(
        (Join-Path $consumerRoot 'Pocok.Consumer.csproj'),
        $template.Replace('__PACKAGE_VERSION__', $version),
        [System.Text.UTF8Encoding]::new($false))

    $configContent = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
"@
    for ($i = 0; $i -lt $Sources.Count; $i++) {
        $configContent += "`n    <add key=""source_$i"" value=""$($Sources[$i])"" />"
    }
    $configContent += @"
`n  </packageSources>
</configuration>
"@
    [System.IO.File]::WriteAllText((Join-Path $consumerRoot 'NuGet.Config'), $configContent, [System.Text.UTF8Encoding]::new($false))

    $restoreArguments = @('restore', (Join-Path $consumerRoot 'Pocok.Consumer.csproj'), '--packages', $packagesRoot, '--no-cache')
    & dotnet @restoreArguments
    if ($LASTEXITCODE -ne 0) {
        throw "$PackageId $FeedMode external consumer restore failed with exit code $LASTEXITCODE."
    }

    & dotnet run --project (Join-Path $consumerRoot 'Pocok.Consumer.csproj') --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "$PackageId $FeedMode external consumer failed with exit code $LASTEXITCODE."
    }

    Write-Host "$PackageId $FeedMode smoke passed using $($Package.Name)."
}

if (-not $NoPack) {
    Remove-Item -LiteralPath $packageDirectoryPath -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $packageDirectoryPath -Force | Out-Null
    & dotnet pack (Join-Path $repositoryRoot 'Pocok.slnx') --configuration Release --output $packageDirectoryPath
    if ($LASTEXITCODE -ne 0) {
        throw "Package build failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path -LiteralPath $packageDirectoryPath -PathType Container)) {
    throw "Package directory does not exist: $packageDirectoryPath"
}

if ($PackageIds.Count -eq 0) {
    $PackageIds = @($consumerSpecs.Keys | Sort-Object)
}

$unknownPackageIds = @($PackageIds | Where-Object { -not $consumerSpecs.ContainsKey($_) })
if ($unknownPackageIds.Count -gt 0) {
    throw "No external consumer is configured for: $($unknownPackageIds -join ', ')"
}

Remove-Item -LiteralPath $closureFeed -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $candidateFeedRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $closureFeed -Force | Out-Null
New-Item -ItemType Directory -Path $candidateFeedRoot -Force | Out-Null
Get-ChildItem -LiteralPath $packageDirectoryPath -File -Filter '*.nupkg' |`
    Copy-Item -Destination $closureFeed

$systemTemp = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$workRoot = [System.IO.Path]::GetFullPath((Join-Path $systemTemp "pocok-package-smoke-$([guid]::NewGuid().ToString('N'))"))
if (-not $workRoot.StartsWith($systemTemp, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Smoke-test directory escaped the system temporary directory: $workRoot"
}

try {
    New-Item -ItemType Directory -Path $workRoot -Force | Out-Null

    foreach ($packageId in $PackageIds) {
        $package = Get-PackageArtifact -PackageId $packageId -Directory $packageDirectoryPath
        $candidateFeed = Join-Path $candidateFeedRoot $packageId
        New-Item -ItemType Directory -Path $candidateFeed -Force | Out-Null
        Copy-Item -LiteralPath $package.FullName -Destination $candidateFeed

        if ($Mode -in @('LocalClosure', 'Both')) {
            Invoke-Consumer -PackageId $packageId -Package $package -FeedMode 'local-closure' -Sources @($closureFeed, 'https://api.nuget.org/v3/index.json') -WorkRoot $workRoot
        }

        if ($Mode -in @('Publication', 'Both')) {
            Invoke-Consumer -PackageId $packageId -Package $package -FeedMode 'publication' -Sources @($candidateFeed, 'https://api.nuget.org/v3/index.json') -WorkRoot $workRoot
        }
    }
}
finally {
    $resolvedWorkRoot = [System.IO.Path]::GetFullPath($workRoot)
    if ($resolvedWorkRoot.StartsWith($systemTemp, [System.StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path -Leaf $resolvedWorkRoot) -like 'pocok-package-smoke-*') {
        Remove-Item -LiteralPath $resolvedWorkRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
