# SPDX-License-Identifier: Apache-2.0
# Copyright 2026 Pocok contributors

Set-StrictMode -Version Latest

function Get-PocokRepositoryRoot {
    param([string]$StartPath = $PSScriptRoot)

    $current = [System.IO.DirectoryInfo]::new([System.IO.Path]::GetFullPath($StartPath))
    while ($null -ne $current) {
        if (Test-Path -LiteralPath (Join-Path $current.FullName 'eng/packages.json') -PathType Leaf) {
            return $current.FullName
        }
        $current = $current.Parent
    }

    throw "Could not find the Pocok repository root from '$StartPath'."
}

function ConvertTo-PocokPath {
    param(
        [Parameter(Mandatory)] [string]$Path,
        [Parameter(Mandatory)] [string]$RepositoryRoot
    )

    $root = [System.IO.Path]::GetFullPath($RepositoryRoot).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $candidate = $Path -replace '\\', '/'
    if ([System.IO.Path]::IsPathRooted($Path)) {
        $candidate = [System.IO.Path]::GetRelativePath($root, [System.IO.Path]::GetFullPath($Path))
    }
    $candidate = $candidate -replace '\\', '/'
    while ($candidate.StartsWith('./', [StringComparison]::Ordinal)) {
        $candidate = $candidate.Substring(2)
    }
    return $candidate.TrimStart('/')
}

function Resolve-PocokPath {
    param(
        [Parameter(Mandatory)] [string]$RepositoryRoot,
        [Parameter(Mandatory)] [string]$Path
    )

    return [System.IO.Path]::GetFullPath((Join-Path $RepositoryRoot ($Path -replace '/', [System.IO.Path]::DirectorySeparatorChar)))
}

function Write-PocokJson {
    param(
        [Parameter(Mandatory)] $InputObject,
        [Parameter(Mandatory)] [string]$Path,
        [int]$Depth = 12
    )

    $directory = Split-Path -Parent $Path
    if ($directory) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }
    [System.IO.File]::WriteAllText(
        $Path,
        (ConvertTo-Json -InputObject $InputObject -Depth $Depth),
        [System.Text.UTF8Encoding]::new($false))
}

function Get-PocokProjectKind {
    param([Parameter(Mandatory)] [string]$Path)

    switch -Regex ($Path) {
        '^showcase/' { return 'Showcase' }
        '^samples/Showcase/' { return 'Showcase' }
        '^src/' { return 'Source' }
        '^tests/Fixtures/' { return 'Fixture' }
        '^tests/' { return 'Test' }
        '^samples/' { return 'Sample' }
        '^benchmarks/' { return 'Benchmark' }
        default { return 'Other' }
    }
}

function Get-PocokProjectModel {
    param([string]$RepositoryRoot = (Get-PocokRepositoryRoot))

    $root = [System.IO.Path]::GetFullPath($RepositoryRoot)
    Write-Host "[CI tooling] Discovering projects under '$root'."

    # Filter generated directories by repository-relative path. Filtering the absolute
    # path is incorrect when a synthetic test repository itself lives under artifacts/.
    $projectFiles = @(
        Get-ChildItem -LiteralPath $root -Recurse -File -Filter '*.csproj' |
            Where-Object {
                $relativePath = ConvertTo-PocokPath -Path $_.FullName -RepositoryRoot $root
                $relativePath -notmatch '(^|/)(bin|obj|artifacts|\.git)(/|$)'
            } |
            Sort-Object FullName
    )
    Write-Host "[CI tooling] Discovered $(@($projectFiles).Length) project file(s)."

    $projects = [ordered]@{}
    foreach ($file in $projectFiles) {
        $path = ConvertTo-PocokPath -Path $file.FullName -RepositoryRoot $root
        try {
            [xml]$document = Get-Content -LiteralPath $file.FullName -Raw
        }
        catch {
            throw "Failed to parse project '$path': $($_.Exception.Message)"
        }
        $references = @(
            $document.SelectNodes('//*[local-name()="ProjectReference"]') |
                ForEach-Object { $_.GetAttribute('Include') } |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
                ForEach-Object {
                    ConvertTo-PocokPath -Path ([System.IO.Path]::GetFullPath((Join-Path $file.DirectoryName $_))) -RepositoryRoot $root
                } |
                Sort-Object -Unique
        )
        $compileIncludes = @(
            $document.SelectNodes('//*[local-name()="Compile"]') |
                ForEach-Object { $_.GetAttribute('Include') } |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and ($_ -match '[\\/]') } |
                ForEach-Object {
                    ConvertTo-PocokPath -Path ([System.IO.Path]::GetFullPath((Join-Path $file.DirectoryName $_))) -RepositoryRoot $root
                } |
                Sort-Object -Unique
        )
        $projectName = [System.IO.Path]::GetFileNameWithoutExtension($path)
        $outputTypeNode = $document.SelectSingleNode('//*[local-name()="OutputType"]')
        $assemblyNameNode = $document.SelectSingleNode('//*[local-name()="AssemblyName"]')
        $packageIdNode = $document.SelectSingleNode('//*[local-name()="PackageId"]')
        $isTestProject = $projectName.EndsWith('.Tests', [StringComparison]::Ordinal)
        $projects[$path] = [pscustomobject]@{
            Path = $path
            Directory = ([System.IO.Path]::GetDirectoryName($path) -replace '\\', '/')
            Name = $projectName
            Kind = Get-PocokProjectKind -Path $path
            References = $references
            CompileIncludes = $compileIncludes
            OutputType = if ($null -eq $outputTypeNode) { 'Library' } else { [string]$outputTypeNode.InnerText }
            AssemblyName = if ($null -eq $assemblyNameNode) { $projectName } else { [string]$assemblyNameNode.InnerText }
            PackageId = if ($null -eq $packageIdNode) { $projectName } else { [string]$packageIdNode.InnerText }
            IsTestProject = $isTestProject
        }
    }

    foreach ($project in $projects.Values) {
        foreach ($reference in $project.References) {
            if (-not $projects.Contains($reference)) {
                throw "Project '$($project.Path)' references missing project '$reference'."
            }
        }
    }

    $visiting = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $visited = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $visitPath = [System.Collections.Generic.List[string]]::new()
    function Visit-PocokProject {
        param([string]$ProjectPath)
        if ($visited.Contains($ProjectPath)) { return }
        if (-not $visiting.Add($ProjectPath)) {
            $cycleStart = $visitPath.IndexOf($ProjectPath)
            $cycle = if ($cycleStart -ge 0) {
                @($visitPath.GetRange($cycleStart, $visitPath.Count - $cycleStart)) + @($ProjectPath)
            }
            else {
                @($ProjectPath)
            }
            throw "Project dependency cycle detected: $($cycle -join ' -> ')."
        }

        $visitPath.Add($ProjectPath)
        try {
            foreach ($reference in $projects[$ProjectPath].References) {
                Visit-PocokProject -ProjectPath $reference
            }
            $visited.Add($ProjectPath) | Out-Null
        }
        finally {
            $visiting.Remove($ProjectPath) | Out-Null
            if ($visitPath.Count -gt 0) {
                $visitPath.RemoveAt($visitPath.Count - 1)
            }
        }
    }
    foreach ($projectPath in $projects.Keys) {
        Visit-PocokProject -ProjectPath $projectPath
    }
    Write-Host "[CI tooling] Project graph validation passed for $(@($projects.Keys).Length) project(s)."

    return $projects
}

