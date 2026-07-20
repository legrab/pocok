# SPDX-License-Identifier: Apache-2.0
# Copyright 2026 Pocok contributors

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module (Join-Path $PSScriptRoot 'Pocok.Ci.psm1') -Force

$repositoryRoot = Get-PocokRepositoryRoot -StartPath $PSScriptRoot
$passed = 0

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
    $script:passed++
}

function Assert-SequenceEqual {
    param([object[]]$Actual, [object[]]$Expected, [string]$Message)
    $actualText = @($Actual) -join "`n"
    $expectedText = @($Expected) -join "`n"
    Assert-True -Condition ($actualText -ceq $expectedText) -Message "$Message`nExpected:`n$expectedText`nActual:`n$actualText"
}

function Get-Plan {
    param([object[]]$Changes, [string]$EventName = 'pull_request', [switch]$ForceFull)

    $paths = @(
        $Changes | ForEach-Object { @($_.OldPath, $_.Path) } |
            Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) }
    )
    Write-Host "`n[CI tooling test] Planning: $($paths -join ', ')"
    try {
        $plan = New-PocokCiPlan -Changes $Changes -RepositoryRoot $repositoryRoot -EventName $EventName -ForceFull:$ForceFull -BaseSha 'base' -HeadSha 'head'
        Write-Host "[CI tooling test] Result: $($plan.mode); packages: $(@($plan.affectedPackageIds) -join ', '); tests: $(@($plan.affectedTestProjects) -join ', ')"
        return $plan
    }
    catch {
        Write-Host "[CI tooling test] Planning failed for: $($paths -join ', '): $($_.Exception.Message)"
        throw
    }
}

function Change {
    param([string]$Path, [string]$Status = 'M', [string]$OldPath)
    return [pscustomobject]@{ Path = $Path; Status = $Status; OldPath = $OldPath }
}

$plan = Get-Plan -Changes @(Change 'docs/ci.md')
Assert-True ($plan.mode -eq 'DocumentationOnly') 'Documentation-only changes must skip .NET validation.'

$showcasePlan = Get-Plan -Changes @(Change 'samples/Showcase/Pocok.Showcase.Conversion/ConversionShowcaseSlice.cs')
Assert-True ($showcasePlan.mode -eq 'Partial') 'Showcase changes must remain delegated to the Showcase workflow without selecting core projects.'
Assert-True ($showcasePlan.affectedTestProjects.Count -eq 0) 'Showcase-owned tests must not enter the core package validation plan.'
Assert-True ($showcasePlan.affectedSampleProjects.Count -eq 0) 'Showcase plugins must not enter the core package-sample validation plan.'

$plan = Get-Plan -Changes @(Change 'src/BackgroundWork/Coalescing/CoalescingTaskRunner.cs')
Assert-SequenceEqual $plan.affectedPackageIds @('Pocok.BackgroundWork', 'Pocok.Localization') 'BackgroundWork must select its reverse package closure.'
Assert-True ($plan.affectedTestProjects -contains 'tests/Unit/BackgroundWork.Tests/Pocok.BackgroundWork.Tests.csproj') 'BackgroundWork tests were not selected.'
Assert-True ($plan.affectedTestProjects -contains 'tests/Unit/Localization.Tests/Pocok.Localization.Tests.csproj') 'Localization tests were not selected for a BackgroundWork source change.'

$plan = Get-Plan -Changes @(Change 'tests/Unit/BackgroundWork.Tests/Coalescing/CoalescingTaskRunnerTests.cs')
Assert-True ($plan.affectedTestProjects -contains 'tests/Unit/BackgroundWork.Tests/Pocok.BackgroundWork.Tests.csproj') 'A BackgroundWork test-only change must run BackgroundWork tests.'
Assert-True ($plan.affectedTestProjects -notcontains 'tests/Unit/Localization.Tests/Pocok.Localization.Tests.csproj') 'A BackgroundWork test-only change must not run Localization tests.'

$plan = Get-Plan -Changes @(Change 'src/Conversion/ValueConverter.cs')
Assert-SequenceEqual $plan.affectedPackageIds @('Pocok.Conversion', 'Pocok.Scripting', 'Pocok.Scripting.CSharp', 'Pocok.Scripting.JavaScript', 'Pocok.Scripting.Python', 'Pocok.Signals') 'Conversion reverse dependencies are incorrect.'

