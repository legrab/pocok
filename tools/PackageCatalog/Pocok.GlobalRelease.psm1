Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-PocokRepositoryRoot {
    [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
}

function ConvertFrom-PocokGlobalTag {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Tag)

    if ($Tag -notmatch '^GLOBAL-v(?<version>[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?)$') {
        throw "Global release tag '$Tag' must match GLOBAL-v<major.minor.patch[-prerelease]> exactly. Build metadata is not supported."
    }

    $versionText = $Matches.version
    $parsed = $null
    if (-not [System.Management.Automation.SemanticVersion]::TryParse($versionText, [ref]$parsed)) {
        throw "Global release version '$versionText' is not valid SemVer."
    }

    [pscustomobject]@{ Tag = $Tag; Version = $versionText; SemanticVersion = $parsed }
}

function Get-PocokCatalog {
    [CmdletBinding()]
    param([string]$CatalogPath = 'eng/packages.json')

    $root = Get-PocokRepositoryRoot
    $path = [System.IO.Path]::GetFullPath((Join-Path $root $CatalogPath))
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Catalog not found: $path" }
    Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
}

function Get-PocokGlobalReleaseGraph {
    [CmdletBinding()]
    param([string]$CatalogPath = 'eng/packages.json')

    $catalog = Get-PocokCatalog -CatalogPath $CatalogPath
    $all = @($catalog.packages)
    $targets = @($all | Where-Object { $_.state -ne 'Retired' -and [bool]$_.releasable })
    if ($targets.Count -eq 0) { throw 'The catalog contains no releasable packages.' }

    foreach ($selector in @('id','project','tagPrefix','versionProperty')) {
        $duplicates = @($all | Group-Object -Property $selector | Where-Object Count -gt 1)
        if ($duplicates.Count -gt 0) { throw "Duplicate catalog $selector values: $($duplicates.Name -join ', ')" }
    }

    $byId = @{}
    foreach ($package in $all) { $byId[[string]$package.id] = $package }
    $targetIds = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($package in $targets) { [void]$targetIds.Add([string]$package.id) }

    $indegree = @{}
    $dependents = @{}
    foreach ($package in $targets) {
        $id = [string]$package.id
        $indegree[$id] = 0
        $dependents[$id] = [System.Collections.Generic.List[string]]::new()
    }

    foreach ($package in $targets) {
        $id = [string]$package.id
        foreach ($dependencyValue in @($package.internalDependencies)) {
            $dependency = [string]$dependencyValue
            if ($dependency -eq $id) { throw "$id depends on itself." }
            if (-not $byId.ContainsKey($dependency)) { throw "$id references unknown internal dependency $dependency." }
            $dependencyPackage = $byId[$dependency]
            if ($dependencyPackage.state -eq 'Retired' -or -not [bool]$dependencyPackage.releasable) {
                throw "Releasable package $id depends on non-releasable package $dependency."
            }
            $indegree[$id]++
            $dependents[$dependency].Add($id)
        }
    }

    $ready = [System.Collections.Generic.List[string]]::new()
    foreach ($id in $indegree.Keys) { if ($indegree[$id] -eq 0) { $ready.Add($id) } }
    $orderedIds = [System.Collections.Generic.List[string]]::new()
    while ($ready.Count -gt 0) {
        $next = @($ready | Sort-Object -CaseSensitive)[0]
        [void]$ready.Remove($next)
        $orderedIds.Add($next)
        foreach ($dependent in @($dependents[$next] | Sort-Object -CaseSensitive)) {
            $indegree[$dependent]--
            if ($indegree[$dependent] -eq 0) { $ready.Add($dependent) }
        }
    }

    if ($orderedIds.Count -ne $targets.Count) {
        $blocked = @($indegree.GetEnumerator() | Where-Object Value -gt 0 | ForEach-Object Key | Sort-Object)
        throw "Package dependency cycle detected among: $($blocked -join ', ')"
    }

    $position = 0
    @($orderedIds | ForEach-Object {
        $package = $byId[$_]
        [pscustomobject]@{
            order = $position++
            id = [string]$package.id
            project = [string]$package.project
            tagPrefix = [string]$package.tagPrefix
            versionProperty = [string]$package.versionProperty
            internalDependencies = @($package.internalDependencies | ForEach-Object { [string]$_ })
        }
    })
}

