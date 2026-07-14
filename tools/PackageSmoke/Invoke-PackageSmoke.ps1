[CmdletBinding()]
param(
    [switch]$NoPack
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

$package = Get-ChildItem -LiteralPath $packageDirectory -File -Filter 'Pocok.Primitives.*.nupkg' |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

if ($null -eq $package) {
    throw "Pocok.Primitives package was not found in $packageDirectory."
}

$templateRoot = Join-Path $PSScriptRoot 'ExternalConsumer'
$systemTemp = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$workRoot = [System.IO.Path]::GetFullPath((Join-Path $systemTemp "pocok-package-smoke-$([guid]::NewGuid().ToString('N'))"))

if (-not $workRoot.StartsWith($systemTemp, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Smoke-test directory escaped the system temporary directory: $workRoot"
}

try {
    New-Item -ItemType Directory -Path $workRoot | Out-Null
    Copy-Item -LiteralPath (Join-Path $templateRoot 'Program.cs') -Destination $workRoot
    Copy-Item -LiteralPath (Join-Path $templateRoot 'Pocok.Primitives.Consumer.csproj.template') -Destination (Join-Path $workRoot 'Pocok.Primitives.Consumer.csproj')

    $project = Join-Path $workRoot 'Pocok.Primitives.Consumer.csproj'
    $packages = Join-Path $workRoot '.packages'

    & dotnet restore $project --source $packageDirectory --packages $packages
    if ($LASTEXITCODE -ne 0) {
        throw "External consumer restore failed with exit code $LASTEXITCODE."
    }

    & dotnet run --project $project --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "External consumer failed with exit code $LASTEXITCODE."
    }
}
finally {
    $resolvedWorkRoot = [System.IO.Path]::GetFullPath($workRoot)
    if ($resolvedWorkRoot.StartsWith($systemTemp, [System.StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path -Leaf $resolvedWorkRoot) -like 'pocok-package-smoke-*') {
        Remove-Item -LiteralPath $resolvedWorkRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "Package smoke test passed using $($package.Name)."
