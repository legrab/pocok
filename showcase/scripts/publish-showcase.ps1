[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string] $OutputPath = "artifacts/showcase",
    [switch] $NoRestore,
    [switch] $RequireComplete,
    [string] $DotNetPath = $(if ($env:DOTNET_HOST_PATH) { $env:DOTNET_HOST_PATH } else { "dotnet" })
)

$ErrorActionPreference = "Stop"
Remove-Item Env:PLATFORM -ErrorAction SilentlyContinue

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptDirectory "../.."))
$output = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    [System.IO.Path]::GetFullPath($OutputPath)
}
else {
    [System.IO.Path]::GetFullPath($OutputPath, (Get-Location).Path)
}

$toolProject = Join-Path $repositoryRoot "showcase/tools/Pocok.Showcase.PublishTool/Pocok.Showcase.PublishTool.csproj"
$toolDll = Join-Path $repositoryRoot "showcase/tools/Pocok.Showcase.PublishTool/bin/Release/net10.0/Pocok.Showcase.PublishTool.dll"
$buildArguments = @("build", $toolProject, "--configuration", "Release", "--nologo", "--maxcpucount:1")
if ($NoRestore) {
    $buildArguments += "--no-restore"
}

& $DotNetPath @buildArguments
if ($LASTEXITCODE -ne 0) {
    throw "Showcase publish-tool build failed with exit code $LASTEXITCODE."
}

$publishArguments = @(
    $toolDll,
    "--repository-root", $repositoryRoot,
    "--output", $output,
    "--dotnet", $DotNetPath
)
if ($NoRestore) {
    $publishArguments += "--no-restore"
}
if ($RequireComplete) {
    $publishArguments += "--require-complete"
}

& $DotNetPath @publishArguments
if ($LASTEXITCODE -ne 0) {
    throw "Showcase publication failed with exit code $LASTEXITCODE."
}