function Get-PocokPackageModel {
    param(
        [string]$RepositoryRoot = (Get-PocokRepositoryRoot),
        $Projects = (Get-PocokProjectModel -RepositoryRoot $RepositoryRoot)
    )

    $catalogPath = Join-Path $RepositoryRoot 'eng/packages.json'
    $catalog = Get-Content -LiteralPath $catalogPath -Raw | ConvertFrom-Json
    $packages = [ordered]@{}
    foreach ($package in $catalog.packages) {
        $id = [string]$package.id
        $projectPath = ([string]$package.project -replace '\\', '/')
        if ([string]$package.state -ne 'Retired' -and -not $Projects.Contains($projectPath)) {
            throw "Package '$id' references missing project '$projectPath'."
        }
        $packages[$id] = [pscustomobject]@{
            Id = $id
            Project = $projectPath
            State = [string]$package.state
            Releasable = [bool]$package.releasable
            Consumer = [string]$package.consumer
            InternalDependencies = @($package.internalDependencies | ForEach-Object { [string]$_ } | Sort-Object -Unique)
        }
    }

    $activePackageByProject = @{}
    foreach ($package in $packages.Values | Where-Object State -ne 'Retired') {
        $activePackageByProject[$package.Project] = $package.Id
    }
    foreach ($package in $packages.Values | Where-Object State -ne 'Retired') {
        $projectDependencyIds = @(
            $Projects[$package.Project].References |
                Where-Object { $activePackageByProject.ContainsKey($_) } |
                ForEach-Object { $activePackageByProject[$_] } |
                Sort-Object -Unique
        )
        $catalogDependencyIds = @($package.InternalDependencies | Sort-Object -Unique)
        if (($projectDependencyIds -join "`n") -cne ($catalogDependencyIds -join "`n")) {
            throw "Package catalog dependencies for '$($package.Id)' do not match its direct package project references. Catalog: [$($catalogDependencyIds -join ', ')]. Projects: [$($projectDependencyIds -join ', ')]."
        }
    }

    foreach ($package in $packages.Values | Where-Object State -ne 'Retired') {
        foreach ($dependencyId in $package.InternalDependencies) {
            if (-not $packages.Contains($dependencyId)) {
                throw "Package '$($package.Id)' references unknown package '$dependencyId'."
            }
            if ($packages[$dependencyId].State -eq 'Retired') {
                throw "Package '$($package.Id)' references retired package '$dependencyId'."
            }
            $projectClosure = Get-PocokForwardClosure -Seeds @($package.Project) -Projects $Projects
            if ($projectClosure -notcontains $packages[$dependencyId].Project) {
                throw "Package catalog dependency '$($package.Id)' -> '$dependencyId' is not represented by project references."
            }
        }
    }

    $visiting = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $visited = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    function Visit-PocokPackage {
        param([string]$PackageId)
        if ($visited.Contains($PackageId)) { return }
        if (-not $visiting.Add($PackageId)) {
            throw "Package dependency cycle detected at '$PackageId'."
        }
        foreach ($dependency in $packages[$PackageId].InternalDependencies) {
            Visit-PocokPackage -PackageId $dependency
        }
        $visiting.Remove($PackageId) | Out-Null
        $visited.Add($PackageId) | Out-Null
    }
    foreach ($packageId in $packages.Keys) {
        Visit-PocokPackage -PackageId $packageId
    }

    return $packages
}

function Get-PocokForwardClosure {
    param(
        [Parameter(Mandatory)] [AllowEmptyCollection()] [string[]]$Seeds,
        [Parameter(Mandatory)] $Projects
    )

    $visited = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $pending = [System.Collections.Generic.Stack[string]]::new()
    foreach ($seed in $Seeds) {
        if ($Projects.Contains($seed)) { $pending.Push($seed) }
    }
    while ($pending.Count -gt 0) {
        $current = $pending.Pop()
        if (-not $visited.Add($current)) { continue }
        foreach ($reference in $Projects[$current].References) {
            $pending.Push($reference)
        }
    }
    return @($visited | Sort-Object)
}