function Get-PocokTagCommit {
    param([Parameter(Mandatory)][string]$Tag, [switch]$AllowMissing)
    $root = Get-PocokRepositoryRoot
    $result = & git -C $root rev-parse --verify --quiet "refs/tags/$Tag^{commit}"
    if ($LASTEXITCODE -ne 0 -or -not $result) {
        if ($AllowMissing) { return $null }
        throw "Repository tag '$Tag' does not exist."
    }
    ([string]$result).Trim()
}

function Test-PocokReleaseTags {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$GlobalTag,
        [Parameter(Mandatory)][string]$Commit,
        [Parameter(Mandatory)][object[]]$Graph
    )

    $globalCommit = Get-PocokTagCommit -Tag $GlobalTag
    if ($globalCommit -ne $Commit) { throw "Global tag $GlobalTag resolves to $globalCommit, expected $Commit." }
    $version = (ConvertFrom-PocokGlobalTag -Tag $GlobalTag).Version
    foreach ($package in $Graph) {
        $packageTag = "$($package.tagPrefix)$version"
        $packageCommit = Get-PocokTagCommit -Tag $packageTag -AllowMissing
        if ($packageCommit -and $packageCommit -ne $Commit) {
            throw "Package-specific tag $packageTag for $($package.id) resolves to $packageCommit, expected global tag commit $Commit."
        }
    }
}

function Get-PocokNuGetFlatContainerBase {
    param([string]$Source = 'https://api.nuget.org/v3/index.json')
    $service = Invoke-RestMethod -Uri $Source -Method Get -TimeoutSec 30
    $resource = @($service.resources | Where-Object { $_.'@type' -like 'PackageBaseAddress*' } | Select-Object -First 1)
    if ($resource.Count -ne 1) { throw "NuGet service index does not expose PackageBaseAddress: $Source" }
    ([string]$resource[0].'@id').TrimEnd('/')
}

function Get-PocokNuspecProvenance {
    param([Parameter(Mandatory)][string]$PackagePath)
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
    try {
        $entry = @($archive.Entries | Where-Object { $_.FullName -match '^[^/]+\.nuspec$' } | Select-Object -First 1)
        if ($entry.Count -ne 1) { throw "Package '$PackagePath' has no root nuspec." }
        $reader = [System.IO.StreamReader]::new($entry[0].Open())
        try { [xml]$xml = $reader.ReadToEnd() } finally { $reader.Dispose() }
        $metadata = $xml.package.metadata
        [pscustomobject]@{
            Id = [string]$metadata.id
            Version = [string]$metadata.version
            RepositoryUrl = [string]$metadata.repository.url
            RepositoryCommit = [string]$metadata.repository.commit
        }
    }
    finally { $archive.Dispose() }
}