$plan = Get-Plan -Changes @(Change 'src/AppDefaults/ApplicationConfiguratorExtensions.cs')
Assert-SequenceEqual $plan.affectedPackageIds @('Pocok.AppDefaults', 'Pocok.AppDefaults.Licensing', 'Pocok.AppDefaults.Logging', 'Pocok.AppDefaults.Logging.Serilog', 'Pocok.AppDefaults.Modularity') 'AppDefaults reverse dependencies are incorrect.'

$plan = Get-Plan -Changes @(Change 'src/Modularity.Contracts/IServiceModule.cs')
Assert-SequenceEqual $plan.affectedPackageIds @('Pocok.AppDefaults.Modularity', 'Pocok.Modularity', 'Pocok.Modularity.Contracts') 'Modularity.Contracts reverse dependencies are incorrect.'

$plan = Get-Plan -Changes @(Change 'samples/Localization.Console/Program.cs')
Assert-SequenceEqual $plan.affectedSampleProjects @('samples/Localization.Console/Pocok.Localization.Console.csproj') 'A Localization sample-only change must stay sample-scoped.'
Assert-True ($plan.affectedTestProjects.Count -eq 0) 'A sample-only change must not select tests.'

$plan = Get-Plan -Changes @(Change 'src/Localization/README.md')
Assert-True ([bool]$plan.runPack -and [bool]$plan.runPublicAudit) 'A package README change must pack and audit.'
Assert-True ($plan.affectedSmokePackageIds -contains 'Pocok.Localization') 'A package README change must smoke-test its package.'

$plan = Get-Plan -Changes @(Change 'tests/Packaging/PublicApiTests.PublicApiMatchesSnapshot_assembly=Localization.verified.txt')
Assert-True ($plan.affectedPackageIds -contains 'Pocok.Localization') 'A public API snapshot must map to its package.'

$plan = Get-Plan -Changes @(Change 'tools/PackageSmoke/LocalizationConsumer/Program.cs')
Assert-SequenceEqual $plan.affectedSmokePackageIds @('Pocok.Localization') 'A package smoke consumer change must map to its package.'

$plan = Get-Plan -Changes @(Change 'tests/Fixtures/Modularity.Plugin/GreetingModule.cs')
Assert-True ($plan.affectedTestProjects -contains 'tests/Integration/Modularity.Tests/Pocok.Modularity.Integration.Tests.csproj') 'Fixture changes must select referencing integration tests.'

Assert-True ((Get-Plan -Changes @(Change 'Directory.Packages.props')).mode -eq 'Full') 'Directory.Packages.props must force full validation.'
Assert-True ((Get-Plan -Changes @(Change 'eng/coverage.runsettings')).mode -eq 'Full') 'Coverage configuration must force full validation.'
Assert-True ((Get-Plan -Changes @(Change 'SECURITY.md')).mode -eq 'Full') 'Public repository policy files used by the audit must force full validation.'
Assert-True ((Get-Plan -Changes @(Change '.github/workflows/ci.yml')).mode -eq 'Full') 'Workflow changes must force full validation.'
Assert-True ((Get-Plan -Changes @(Change 'tools/Ci/Pocok.Ci.psm1')).mode -eq 'Full') 'CI tooling changes must force full validation.'
Assert-True ((Get-Plan -Changes @(Change 'unknown/Unexpected.cs')).mode -eq 'Full') 'Unknown C# paths must force full validation.'

$plan = Get-Plan -Changes @([pscustomobject]@{ Status = 'R100'; OldPath = 'src/BackgroundWork/Old.cs'; Path = 'src/Conversion/New.cs' })
Assert-True ($plan.affectedPackageIds -contains 'Pocok.BackgroundWork' -and $plan.affectedPackageIds -contains 'Pocok.Conversion') 'Renames must classify both old and new paths.'
Assert-True ((Get-Plan -Changes @(Change 'src/Conversion/Pocok.Conversion.csproj' 'D')).mode -eq 'Full') 'Deleted project definitions must force full validation.'
Assert-True ((Get-Plan -Changes @(Change 'docs/ci.md') -ForceFull).mode -eq 'Full') 'The ci:full equivalent must force full validation.'
Assert-True ((Get-Plan -Changes @(Change 'src/Conversion/ValueConverter.cs') -EventName 'push').mode -eq 'Full') 'Pushes to main must force full validation.'

