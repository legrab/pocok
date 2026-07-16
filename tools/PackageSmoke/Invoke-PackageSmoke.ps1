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
$nugetSource = 'https://api.nuget.org/v3/index.json'
$closureResolver = Join-Path $repositoryRoot 'tools/PackageCatalog/Resolve-PackageClosure.ps1'
$catalog = Get-Content -LiteralPath (Join-Path $repositoryRoot 'eng/packages.json') -Raw | ConvertFrom-Json
$packagesById = @{}
foreach ($package in $catalog.packages) {
    $packagesById[[string]$package.id] = $package
}

$consumerSpecs = @{
    'Pocok.AppDefaults.Licensing' = @{ Template = 'AppDefaultsLicensingConsumer/Pocok.AppDefaults.Licensing.Consumer.csproj.template'; Program = 'AppDefaultsLicensingConsumer/Program.cs' }
    'Pocok.Licensing' = @{ Template = 'LicensingConsumer/Pocok.Licensing.Consumer.csproj.template'; Program = 'LicensingConsumer/Program.cs' }
    'Pocok.AppDefaults.Modularity' = @{ Template = 'AppDefaultsModularityConsumer/Pocok.AppDefaults.Modularity.Consumer.csproj.template'; Program = 'AppDefaultsModularityConsumer/Program.cs' }
    'Pocok.Modularity' = @{ Template = 'ModularityConsumer/Pocok.Modularity.Consumer.csproj.template'; Program = 'ModularityConsumer/Program.cs' }
    'Pocok.Modularity.Contracts' = @{ Template = 'ModularityContractsConsumer/Pocok.Modularity.Contracts.Consumer.csproj.template'; Program = 'ModularityContractsConsumer/Program.cs' }
    'Pocok.AppDefaults.Logging.Serilog' = @{ Template = 'AppDefaultsLoggingSerilogConsumer/Pocok.AppDefaults.Logging.Serilog.Consumer.csproj.template'; Program = 'AppDefaultsLoggingSerilogConsumer/Program.cs' }
    'Pocok.AppDefaults.Logging' = @{ Template = 'AppDefaultsLoggingConsumer/Pocok.AppDefaults.Logging.Consumer.csproj.template'; Program = 'AppDefaultsLoggingConsumer/Program.cs' }
    'Pocok.AppDefaults' = @{ Template = 'AppDefaultsConsumer/Pocok.AppDefaults.Consumer.csproj.template'; Program = 'AppDefaultsConsumer/Program.cs' }
    'Pocok.Conversion' = @{ Template = 'ConversionConsumer/Pocok.Conversion.Consumer.csproj.template'; Program = 'ConversionConsumer/Program.cs' }
    'Pocok.Readiness' = @{ Template = 'ReadinessConsumer/Pocok.Readiness.Consumer.csproj.template'; Program = 'ReadinessConsumer/Program.cs' }
    'Pocok.Scripting' = @{ Template = 'ScriptingConsumer/Pocok.Scripting.Consumer.csproj.template'; Program = 'ScriptingConsumer/Program.cs' }
    'Pocok.Signals' = @{ Template = 'SignalsConsumer/Pocok.Signals.Consumer.csproj.template'; Program = 'SignalsConsumer/Program.cs' }
    'Pocok.Localization' = @{ Template = 'LocalizationConsumer/Pocok.Localization.Consumer.csproj.template'; Program = 'LocalizationConsumer/Program.cs' }
    'Pocok.Subscriptions' = @{ Template = 'SubscriptionsConsumer/Pocok.Subscriptions.Consumer.csproj.template'; Program = 'SubscriptionsConsumer/Program.cs' }
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

function Write-NuGetConfig {
    param(
        [Parameter(Mandatory)] [string]$Path,
        [Parameter(Mandatory)] [object[]]$Sources,
        [Parameter(Mandatory)] [hashtable]$Mappings
    )

    $settings = [System.Xml.XmlWriterSettings]::new()
    $settings.Indent = $true
    $settings.Encoding = [System.Text.UTF8Encoding]::new($false)
    $writer = [System.Xml.XmlWriter]::Create($Path, $settings)
    try {
        $writer.WriteStartDocument()
        $writer.WriteStartElement('configuration')
        $writer.WriteStartElement('packageSources')
        $writer.WriteStartElement('clear')
        $writer.WriteEndElement()
        foreach ($source in $Sources) {
            $writer.WriteStartElement('add')
            $writer.WriteAttributeString('key', [string]$source.Key)
            $writer.WriteAttributeString('value', [string]$source.Value)
            $writer.WriteEndElement()
        }
        $writer.WriteEndElement()

        $writer.WriteStartElement('packageSourceMapping')
        foreach ($source in $Sources) {
            $key = [string]$source.Key
            $writer.WriteStartElement('packageSource')
            $writer.WriteAttributeString('key', $key)
            foreach ($pattern in @($Mappings[$key])) {
                $writer.WriteStartElement('package')
                $writer.WriteAttributeString('pattern', [string]$pattern)
                $writer.WriteEndElement()
            }
            $writer.WriteEndElement()
        }
        $writer.WriteEndElement()
        $writer.WriteEndElement()
        $writer.WriteEndDocument()
    }
    finally {
        $writer.Dispose()
    }
}

function Invoke-Consumer {
    param(
        [Parameter(Mandatory)] [string]$PackageId,
        [Parameter(Mandatory)] [System.IO.FileInfo]$Package,
        [Parameter(Mandatory)] [string]$FeedMode,
        [Parameter(Mandatory)] [object[]]$Sources,
        [Parameter(Mandatory)] [hashtable]$Mappings,
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

    $nugetConfig = Join-Path $consumerRoot 'NuGet.Config'
    Write-NuGetConfig -Path $nugetConfig -Sources $Sources -Mappings $Mappings

    & dotnet restore (Join-Path $consumerRoot 'Pocok.Consumer.csproj') --packages $packagesRoot --no-cache --configfile $nugetConfig
    if ($LASTEXITCODE -ne 0) {
        throw "$PackageId $FeedMode external consumer restore failed with exit code $LASTEXITCODE."
    }

    & dotnet run --project (Join-Path $consumerRoot 'Pocok.Consumer.csproj') --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "$PackageId $FeedMode external consumer failed with exit code $LASTEXITCODE."
    }

    Write-Host "$PackageId $FeedMode smoke passed using $($Package.Name)."
}

if ($PackageIds.Count -eq 0) {
    $PackageIds = @($consumerSpecs.Keys | Sort-Object)
}

$unknownPackageIds = @($PackageIds | Where-Object { -not $consumerSpecs.ContainsKey($_) })
if ($unknownPackageIds.Count -gt 0) {
    throw "No external consumer is configured for: $($unknownPackageIds -join ', ')"
}

$requestedClosures = @{}
$closureIds = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$orderedClosureIds = [System.Collections.Generic.List[string]]::new()
foreach ($packageId in $PackageIds) {
    $closure = @(& $closureResolver -CandidatePackageId $packageId | ForEach-Object { [string]$_.id })
    $requestedClosures[$packageId] = $closure
    foreach ($closureId in $closure) {
        if ($closureIds.Add($closureId)) {
            $orderedClosureIds.Add($closureId)
        }
    }
}

if (-not $NoPack) {
    Remove-Item -LiteralPath $packageDirectoryPath -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $packageDirectoryPath -Force | Out-Null
    foreach ($closureId in $orderedClosureIds) {
        $project = [string]$packagesById[$closureId].project
        & dotnet pack (Join-Path $repositoryRoot $project) --configuration Release --output $packageDirectoryPath
        if ($LASTEXITCODE -ne 0) {
            throw "Packing $closureId failed with exit code $LASTEXITCODE."
        }
    }
}

if (-not (Test-Path -LiteralPath $packageDirectoryPath -PathType Container)) {
    throw "Package directory does not exist: $packageDirectoryPath"
}

Remove-Item -LiteralPath $closureFeed -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $candidateFeedRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $closureFeed -Force | Out-Null
New-Item -ItemType Directory -Path $candidateFeedRoot -Force | Out-Null
foreach ($closureId in $closureIds) {
    $artifact = Get-PackageArtifact -PackageId $closureId -Directory $packageDirectoryPath
    Copy-Item -LiteralPath $artifact.FullName -Destination $closureFeed
}

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
            $sources = @(
                [pscustomobject]@{ Key = 'pocok-local'; Value = $closureFeed },
                [pscustomobject]@{ Key = 'nuget-org'; Value = $nugetSource }
            )
            $mappings = @{
                'pocok-local' = @('Pocok.*')
                'nuget-org' = @('Acornima', 'Jint', 'Microsoft.*', 'System.*', 'Serilog*', 'NETStandard.Library')
            }
            Invoke-Consumer -PackageId $packageId -Package $package -FeedMode 'local-closure' -Sources $sources -Mappings $mappings -WorkRoot $workRoot
        }

        if ($Mode -in @('Publication', 'Both')) {
            $publishedInternalDependencies = @($requestedClosures[$packageId] | Where-Object { $_ -ne $packageId })
            $sources = @(
                [pscustomobject]@{ Key = 'candidate-local'; Value = $candidateFeed },
                [pscustomobject]@{ Key = 'nuget-org'; Value = $nugetSource }
            )
            $mappings = @{
                'candidate-local' = @($packageId)
                'nuget-org' = @($publishedInternalDependencies + @('Acornima', 'Jint', 'Microsoft.*', 'System.*', 'Serilog*', 'NETStandard.Library'))
            }
            Invoke-Consumer -PackageId $packageId -Package $package -FeedMode 'publication' -Sources $sources -Mappings $mappings -WorkRoot $workRoot
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