function Get-PocokReverseProjectClosure {
    param(
        [Parameter(Mandatory)] [AllowEmptyCollection()] [string[]]$Seeds,
        [Parameter(Mandatory)] $Projects
    )

    $reverse = @{}
    foreach ($path in $Projects.Keys) { $reverse[$path] = [System.Collections.Generic.List[string]]::new() }
    foreach ($project in $Projects.Values) {
        foreach ($reference in $project.References) {
            $reverse[$reference].Add($project.Path)
        }
    }
    $visited = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $pending = [System.Collections.Generic.Queue[string]]::new()
    foreach ($seed in $Seeds) {
        if ($Projects.Contains($seed)) { $pending.Enqueue($seed) }
    }
    while ($pending.Count -gt 0) {
        $current = $pending.Dequeue()
        if (-not $visited.Add($current)) { continue }
        foreach ($dependent in $reverse[$current]) { $pending.Enqueue($dependent) }
    }
    return @($visited | Sort-Object)
}

function Get-PocokReversePackageClosure {
    param(
        [Parameter(Mandatory)] [AllowEmptyCollection()] [string[]]$Seeds,
        [Parameter(Mandatory)] $Packages
    )

    $reverse = @{}
    foreach ($id in $Packages.Keys) { $reverse[$id] = [System.Collections.Generic.List[string]]::new() }
    foreach ($package in $Packages.Values) {
        foreach ($dependency in $package.InternalDependencies) {
            $reverse[$dependency].Add($package.Id)
        }
    }
    $visited = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $pending = [System.Collections.Generic.Queue[string]]::new()
    foreach ($seed in $Seeds) {
        if ($Packages.Contains($seed)) { $pending.Enqueue($seed) }
    }
    while ($pending.Count -gt 0) {
        $current = $pending.Dequeue()
        if (-not $visited.Add($current)) { continue }
        foreach ($dependent in $reverse[$current]) { $pending.Enqueue($dependent) }
    }
    return @($visited | Sort-Object)
}

function Get-PocokPackageDependencyClosure {
    param(
        [Parameter(Mandatory)] [AllowEmptyCollection()] [string[]]$Seeds,
        [Parameter(Mandatory)] $Packages
    )

    $visited = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    function Visit-PocokClosurePackage {
        param([string]$PackageId)
        if (-not $Packages.Contains($PackageId) -or -not $visited.Add($PackageId)) { return }
        foreach ($dependency in $Packages[$PackageId].InternalDependencies) {
            Visit-PocokClosurePackage -PackageId $dependency
        }
    }
    foreach ($seed in $Seeds) { Visit-PocokClosurePackage -PackageId $seed }
    return @($visited | Sort-Object)
}

function Get-PocokTestOwnership {
    param(
        [Parameter(Mandatory)] $Projects,
        [Parameter(Mandatory)] $Packages
    )

    $packageByProject = @{}
    foreach ($package in $Packages.Values | Where-Object State -ne 'Retired') {
        $packageByProject[$package.Project] = $package.Id
    }
    $ownership = [ordered]@{}
    foreach ($project in $Projects.Values | Where-Object { $_.IsTestProject -and $_.Kind -eq 'Test' }) {
        if ($project.Path -eq 'tests/Architecture/Pocok.Architecture.Tests.csproj') {
            $ownership[$project.Path] = [pscustomobject]@{ Kind = 'RepositoryWide'; PackageId = $null; SourceProject = $null }
            continue
        }
        if ($project.Path -eq 'tests/Packaging/Pocok.Packaging.Tests.csproj') {
            $ownership[$project.Path] = [pscustomobject]@{ Kind = 'RepositoryWide'; PackageId = $null; SourceProject = $null }
            continue
        }

        $sourceReferences = @($project.References | Where-Object { $Projects[$_].Kind -eq 'Source' })
        $nameStem = $project.Name -replace '^Pocok\.', '' -replace '\.Integration\.Tests$', '' -replace '\.Tests$', ''
        $matching = @($sourceReferences | Where-Object {
            ($Projects[$_].Name -replace '^Pocok\.', '') -eq $nameStem
        })
        $ownerProject = if (@($matching).Length -eq 1) {
            $matching[0]
        }
        elseif (@($sourceReferences).Length -eq 1) {
            $sourceReferences[0]
        }
        else {
            $null
        }

        if ($null -eq $ownerProject -or -not $packageByProject.ContainsKey($ownerProject)) {
            throw "Test project '$($project.Path)' has no resolvable owning package slice."
        }
        $ownership[$project.Path] = [pscustomobject]@{
            Kind = 'Slice'
            PackageId = $packageByProject[$ownerProject]
            SourceProject = $ownerProject
        }
    }
    return $ownership
}

function Get-PocokProjectForPath {
    param(
        [Parameter(Mandatory)] [string]$Path,
        [Parameter(Mandatory)] $Projects
    )

    $matches = @($Projects.Values | Where-Object {
        $Path -eq $_.Path -or $Path.StartsWith("$($_.Directory)/", [StringComparison]::Ordinal)
    } | Sort-Object { $_.Directory.Length } -Descending)
    if (@($matches).Length -eq 0) { return $null }
    return $matches[0]
}