$plan = Get-Plan -Changes @(Change 'src/AppDefaults/ApplicationConfiguratorExtensions.cs')
Assert-SequenceEqual $plan.affectedPackageIds @($plan.affectedPackageIds | Sort-Object) 'Package arrays must be ordinally deterministic.'
$windowsPlan = Get-Plan -Changes @(Change 'src\Conversion\ValueConverter.cs')
Assert-SequenceEqual $windowsPlan.affectedPackageIds @('Pocok.Conversion', 'Pocok.Scripting', 'Pocok.Scripting.CSharp', 'Pocok.Scripting.JavaScript', 'Pocok.Scripting.Python', 'Pocok.Signals') 'Windows separators must normalize correctly.'

$workflowViolations = @(Get-PocokWorkflowActionPinViolations -WorkflowRoot (Join-Path $repositoryRoot '.github/workflows'))
Assert-True ($workflowViolations.Count -eq 0) "Repository workflows must pin external actions to full commit SHAs. Violations: $($workflowViolations | ConvertTo-Json -Compress)"

$tempRoot = Join-Path $repositoryRoot 'artifacts/ci-tooling-tests'
Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
try {
    $workflowRoot = Join-Path $tempRoot 'workflow-pins'
    New-Item -ItemType Directory -Path $workflowRoot -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $workflowRoot 'valid.yml') -Encoding utf8NoBOM -Value @'
jobs:
  valid:
    steps:
      - uses: actions/checkout@9c091bb21b7c1c1d1991bb908d89e4e9dddfe3e0
      - uses: "owner/action/subpath@0123456789abcdef0123456789abcdef01234567"
      - uses: ./.github/actions/local
'@
    $validWorkflowViolations = @(Get-PocokWorkflowActionPinViolations -WorkflowRoot $workflowRoot)
    Assert-True ($validWorkflowViolations.Count -eq 0) 'Full commit SHAs and repository-local actions must pass workflow pin validation.'

    Set-Content -LiteralPath (Join-Path $workflowRoot 'invalid.yaml') -Encoding utf8NoBOM -Value @'
jobs:
  invalid:
    steps:
      - uses: actions/checkout@v7
      - uses: owner/action@0123456789abcdef0123456789abcdef0123456
      - uses: docker://example/image:latest
