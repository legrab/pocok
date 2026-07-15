[CmdletBinding()]
param([ValidateSet('Debug', 'Release')] [string]$Configuration = 'Release')

$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$pluginProject = Join-Path $root 'samples/ModularCommunicator.EchoPlugin/Pocok.ModularCommunicator.EchoPlugin.csproj'
$hostProject = Join-Path $root 'samples/ModularCommunicator.Host/Pocok.ModularCommunicator.Host.csproj'

& dotnet build $pluginProject --configuration $Configuration
if ($LASTEXITCODE -ne 0) { throw "Plugin build failed with exit code $LASTEXITCODE." }
& dotnet build $hostProject --configuration $Configuration
if ($LASTEXITCODE -ne 0) { throw "Host build failed with exit code $LASTEXITCODE." }

$source = Join-Path $root "samples/ModularCommunicator.EchoPlugin/bin/$Configuration/net10.0"
$destination = Join-Path $root "samples/ModularCommunicator.Host/bin/$Configuration/net10.0/plugins/echo"
Remove-Item -LiteralPath $destination -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $destination -Force | Out-Null
Get-ChildItem -LiteralPath $source -File | Copy-Item -Destination $destination

Write-Host "Staged the echo plugin at $destination"
$pluginRoot = Split-Path -Parent $destination
Write-Host "Run: dotnet run --project '$hostProject' --configuration $Configuration --no-build -- '$pluginRoot'"