function Test-PocokDocumentationPath {
    param(
        [Parameter(Mandatory)] [string]$Path,
        [Parameter(Mandatory)] $Projects
    )

    $project = Get-PocokProjectForPath -Path $Path -Projects $Projects
    if ($null -ne $project -and $project.Kind -eq 'Source' -and [System.IO.Path]::GetFileName($Path) -eq 'README.md') {
        return $false
    }
    return $Path -match '^(docs|prompts|sessions)/' -or
        $Path -match '^[^/]+\.md$' -or
        $Path -eq '.github/pull_request_template.md'
}

function Get-PocokFullTriggerReason {
    param([Parameter(Mandatory)] [string]$Path)

    $exact = @(
        'global.json',
        'Directory.Build.props',
        'Directory.Build.targets',
        'Directory.Packages.props',
        'Pocok.slnx',
        'Pocok.Core.slnx',
        '.editorconfig',
        '.gitattributes',
        'eng/packages.json',
        'eng/packages.schema.json',
        'eng/coverage.runsettings',
        'LICENSE',
        'NOTICE',
        'SECURITY.md',
        'STEWARDSHIP.md',
        'THIRD-PARTY-NOTICES.md'
    )
    if ($exact -contains $Path) { return "Global CI input changed: $Path" }
    foreach ($prefix in @('.github/workflows/', 'tools/PackageCatalog/', 'tools/PackageMetadata/', 'tools/Ci/')) {
        if ($Path.StartsWith($prefix, [StringComparison]::Ordinal)) {
            return "Global CI policy or tooling changed: $Path"
        }
    }
    return $null
}

function New-PocokEmergencyFullPlan {
    param(
        [Parameter(Mandatory)] [string]$RepositoryRoot,
        [Parameter(Mandatory)] [object[]]$Reasons,
        [string[]]$ChangedFiles = @(),
        [string]$BaseSha,
        [string]$HeadSha
    )

    $root = [System.IO.Path]::GetFullPath($RepositoryRoot)
    $projectRecords = [System.Collections.Generic.List[object]]::new()
    $projectFiles = @(
        Get-ChildItem -LiteralPath $root -Recurse -File -Filter '*.csproj' |
            Where-Object {
                $relativePath = ConvertTo-PocokPath -Path $_.FullName -RepositoryRoot $root
                $relativePath -notmatch '(^|/)(bin|obj|artifacts|\.git)(/|$)'
            } |
            Sort-Object FullName
    )
    Write-Host "[CI tooling] Building emergency full plan from $(@($projectFiles).Length) project file(s)."
    foreach ($file in $projectFiles) {
        $path = ConvertTo-PocokPath -Path $file.FullName -RepositoryRoot $root
        $outputType = 'Library'
        try {
            [xml]$document = Get-Content -LiteralPath $file.FullName -Raw
            $outputTypeNode = $document.SelectSingleNode('//*[local-name()="OutputType"]')
            if ($null -ne $outputTypeNode) { $outputType = [string]$outputTypeNode.InnerText }
        }
        catch {
            Write-Warning "[CI tooling] Could not inspect project '$path' while constructing the emergency full plan: $($_.Exception.Message)"
            # Full validation will surface malformed project content. Planning must not select less work.
        }
        $name = [System.IO.Path]::GetFileNameWithoutExtension($path)
        $projectRecords.Add([pscustomobject]@{
            Path = $path
            Kind = Get-PocokProjectKind -Path $path
            Name = $name
            IsTestProject = $name.EndsWith('.Tests', [StringComparison]::Ordinal)
            OutputType = $outputType
        })
    }

    $activePackages = [System.Collections.Generic.List[object]]::new()
    try {
        $catalog = Get-Content -LiteralPath (Join-Path $root 'eng/packages.json') -Raw | ConvertFrom-Json
        foreach ($package in @($catalog.packages)) {
            if ([string]$package.state -eq 'Retired') { continue }
            $activePackages.Add([pscustomobject]@{
                Id = [string]$package.id
                Project = ([string]$package.project -replace '\\', '/')
            })
        }
    }
    catch {
        $catalogError = "Package catalog could not be read while constructing the full fallback: $($_.Exception.Message)"
        $Reasons += $catalogError
        Write-Warning "[CI tooling] $catalogError"
    }

    $tests = @($projectRecords | Where-Object { $_.IsTestProject -and $_.Kind -eq 'Test' } | Sort-Object Path)
    $samples = @($projectRecords | Where-Object Kind -eq 'Sample' | Sort-Object Path)
    $packageIds = @(
        $activePackages |
            ForEach-Object { $_.Id } |
            Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } |
            Sort-Object -Unique
    )
    $packageProjects = @(
        $activePackages |
            ForEach-Object { $_.Project } |
            Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } |
            Sort-Object -Unique
    )
    $testPaths = @($tests | ForEach-Object { $_.Path } | Sort-Object -Unique)
    $samplePaths = @($samples | ForEach-Object { $_.Path } | Sort-Object -Unique)
    $projectPaths = @($projectRecords | ForEach-Object { $_.Path } | Sort-Object -Unique)

    Write-Host "[CI tooling] Emergency full-plan inventory: $(@($projectPaths).Length) project(s), $(@($testPaths).Length) test project(s), $(@($samplePaths).Length) sample project(s), $(@($packageIds).Length) active package(s)."
    if (@($packageIds).Length -eq 0) {
        Write-Warning "[CI tooling] No active packages were discoverable for emergency planning under '$root'. Package operations will remain disabled, while all discovered projects stay selected for validation."
    }

    return [pscustomobject]@{
        schemaVersion = 1
        mode = 'Full'
        baseSha = $BaseSha
        headSha = $HeadSha
        reasons = @($Reasons | Sort-Object -Unique)
        changedFiles = @($ChangedFiles | Sort-Object -Unique)
        affectedPackageIds = $packageIds
        affectedSourceProjects = @($projectRecords | Where-Object Kind -eq 'Source' | ForEach-Object Path | Sort-Object)
        affectedTestProjects = $testPaths
        affectedSampleProjects = $samplePaths
        affectedRunnableSampleProjects = @($samples | Where-Object { $_.OutputType -eq 'Exe' -and $_.Path -ne 'samples/Conversion.Trimmed/Pocok.Conversion.Trimmed.csproj' } | ForEach-Object Path | Sort-Object)
        affectedBenchmarkProjects = @($projectRecords | Where-Object Kind -eq 'Benchmark' | ForEach-Object Path | Sort-Object)
        affectedSmokePackageIds = $packageIds
        affectedAuditPackageIds = $packageIds
        packageIdsToPack = $packageIds
        packageProjectsToPack = $packageProjects
        validationProjects = $projectPaths
        coverageSlices = @()
        runArchitectureTests = $true
        runPackagingTests = $true
        runPackageMetadataTests = $true
        runPack = @($packageProjects).Length -gt 0
        runPublicAudit = @($packageIds).Length -gt 0
        runTrimmedConversion = $true
    }
}

