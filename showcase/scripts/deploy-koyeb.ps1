[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $Repository,
    [string] $Branch = "main",
    [string] $App = "pocok",
    [string] $Service = "showcase",
    [string] $Region = "fra",
    [string] $InstanceType = "free",
    [switch] $Strict,
    [switch] $Execute
)

$ErrorActionPreference = "Stop"
$strictValue = if ($Strict) { "true" } else { "false" }
$arguments = @(
    "services", "create", $Service,
    "--app", $App,
    "--git", $Repository,
    "--git-branch", $Branch,
    "--git-builder", "docker",
    "--git-docker-dockerfile", "showcase/Dockerfile",
    "--instance-type", $InstanceType,
    "--regions", $Region,
    "--env", "PORT=8080",
    "--env", "ASPNETCORE_ENVIRONMENT=Production",
    "--env", "Showcase__RequireCompleteCatalog=$strictValue",
    "--ports", "8080:http",
    "--routes", "/:8080",
    "--checks", "8080:http:/health/ready",
    "--wait"
)

Write-Host "Repository: $Repository"
Write-Host "Branch: $Branch"
Write-Host "App/service: $App/$Service"
Write-Host "Region: $Region"
Write-Host "Instance: $InstanceType"
Write-Host "Strict catalog: $strictValue"
Write-Host ("Command: koyeb " + ($arguments -join " "))

if ($Execute) {
    if (-not (Get-Command koyeb -ErrorAction SilentlyContinue)) {
        throw "Koyeb CLI is not installed."
    }

    & koyeb @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Koyeb deployment failed with exit code $LASTEXITCODE."
    }
}
else {
    Write-Host "Dry run only. Add -Execute after authenticating the Koyeb CLI and ensuring the app exists."
}
