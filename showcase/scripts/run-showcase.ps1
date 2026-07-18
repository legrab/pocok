[CmdletBinding()]
param(
    [ValidateRange(1, 65535)]
    [int] $Port = 8080,
    [switch] $NoRestore,
    [string] $DotNetPath = $(if ($env:DOTNET_HOST_PATH) { $env:DOTNET_HOST_PATH } else { "dotnet" })
)

$ErrorActionPreference = "Stop"
Remove-Item Env:PLATFORM -ErrorAction SilentlyContinue
$temporary = Join-Path ([System.IO.Path]::GetTempPath()) ("pocok-showcase-" + [Guid]::NewGuid().ToString("N"))
$exitCode = 0

try {
    $publish = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "publish-showcase.ps1"
    & $publish -OutputPath $temporary -NoRestore:$NoRestore -DotNetPath $DotNetPath
    if ($LASTEXITCODE -ne 0) {
        throw "Showcase publication failed."
    }

    $env:PORT = $Port.ToString([Globalization.CultureInfo]::InvariantCulture)
    $env:ASPNETCORE_ENVIRONMENT = if ($env:ASPNETCORE_ENVIRONMENT) {
        $env:ASPNETCORE_ENVIRONMENT
    }
    else {
        "Development"
    }
    $env:SHOWCASE_PLUGIN_DIR = Join-Path $temporary "plugins"

    Write-Host "Pocok Showcase: http://127.0.0.1:$Port"
    & $DotNetPath (Join-Path $temporary "Pocok.Showcase.Web.dll")
    $exitCode = $LASTEXITCODE
}
finally {
    Remove-Item -LiteralPath $temporary -Recurse -Force -ErrorAction SilentlyContinue
}

exit $exitCode