function New-PocokCiPlan {
    param(
        [Parameter(Mandatory)] [AllowEmptyCollection()] [object[]]$Changes,
        [string]$RepositoryRoot = (Get-PocokRepositoryRoot),
        [ValidateSet('pull_request', 'push', 'workflow_dispatch', 'local')] [string]$EventName = 'local',
        [switch]$ForceFull,
        [string]$BaseSha,
        [string]$HeadSha
    )

    $root = [System.IO.Path]::GetFullPath($RepositoryRoot)
    Write-Host "[CI tooling] Creating $EventName impact plan for '$root'."
    $reasons = [System.Collections.Generic.List[string]]::new()
    try {
        $projects = Get-PocokProjectModel -RepositoryRoot $root
        $packages = Get-PocokPackageModel -RepositoryRoot $root -Projects $projects
        $ownership = Get-PocokTestOwnership -Projects $projects -Packages $packages
    }
    catch {
        Write-Warning "[CI tooling] Repository graph validation failed for '$root': $($_.Exception.Message)"
        $reasons.Add("Repository graph could not be proven safe: $($_.Exception.Message)")
        $fallbackChangedFiles = @(
            $Changes | ForEach-Object { @($_.Path, $_.OldPath) } |
                Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } |
                ForEach-Object { ([string]$_ -replace '\\', '/').TrimStart([char[]]'./') } |
                Sort-Object -Unique
        )
        return New-PocokEmergencyFullPlan -RepositoryRoot $root -Reasons @($reasons) -ChangedFiles $fallbackChangedFiles -BaseSha $BaseSha -HeadSha $HeadSha
    }

    $normalizedChanges = @($Changes | ForEach-Object {
        [pscustomobject]@{
            Status = if ($_.Status) { [string]$_.Status } else { 'M' }
            Path = ConvertTo-PocokPath -Path ([string]$_.Path) -RepositoryRoot $root
            OldPath = if ($_.OldPath) { ConvertTo-PocokPath -Path ([string]$_.OldPath) -RepositoryRoot $root } else { $null }
        }
    })
    $changedFiles = @(
        $normalizedChanges | ForEach-Object { @($_.Path, $_.OldPath) } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            Sort-Object -Unique
    )

    if ($EventName -eq 'push') {
        $reasons.Add('Pushes to main always use full validation.')
    }
    if ($ForceFull) {
        $reasons.Add('Full validation was explicitly requested.')
    }
    foreach ($path in $changedFiles) {
        $reason = Get-PocokFullTriggerReason -Path $path
        if ($reason) { $reasons.Add($reason) }
    }
    foreach ($change in $normalizedChanges) {
        if ($change.Status.StartsWith('D', [StringComparison]::Ordinal) -and $change.Path.EndsWith('.csproj', [StringComparison]::OrdinalIgnoreCase)) {
            $reasons.Add("Deleted project definition requires full validation: $($change.Path)")
        }
    }
    if ($reasons.Count -gt 0) {
        return New-PocokFullPlan -RepositoryRoot $root -Reasons $reasons -ChangedFiles $changedFiles -BaseSha $BaseSha -HeadSha $HeadSha -Projects $projects -Packages $packages -Ownership $ownership
    }

    if (@($changedFiles).Length -eq 0) {
        $reasons.Add('No changed files were available.')
        return New-PocokFullPlan -RepositoryRoot $root -Reasons $reasons -ChangedFiles $changedFiles -BaseSha $BaseSha -HeadSha $HeadSha -Projects $projects -Packages $packages -Ownership $ownership
    }

    if (@($changedFiles | Where-Object { -not (Test-PocokDocumentationPath -Path $_ -Projects $projects) }).Length -eq 0) {
        return [pscustomobject]@{
            schemaVersion = 1
            mode = 'DocumentationOnly'
            baseSha = $BaseSha
            headSha = $HeadSha
            reasons = @('All changed paths are ordinary repository documentation.')
            changedFiles = $changedFiles
            affectedPackageIds = @()
            affectedSourceProjects = @()
            affectedTestProjects = @()
            affectedSampleProjects = @()
            affectedRunnableSampleProjects = @()
            affectedBenchmarkProjects = @()
            affectedSmokePackageIds = @()
            affectedAuditPackageIds = @()
            packageIdsToPack = @()
            packageProjectsToPack = @()
            validationProjects = @()
            coverageSlices = @()
            runArchitectureTests = $false
            runPackagingTests = $false
            runPackageMetadataTests = $true
            runPack = $false
            runPublicAudit = $false
            runTrimmedConversion = $false
        }
    }

    $unknownRelevant = [System.Collections.Generic.List[string]]::new()
    $directSourceProjects = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $directTests = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $directSamples = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $directBenchmarks = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $directFixtures = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $artifactPackageIds = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $smokeOnlyPackageIds = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)

    $packageByProject = @{}
    foreach ($package in $packages.Values | Where-Object State -ne 'Retired') { $packageByProject[$package.Project] = $package.Id }
    $packageByAssembly = @{}
    foreach ($package in $packages.Values | Where-Object State -ne 'Retired') {
        $packageByAssembly[$projects[$package.Project].AssemblyName] = $package.Id
        $packageByAssembly[$package.Id] = $package.Id
    }

    foreach ($path in $changedFiles) {
        if ($path.StartsWith('tests/Packaging/', [StringComparison]::Ordinal) -and $path -match 'PublicApiTests\.PublicApiMatchesSnapshot_assembly=(?<assembly>.+)\.verified\.txt$') {
            $assembly = $Matches['assembly']
            $candidate = "Pocok.$assembly"
            if ($packageByAssembly.ContainsKey($candidate)) {
                $artifactPackageIds.Add($packageByAssembly[$candidate]) | Out-Null
                continue
            }
        }

        $project = Get-PocokProjectForPath -Path $path -Projects $projects
        if ($null -ne $project) {
            switch ($project.Kind) {
                'Source' {
                    $directSourceProjects.Add($project.Path) | Out-Null
                    if ([System.IO.Path]::GetFileName($path) -eq 'README.md' -and $packageByProject.ContainsKey($project.Path)) {
                        $artifactPackageIds.Add($packageByProject[$project.Path]) | Out-Null
                    }
                }
                'Test' { $directTests.Add($project.Path) | Out-Null }
                'Sample' { $directSamples.Add($project.Path) | Out-Null }
                'Benchmark' { $directBenchmarks.Add($project.Path) | Out-Null }
                'Fixture' { $directFixtures.Add($project.Path) | Out-Null }
            }
            continue
        }

        $linkedProjects = @($projects.Values | Where-Object { $_.CompileIncludes -contains $path })
        if (@($linkedProjects).Length -gt 0) {
            foreach ($linkedProject in $linkedProjects) {
                if ($linkedProject.Kind -eq 'Source') { $directSourceProjects.Add($linkedProject.Path) | Out-Null }
                elseif ($linkedProject.Kind -eq 'Test') { $directTests.Add($linkedProject.Path) | Out-Null }
            }
            continue
        }

        $matchedConsumer = $false
        foreach ($package in $packages.Values | Where-Object State -ne 'Retired') {
            $consumerPrefix = "tools/PackageSmoke/$($package.Consumer)/"
            if ($path.StartsWith($consumerPrefix, [StringComparison]::Ordinal)) {
                $smokeOnlyPackageIds.Add($package.Id) | Out-Null
                $matchedConsumer = $true
                break
            }
        }
        if ($matchedConsumer) { continue }

        if (Test-PocokDocumentationPath -Path $path -Projects $projects) { continue }
        if ($path -match '\.(cs|csproj|props|targets|json|ya?ml|ps1)$' -or $path -match '(?i)(package|nuget|publicapi)') {
            $unknownRelevant.Add($path)
        }
    }

    if ($unknownRelevant.Count -gt 0) {
        $reasons.Add("Relevant changed paths could not be mapped safely: $($unknownRelevant -join ', ')")
        return New-PocokFullPlan -RepositoryRoot $root -Reasons $reasons -ChangedFiles $changedFiles -BaseSha $BaseSha -HeadSha $HeadSha -Projects $projects -Packages $packages -Ownership $ownership
    }

    $affectedSourceProjects = @(
        Get-PocokReverseProjectClosure -Seeds @($directSourceProjects) -Projects $projects |
            Where-Object { $projects[$_].Kind -eq 'Source' } |
            Sort-Object -Unique
    )
    $sourcePackageIds = @($affectedSourceProjects | Where-Object { $packageByProject.ContainsKey($_) } | ForEach-Object { $packageByProject[$_] })
    $affectedFromSource = @(Get-PocokReversePackageClosure -Seeds $sourcePackageIds -Packages $packages)
    $affectedFromArtifacts = @(Get-PocokReversePackageClosure -Seeds @($artifactPackageIds) -Packages $packages)

    $directTestOwnerPackageIds = @(
        $directTests | Where-Object { $ownership.Contains($_) -and $ownership[$_].Kind -eq 'Slice' } |
            ForEach-Object { $ownership[$_].PackageId }
    )
    $affectedPackageIds = @(
        @($affectedFromSource) + @($affectedFromArtifacts) + @($smokeOnlyPackageIds) + @($directTestOwnerPackageIds) |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            Sort-Object -Unique
    )

    $selectedTests = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($test in $directTests) { $selectedTests.Add($test) | Out-Null }
    foreach ($test in $projects.Values | Where-Object { $_.IsTestProject -and $_.Kind -eq 'Test' }) {
        if ($ownership[$test.Path].Kind -eq 'Slice' -and $affectedPackageIds -contains $ownership[$test.Path].PackageId) {
            $selectedTests.Add($test.Path) | Out-Null
            continue
        }
        $closure = Get-PocokForwardClosure -Seeds @($test.Path) -Projects $projects
        if (@($closure | Where-Object { $affectedSourceProjects -contains $_ }).Length -gt 0) {
            $selectedTests.Add($test.Path) | Out-Null
        }
        if (@($closure | Where-Object { $directFixtures.Contains($_) }).Length -gt 0) {
            $selectedTests.Add($test.Path) | Out-Null
        }
    }

    $runArchitectureTests = $directSourceProjects.Count -gt 0 -or $directFixtures.Count -gt 0
    $runPackagingTests = @($sourcePackageIds).Length -gt 0 -or $artifactPackageIds.Count -gt 0 -or $smokeOnlyPackageIds.Count -gt 0
    if ($runArchitectureTests) { $selectedTests.Add('tests/Architecture/Pocok.Architecture.Tests.csproj') | Out-Null }
    if ($runPackagingTests) { $selectedTests.Add('tests/Packaging/Pocok.Packaging.Tests.csproj') | Out-Null }

    $selectedSamples = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($sample in $directSamples) { $selectedSamples.Add($sample) | Out-Null }
    foreach ($sample in $projects.Values | Where-Object Kind -eq 'Sample') {
        $closure = Get-PocokForwardClosure -Seeds @($sample.Path) -Projects $projects
        if (@($closure | Where-Object { $affectedSourceProjects -contains $_ }).Length -gt 0) {
            $selectedSamples.Add($sample.Path) | Out-Null
        }
    }

    $selectedBenchmarks = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($benchmark in $directBenchmarks) { $selectedBenchmarks.Add($benchmark) | Out-Null }
    foreach ($benchmark in $projects.Values | Where-Object Kind -eq 'Benchmark') {
        $closure = Get-PocokForwardClosure -Seeds @($benchmark.Path) -Projects $projects
        if (@($closure | Where-Object { $affectedSourceProjects -contains $_ }).Length -gt 0) {
            $selectedBenchmarks.Add($benchmark.Path) | Out-Null
        }
    }

    $packageImpactSeeds = @(@($affectedFromSource) + @($affectedFromArtifacts) + @($smokeOnlyPackageIds) | Sort-Object -Unique)
    $packageIdsToPack = @(
        if (@($packageImpactSeeds).Length -gt 0) {
            Get-PocokPackageDependencyClosure -Seeds $packageImpactSeeds -Packages $packages
        }
    )
    $packageProjectsToPack = @($packageIdsToPack | ForEach-Object { $packages[$_].Project } | Sort-Object -Unique)
    $runPack = @($packageIdsToPack).Length -gt 0

    $validationProjects = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($path in @($affectedSourceProjects) + @($selectedTests) + @($selectedSamples) + @($selectedBenchmarks) + @($packageProjectsToPack)) {
        if ($projects.Contains($path)) {
            foreach ($dependency in Get-PocokForwardClosure -Seeds @($path) -Projects $projects) {
                $validationProjects.Add($dependency) | Out-Null
            }
        }
    }

    $coverageSlices = @(
        foreach ($packageId in $affectedPackageIds) {
            $package = $packages[$packageId]
            $tests = @($ownership.Keys | Where-Object {
                $ownership[$_].Kind -eq 'Slice' -and $ownership[$_].PackageId -eq $packageId -and $selectedTests.Contains($_)
            } | Sort-Object)
            if (@($tests).Length -gt 0) {
                [pscustomobject]@{
                    packageId = $packageId
                    sourceProject = $package.Project
                    sourceRoot = $projects[$package.Project].Directory
                    assemblyName = $projects[$package.Project].AssemblyName
                    testProjects = $tests
                }
            }
        }
    )

    $runnableSamples = @($selectedSamples | Where-Object {
        $projects[$_].OutputType -eq 'Exe' -and $_ -ne 'samples/Conversion.Trimmed/Pocok.Conversion.Trimmed.csproj'
    } | Sort-Object)
    $runTrimmed = $selectedSamples.Contains('samples/Conversion.Trimmed/Pocok.Conversion.Trimmed.csproj') -or $affectedPackageIds -contains 'Pocok.Conversion'

    return [pscustomobject]@{
        schemaVersion = 1
        mode = 'Partial'
        baseSha = $BaseSha
        headSha = $HeadSha
        reasons = @('Selected from changed paths and reverse transitive project/package dependencies.')
        changedFiles = $changedFiles
        affectedPackageIds = $affectedPackageIds
        affectedSourceProjects = $affectedSourceProjects
        affectedTestProjects = @($selectedTests | Sort-Object)
        affectedSampleProjects = @($selectedSamples | Sort-Object)
        affectedRunnableSampleProjects = $runnableSamples
        affectedBenchmarkProjects = @($selectedBenchmarks | Sort-Object)
        affectedSmokePackageIds = @($packageImpactSeeds | Sort-Object)
        affectedAuditPackageIds = @(@($affectedFromSource) + @($affectedFromArtifacts) | Sort-Object -Unique)
        packageIdsToPack = $packageIdsToPack
        packageProjectsToPack = $packageProjectsToPack
        validationProjects = @($validationProjects | Sort-Object)
        coverageSlices = $coverageSlices
        runArchitectureTests = $runArchitectureTests
        runPackagingTests = $runPackagingTests
        runPackageMetadataTests = $true
        runPack = $runPack
        runPublicAudit = @(@($affectedFromSource) + @($affectedFromArtifacts)).Length -gt 0
        runTrimmedConversion = $runTrimmed
    }
}