function Get-PocokNuGetPackageState {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$PackageId,
        [Parameter(Mandatory)][string]$Version,
        [Parameter(Mandatory)][string]$ExpectedCommit,
        [string]$Source = 'https://api.nuget.org/v3/index.json',
        [string]$DownloadDirectory = 'artifacts/global-release/public'
    )

    $root = Get-PocokRepositoryRoot
    $base = Get-PocokNuGetFlatContainerBase -Source $Source
    $idLower = $PackageId.ToLowerInvariant()
    $versionLower = $Version.ToLowerInvariant()
    $indexUrl = "$base/$idLower/index.json"
    try { $index = Invoke-RestMethod -Uri $indexUrl -Method Get -TimeoutSec 30 -ErrorAction Stop }
    catch {
        if ($_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq 404) {
            return [pscustomobject]@{ PackageId=$PackageId; Version=$Version; State='PendingPublish'; RepositoryCommit=$null; PackagePath=$null }
        }
        throw
    }
    $exists = @($index.versions | Where-Object { ([string]$_).ToLowerInvariant() -eq $versionLower }).Count -gt 0
    if (-not $exists) { return [pscustomobject]@{ PackageId=$PackageId; Version=$Version; State='PendingPublish'; RepositoryCommit=$null; PackagePath=$null } }

    $directory = [System.IO.Path]::GetFullPath((Join-Path $root $DownloadDirectory))
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $path = Join-Path $directory "$PackageId.$Version.public.nupkg"
    Invoke-WebRequest -Uri "$base/$idLower/$versionLower/$idLower.$versionLower.nupkg" -OutFile $path -TimeoutSec 60
    $provenance = Get-PocokNuspecProvenance -PackagePath $path
    $repoOk = $provenance.RepositoryUrl -match '^https://github\.com/legrab/pocok(?:\.git)?/?$'
    if ($provenance.Id -ne $PackageId -or $provenance.Version -ne $Version -or -not $repoOk -or $provenance.RepositoryCommit -ne $ExpectedCommit) {
        throw "NuGet package $PackageId $Version already exists with mismatched or insufficient provenance. Expected legrab/pocok commit $ExpectedCommit; observed repository '$($provenance.RepositoryUrl)' commit '$($provenance.RepositoryCommit)'. NuGet versions are immutable; use a new GLOBAL version."
    }
    [pscustomobject]@{ PackageId=$PackageId; Version=$Version; State='AlreadyPublishedMatching'; RepositoryCommit=$provenance.RepositoryCommit; PackagePath=$path }
}

function Wait-PocokNuGetPackage {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$PackageId,
        [Parameter(Mandatory)][string]$Version,
        [Parameter(Mandatory)][string]$ExpectedCommit,
        [string]$Source = 'https://api.nuget.org/v3/index.json',
        [TimeSpan]$Timeout = ([TimeSpan]::FromMinutes(15))
    )
    $started = [DateTimeOffset]::UtcNow
    $delays = @(2,4,8,15,30)
    $attempt = 0
    while ([DateTimeOffset]::UtcNow - $started -lt $Timeout) {
        try {
            $state = Get-PocokNuGetPackageState -PackageId $PackageId -Version $Version -ExpectedCommit $ExpectedCommit -Source $Source
            if ($state.State -eq 'AlreadyPublishedMatching') { return $state }
        }
        catch {
            if ($_.Exception.Message -match 'mismatched or insufficient provenance') { throw }
            Write-Warning $_.Exception.Message
        }
        $delay = $delays[[Math]::Min($attempt, $delays.Count - 1)]
        Start-Sleep -Seconds $delay
        $attempt++
    }
    throw "Timed out after $Timeout waiting for $PackageId $Version to become available with matching provenance."
}

function Write-PocokJson {
    param([Parameter(Mandatory)]$Value, [Parameter(Mandatory)][string]$Path)
    $root = Get-PocokRepositoryRoot
    $resolved = [System.IO.Path]::GetFullPath((Join-Path $root $Path))
    New-Item -ItemType Directory -Path (Split-Path -Parent $resolved) -Force | Out-Null
    [System.IO.File]::WriteAllText($resolved, ($Value | ConvertTo-Json -Depth 12), [System.Text.UTF8Encoding]::new($false))
    $resolved
}

Export-ModuleMember -Function ConvertFrom-PocokGlobalTag,Get-PocokCatalog,Get-PocokGlobalReleaseGraph,Get-PocokTagCommit,Test-PocokReleaseTags,Get-PocokNuGetPackageState,Wait-PocokNuGetPackage,Write-PocokJson,Get-PocokRepositoryRoot