'@
    $invalidWorkflowViolations = @(Get-PocokWorkflowActionPinViolations -WorkflowRoot $workflowRoot)
    Assert-True ($invalidWorkflowViolations.Count -eq 3) 'Floating tags, short SHAs, and non-commit external references must fail workflow pin validation.'
    Assert-SequenceEqual @($invalidWorkflowViolations.reference) @('actions/checkout@v7', 'owner/action@0123456789abcdef0123456789abcdef0123456', 'docker://example/image:latest') 'Workflow pin violations must be deterministic and actionable.'

    $cycleRoot = Join-Path $tempRoot 'cycle'
    Write-Host "`n[CI tooling test] Synthetic project-cycle repository: $cycleRoot"
    New-Item -ItemType Directory -Path (Join-Path $cycleRoot 'eng') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $cycleRoot 'src/A') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $cycleRoot 'src/B') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $cycleRoot 'eng/packages.json') -Value '{"packages":[]}' -Encoding utf8NoBOM
    Set-Content -LiteralPath (Join-Path $cycleRoot 'src/A/A.csproj') -Value '<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><ProjectReference Include="../B/B.csproj" /></ItemGroup></Project>' -Encoding utf8NoBOM
    Set-Content -LiteralPath (Join-Path $cycleRoot 'src/B/B.csproj') -Value '<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><ProjectReference Include="../A/A.csproj" /></ItemGroup></Project>' -Encoding utf8NoBOM
    $cycleFailed = $false
    try {
        Get-PocokProjectModel -RepositoryRoot $cycleRoot | Out-Null
    }
    catch {
        $cycleFailed = $true
        Write-Host "[CI tooling test] Expected cycle detected: $($_.Exception.Message)"
    }
    Assert-True $cycleFailed "Project graph cycles must fail safely from root '$cycleRoot'."
    $cyclePlan = New-PocokCiPlan -Changes @(Change 'src/A/A.csproj') -RepositoryRoot $cycleRoot -EventName pull_request
    Assert-True ($cyclePlan.mode -eq 'Full') 'A graph cycle must select emergency full validation instead of failing open.'

    $mismatchRoot = Join-Path $tempRoot 'mismatch'
    Write-Host "`n[CI tooling test] Synthetic package-mismatch repository: $mismatchRoot"
    New-Item -ItemType Directory -Path (Join-Path $mismatchRoot 'eng') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $mismatchRoot 'src/A') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $mismatchRoot 'src/B') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $mismatchRoot 'src/A/Pocok.A.csproj') -Value '<Project Sdk="Microsoft.NET.Sdk" />' -Encoding utf8NoBOM
    Set-Content -LiteralPath (Join-Path $mismatchRoot 'src/B/Pocok.B.csproj') -Value '<Project Sdk="Microsoft.NET.Sdk" />' -Encoding utf8NoBOM
    $catalogJson = '{"packages":[{"id":"Pocok.A","project":"src/A/Pocok.A.csproj","state":"Active","releasable":true,"consumer":"AConsumer","internalDependencies":["Pocok.B"]},{"id":"Pocok.B","project":"src/B/Pocok.B.csproj","state":"Active","releasable":true,"consumer":"BConsumer","internalDependencies":[]}]}'
    Set-Content -LiteralPath (Join-Path $mismatchRoot 'eng/packages.json') -Value $catalogJson -Encoding utf8NoBOM
    $mismatchFailed = $false
    try {
        $syntheticProjects = Get-PocokProjectModel -RepositoryRoot $mismatchRoot
        Get-PocokPackageModel -RepositoryRoot $mismatchRoot -Projects $syntheticProjects | Out-Null
    }
    catch { $mismatchFailed = $true }
    Assert-True $mismatchFailed 'Catalog and project dependency mismatches must fail safely.'
    $mismatchPlan = New-PocokCiPlan -Changes @(Change 'src/A/Pocok.A.csproj') -RepositoryRoot $mismatchRoot -EventName pull_request
    Assert-True ($mismatchPlan.mode -eq 'Full') 'A catalog mismatch must select emergency full validation instead of failing open.'

    $coverageRoot = Join-Path $tempRoot 'coverage'
    $headRoot = Join-Path $coverageRoot 'head/Fake.Tests'
    New-Item -ItemType Directory -Path (Join-Path $headRoot 'one') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $headRoot 'two') -Force | Out-Null
    $reportOne = '<?xml version="1.0"?><coverage><packages><package><classes><class filename="src/Fake/File.cs"><lines><line number="1" hits="1" branch="true"><conditions><condition number="0" type="jump" coverage="100%"/><condition number="1" type="jump" coverage="0%"/></conditions></line><line number="2" hits="0"/></lines></class><class filename="src/Other/Ignored.cs"><lines><line number="1" hits="1"/></lines></class></classes></package></packages></coverage>'
    $reportTwo = '<?xml version="1.0"?><coverage><packages><package><classes><class filename="src/Fake/File.cs"><lines><line number="1" hits="0" branch="true"><conditions><condition number="0" type="jump" coverage="0%"/><condition number="1" type="jump" coverage="100%"/></conditions></line><line number="2" hits="1"/></lines></class></classes></package></packages></coverage>'
    Set-Content -LiteralPath (Join-Path $headRoot 'one/coverage.cobertura.xml') -Value $reportOne -Encoding utf8NoBOM
    Set-Content -LiteralPath (Join-Path $headRoot 'two/coverage.cobertura.xml') -Value $reportTwo -Encoding utf8NoBOM
    $coveragePlan = [pscustomobject]@{
        baseSha = 'base'; headSha = 'head'; coverageSlices = @([pscustomobject]@{
            packageId = 'Pocok.Fake'; sourceProject = 'src/Fake/Pocok.Fake.csproj'; sourceRoot = 'src/Fake'; assemblyName = 'Pocok.Fake'; testProjects = @('tests/Fake.Tests.csproj')
        })
    }
    $coveragePlanPath = Join-Path $coverageRoot 'plan.json'
    Write-PocokJson -InputObject $coveragePlan -Path $coveragePlanPath
    $summaryJson = Join-Path $coverageRoot 'summary.json'
    $summaryMarkdown = Join-Path $coverageRoot 'summary.md'
    & (Join-Path $PSScriptRoot 'Write-CoverageSummary.ps1') -PlanPath (ConvertTo-PocokPath -Path $coveragePlanPath -RepositoryRoot $repositoryRoot) -HeadRoot (ConvertTo-PocokPath -Path (Join-Path $coverageRoot 'head') -RepositoryRoot $repositoryRoot) -BaseRoot (ConvertTo-PocokPath -Path (Join-Path $coverageRoot 'missing-base') -RepositoryRoot $repositoryRoot) -OutputJson (ConvertTo-PocokPath -Path $summaryJson -RepositoryRoot $repositoryRoot) -OutputMarkdown (ConvertTo-PocokPath -Path $summaryMarkdown -RepositoryRoot $repositoryRoot) | Out-Null
    $summary = Get-Content -LiteralPath $summaryJson -Raw | ConvertFrom-Json
    Assert-True ($summary.slices[0].head.linesCovered -eq 2 -and $summary.slices[0].head.linesTotal -eq 2) 'Coverage merging must union duplicate and complementary line hits and filter unrelated sources.'
    Assert-True ($summary.slices[0].head.branchesCovered -eq 2 -and $summary.slices[0].head.branchesTotal -eq 2) 'Coverage merging must union complementary branch conditions.'
    Assert-True ($null -eq $summary.slices[0].base) 'Unavailable base coverage must produce N/A rather than zero or failure.'

    $unreliableRoot = Join-Path $coverageRoot 'unreliable/Unreliable.Tests'
    New-Item -ItemType Directory -Path $unreliableRoot -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $unreliableRoot 'coverage.cobertura.xml') -Value '<?xml version="1.0"?><coverage><packages><package><classes><class filename="src/Unreliable/File.cs"><lines><line number="1" hits="1" branch="true" /></lines></class></classes></package></packages></coverage>' -Encoding utf8NoBOM
    $unreliablePlan = [pscustomobject]@{
        baseSha = 'base'; headSha = 'head'; coverageSlices = @([pscustomobject]@{
            packageId = 'Pocok.Unreliable'; sourceProject = 'src/Unreliable/Pocok.Unreliable.csproj'; sourceRoot = 'src/Unreliable'; assemblyName = 'Pocok.Unreliable'; testProjects = @('tests/Unreliable.Tests.csproj')
        })
    }
    $unreliablePlanPath = Join-Path $coverageRoot 'unreliable-plan.json'
    $unreliableSummaryPath = Join-Path $coverageRoot 'unreliable-summary.json'
    Write-PocokJson -InputObject $unreliablePlan -Path $unreliablePlanPath
    & (Join-Path $PSScriptRoot 'Write-CoverageSummary.ps1') -PlanPath (ConvertTo-PocokPath -Path $unreliablePlanPath -RepositoryRoot $repositoryRoot) -HeadRoot (ConvertTo-PocokPath -Path (Join-Path $coverageRoot 'unreliable') -RepositoryRoot $repositoryRoot) -BaseRoot (ConvertTo-PocokPath -Path (Join-Path $coverageRoot 'missing-base') -RepositoryRoot $repositoryRoot) -OutputJson (ConvertTo-PocokPath -Path $unreliableSummaryPath -RepositoryRoot $repositoryRoot) -OutputMarkdown (ConvertTo-PocokPath -Path (Join-Path $coverageRoot 'unreliable-summary.md') -RepositoryRoot $repositoryRoot) | Out-Null
    $unreliableSummary = Get-Content -LiteralPath $unreliableSummaryPath -Raw | ConvertFrom-Json
    Assert-True ($null -eq $unreliableSummary.slices[0].head.branchPercent) 'Missing branch condition identities must report branch coverage as unavailable.'

    $malformedRoot = Join-Path $coverageRoot 'malformed/Malformed.Tests'
    New-Item -ItemType Directory -Path $malformedRoot -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $malformedRoot 'coverage.cobertura.xml') -Value '<coverage>' -Encoding utf8NoBOM
    $malformedPlan = [pscustomobject]@{
        baseSha = 'base'; headSha = 'head'; coverageSlices = @([pscustomobject]@{
            packageId = 'Pocok.Malformed'; sourceProject = 'src/Malformed/Pocok.Malformed.csproj'; sourceRoot = 'src/Malformed'; assemblyName = 'Pocok.Malformed'; testProjects = @('tests/Malformed.Tests.csproj')
        })
    }
    $malformedPlanPath = Join-Path $coverageRoot 'malformed-plan.json'
    Write-PocokJson -InputObject $malformedPlan -Path $malformedPlanPath
    $malformedFailed = $false
    try {
        & (Join-Path $PSScriptRoot 'Write-CoverageSummary.ps1') -PlanPath (ConvertTo-PocokPath -Path $malformedPlanPath -RepositoryRoot $repositoryRoot) -HeadRoot (ConvertTo-PocokPath -Path (Join-Path $coverageRoot 'malformed') -RepositoryRoot $repositoryRoot) -BaseRoot (ConvertTo-PocokPath -Path (Join-Path $coverageRoot 'missing-base') -RepositoryRoot $repositoryRoot) -OutputJson (ConvertTo-PocokPath -Path (Join-Path $coverageRoot 'malformed-summary.json') -RepositoryRoot $repositoryRoot) -OutputMarkdown (ConvertTo-PocokPath -Path (Join-Path $coverageRoot 'malformed-summary.md') -RepositoryRoot $repositoryRoot) | Out-Null
    }
    catch { $malformedFailed = $true }
    Assert-True $malformedFailed 'Malformed Cobertura input must fail coverage summarization.'

    $emptyRoot = Join-Path $coverageRoot 'empty/Empty.Tests'
    New-Item -ItemType Directory -Path $emptyRoot -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $emptyRoot 'coverage.cobertura.xml') -Value '<?xml version="1.0"?><coverage><packages><package><classes><class filename="src/Other/File.cs"><lines><line number="1" hits="1" /></lines></class></classes></package></packages></coverage>' -Encoding utf8NoBOM
    $emptyPlan = [pscustomobject]@{
        baseSha = 'base'; headSha = 'head'; coverageSlices = @([pscustomobject]@{
            packageId = 'Pocok.Empty'; sourceProject = 'src/Empty/Pocok.Empty.csproj'; sourceRoot = 'src/Empty'; assemblyName = 'Pocok.Empty'; testProjects = @('tests/Empty.Tests.csproj')
        })
    }
    $emptyPlanPath = Join-Path $coverageRoot 'empty-plan.json'
    Write-PocokJson -InputObject $emptyPlan -Path $emptyPlanPath
    $emptyFailed = $false
    try {
        & (Join-Path $PSScriptRoot 'Write-CoverageSummary.ps1') -PlanPath (ConvertTo-PocokPath -Path $emptyPlanPath -RepositoryRoot $repositoryRoot) -HeadRoot (ConvertTo-PocokPath -Path (Join-Path $coverageRoot 'empty') -RepositoryRoot $repositoryRoot) -BaseRoot (ConvertTo-PocokPath -Path (Join-Path $coverageRoot 'missing-base') -RepositoryRoot $repositoryRoot) -OutputJson (ConvertTo-PocokPath -Path (Join-Path $coverageRoot 'empty-summary.json') -RepositoryRoot $repositoryRoot) -OutputMarkdown (ConvertTo-PocokPath -Path (Join-Path $coverageRoot 'empty-summary.md') -RepositoryRoot $repositoryRoot) | Out-Null
    }
    catch { $emptyFailed = $true }
    Assert-True $emptyFailed 'Coverage with no files owned by the slice must fail rather than report a false zero.'

    [xml]$testProps = Get-Content -LiteralPath (Join-Path $repositoryRoot 'tests/Directory.Build.props') -Raw
    $collectorReference = $testProps.SelectSingleNode('//*[local-name()="PackageReference" and @Include="coverlet.collector"]')
    Assert-True ($null -ne $collectorReference) 'Test projects must receive the central coverlet collector reference.'
    Assert-True ((Get-Content -LiteralPath (Join-Path $repositoryRoot 'tests/Directory.Build.props') -Raw) -match "EndsWith\('\.Tests'\)") 'Fixture projects must be excluded from test-only collector dependencies.'
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "CI tooling validation passed ($passed assertions)."