function New-PocokFullPlan {
    param(
        [Parameter(Mandatory)] [string]$RepositoryRoot,
        [Parameter(Mandatory)] [System.Collections.Generic.List[string]]$Reasons,
        [string[]]$ChangedFiles = @(),
        [string]$BaseSha,
        [string]$HeadSha,
        $Projects,
        $Packages,
        $Ownership
    )

    if ($null -eq $Projects) { $Projects = Get-PocokProjectModel -RepositoryRoot $RepositoryRoot }
    if ($null -eq $Packages) { $Packages = Get-PocokPackageModel -RepositoryRoot $RepositoryRoot -Projects $Projects }
    if ($null -eq $Ownership) { $Ownership = Get-PocokTestOwnership -Projects $Projects -Packages $Packages }
    $activePackages = @($Packages.Values | Where-Object State -ne 'Retired' | Sort-Object Id)
    $packageIds = @($activePackages.Id)
    $tests = @($Projects.Values | Where-Object { $_.IsTestProject -and $_.Kind -eq 'Test' } | Sort-Object Path)
    $samples = @($Projects.Values | Where-Object Kind -eq 'Sample' | Sort-Object Path)
    $coverageSlices = @(
        foreach ($package in $activePackages) {
            $ownedTests = @($Ownership.Keys | Where-Object { $Ownership[$_].Kind -eq 'Slice' -and $Ownership[$_].PackageId -eq $package.Id } | Sort-Object)
            if (@($ownedTests).Length -gt 0) {
                [pscustomobject]@{
                    packageId = $package.Id
                    sourceProject = $package.Project
                    sourceRoot = $Projects[$package.Project].Directory
                    assemblyName = $Projects[$package.Project].AssemblyName
                    testProjects = $ownedTests
                }
            }
        }
    )

    return [pscustomobject]@{
        schemaVersion = 1
        mode = 'Full'
        baseSha = $BaseSha
        headSha = $HeadSha
        reasons = @($Reasons | Sort-Object -Unique)
        changedFiles = @($ChangedFiles | Sort-Object -Unique)
        affectedPackageIds = $packageIds
        affectedSourceProjects = @($Projects.Values | Where-Object Kind -eq 'Source' | ForEach-Object Path | Sort-Object)
        affectedTestProjects = @($tests.Path)
        affectedSampleProjects = @($samples.Path)
        affectedRunnableSampleProjects = @($samples | Where-Object { $_.OutputType -eq 'Exe' -and $_.Path -ne 'samples/Conversion.Trimmed/Pocok.Conversion.Trimmed.csproj' } | ForEach-Object Path)
        affectedBenchmarkProjects = @($Projects.Values | Where-Object Kind -eq 'Benchmark' | ForEach-Object Path | Sort-Object)
        affectedSmokePackageIds = $packageIds
        affectedAuditPackageIds = $packageIds
        packageIdsToPack = $packageIds
        packageProjectsToPack = @($activePackages.Project | Sort-Object -Unique)
        validationProjects = @($Projects.Keys | Sort-Object)
        coverageSlices = $coverageSlices
        runArchitectureTests = $true
        runPackagingTests = $true
        runPackageMetadataTests = $true
        runPack = $true
        runPublicAudit = $true
        runTrimmedConversion = $true
    }
}

