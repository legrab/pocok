[CmdletBinding()]
param(
    [switch]$NoPack,
    [string[]]$PackageIds = @()
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$packageDirectory = Join-Path $repositoryRoot 'artifacts/packages'

if (-not $NoPack) {
    & dotnet pack (Join-Path $repositoryRoot 'Pocok.slnx') --configuration Release --output $packageDirectory
    if ($LASTEXITCODE -ne 0) {
        throw "Package build failed with exit code $LASTEXITCODE."
    }
}

$consumerSpecs = @{
    'Pocok.Primitives' = @{
        Template = 'PrimitivesConsumer/Pocok.Primitives.Consumer.csproj.template'
        Program = 'PrimitivesConsumer/Program.cs'
    }
    'Pocok.Conversion.Abstractions' = @{
        Template = 'ConversionAbstractionsConsumer/Pocok.Conversion.Abstractions.Consumer.csproj.template'
        Program = 'ConversionAbstractionsConsumer/Program.cs'
    }
    'Pocok.Conversion' = @{
        Template = 'ConversionConsumer/Pocok.Conversion.Consumer.csproj.template'
        Program = 'ConversionConsumer/Program.cs'
    }
}

if ($PackageIds.Count -eq 0) {
    $PackageIds = Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'src') -Recurse -File -Filter '*.csproj' |
        ForEach-Object {
            [xml]$projectDocument = [System.IO.File]::ReadAllText($_.FullName)
            $isPackableNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/IsPackable')
            $packageIdNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/PackageId')
            if ($isPackableNode.InnerText -eq 'true' -and $null -ne $packageIdNode) {
                $packageIdNode.InnerText
            }
        }
}

$unknownPackageIds = $PackageIds | Where-Object { -not $consumerSpecs.ContainsKey($_) }
if ($unknownPackageIds) {
    throw "No external consumer is configured for: $($unknownPackageIds -join ', ')"
}

$systemTemp = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$workRoot = [System.IO.Path]::GetFullPath((Join-Path $systemTemp "pocok-package-smoke-$([guid]::NewGuid().ToString('N'))"))

if (-not $workRoot.StartsWith($systemTemp, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Smoke-test directory escaped the system temporary directory: $workRoot"
}

try {
    New-Item -ItemType Directory -Path $workRoot | Out-Null

    foreach ($packageId in $PackageIds) {
        $escapedPackageId = [regex]::Escape($packageId)
        $package = Get-ChildItem -LiteralPath $packageDirectory -File -Filter "$packageId.*.nupkg" |
            Where-Object { $_.Name -notlike '*.snupkg' } |
            Where-Object { $_.Name -match "^$escapedPackageId\.(?<version>[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?)\.nupkg$" } |
            Sort-Object LastWriteTimeUtc -Descending |
            Select-Object -First 1

        if ($null -eq $package) {
            throw "$packageId package was not found in $packageDirectory."
        }

        $packageVersion = [regex]::Match(
            $package.Name,
            "^$escapedPackageId\.(?<version>.+)\.nupkg$").Groups['version'].Value
        $consumerRoot = Join-Path $workRoot $packageId
        $packages = Join-Path $consumerRoot '.packages'
        New-Item -ItemType Directory -Path $consumerRoot | Out-Null

        $spec = $consumerSpecs[$packageId]
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $spec.Program) -Destination (Join-Path $consumerRoot 'Program.cs')

        $consumerProjectPath = Join-Path $consumerRoot 'Pocok.Consumer.csproj'
        $projectTemplate = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot $spec.Template))
        $projectContent = $projectTemplate.Replace('__PACKAGE_VERSION__', $packageVersion)
        [System.IO.File]::WriteAllText($consumerProjectPath, $projectContent, [Text.UTF8Encoding]::new($false))

        & dotnet restore $consumerProjectPath --source $packageDirectory --packages $packages
        if ($LASTEXITCODE -ne 0) {
            throw "$packageId external consumer restore failed with exit code $LASTEXITCODE."
        }

        & dotnet run --project $consumerProjectPath --no-restore
        if ($LASTEXITCODE -ne 0) {
            throw "$packageId external consumer failed with exit code $LASTEXITCODE."
        }

        Write-Host "Package smoke passed using $($package.Name)."
    }
}
finally {
    $resolvedWorkRoot = [System.IO.Path]::GetFullPath($workRoot)
    if ($resolvedWorkRoot.StartsWith($systemTemp, [System.StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path -Leaf $resolvedWorkRoot) -like 'pocok-package-smoke-*') {
        Remove-Item -LiteralPath $resolvedWorkRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