function Invoke-PocokCommand {
    param(
        [Parameter(Mandatory)] [string]$FilePath,
        [Parameter(Mandatory)] [string[]]$Arguments,
        [string]$WorkingDirectory = (Get-Location).Path
    )

    Write-Host "> $FilePath $($Arguments -join ' ')"
    Push-Location $WorkingDirectory
    try {
        & $FilePath @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
        }
    }
    finally {
        Pop-Location
    }
}

function Get-PocokWorkflowActionPinViolations {
    [CmdletBinding()]
    param([Parameter(Mandatory)] [string]$WorkflowRoot)

    $resolvedRoot = [IO.Path]::GetFullPath($WorkflowRoot)
    if (-not (Test-Path -LiteralPath $resolvedRoot -PathType Container)) {
        throw "Workflow root does not exist: $resolvedRoot"
    }

    $violations = [Collections.Generic.List[object]]::new()
    $workflowFiles = @(
        Get-ChildItem -LiteralPath $resolvedRoot -File -Recurse |
            Where-Object { $_.Extension -in @('.yml', '.yaml') } |
            Sort-Object FullName
    )

    foreach ($workflowFile in $workflowFiles) {
        $lines = [IO.File]::ReadAllLines($workflowFile.FullName)
        for ($index = 0; $index -lt $lines.Length; $index++) {
            if ($lines[$index] -notmatch '^\s*(?:-\s*)?uses:\s*(?<reference>\S+)') {
                continue
            }

            $reference = $Matches.reference.Trim([char[]]@([char]39, [char]34))
            if ($reference.StartsWith('./', [StringComparison]::Ordinal)) {
                continue
            }

            if ($reference -match '^[^/@\s]+/[^@\s]+@[0-9a-fA-F]{40}$') {
                continue
            }

            $relativePath = [IO.Path]::GetRelativePath($resolvedRoot, $workflowFile.FullName).Replace('\', '/')
            $violations.Add([pscustomobject]@{
                path = $relativePath
                line = $index + 1
                reference = $reference
            })
        }
    }

    return @($violations)
}

Export-ModuleMember -Function @(
    'ConvertTo-PocokPath',
    'Get-PocokWorkflowActionPinViolations',
    'Get-PocokForwardClosure',
    'Get-PocokPackageDependencyClosure',
    'Get-PocokPackageModel',
    'Get-PocokProjectModel',
    'Get-PocokRepositoryRoot',
    'Get-PocokReversePackageClosure',
    'Get-PocokReverseProjectClosure',
    'Get-PocokTestOwnership',
    'Invoke-PocokCommand',
    'New-PocokCiPlan',
    'Resolve-PocokPath',
    'Write-PocokJson'
)
