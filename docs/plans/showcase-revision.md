# Pocok Showcase revision and package-composition plan

**Plan date:** 2026-07-18  
**Status:** Historical predecessor; local ten-plugin coverage is implemented and remaining bundle/deployment work belongs to `mvp-closure.md`  
**Scope:** Showcase acceptance ledger, six sample bundles, multi-engine UI, deterministic source composition, browser proof, and Render rollout

## Execution contract

Use `.agents/skills/pocok-showcase-engineering/SKILL.md` for every slice. Also use the package skill for demonstrated library/sample package contracts and the release skill for catalog, bundle, NuGet, or publication work. The skills contain generic procedure; this plan contains the repository-specific implementation.

Preserve the current package-agnostic Web host, manifest discovery, Home content, circuit-local editor/result state, bounded execution, left rail, themes, in-app log console, responsive three-column workbench, Docker path, and English/Hungarian behavior. Do not switch on package IDs in `Pocok.Showcase.Web` or add project references from the host to sample plugins.

No slice before S9 authorizes Render mutation. No slice in this plan authorizes package/tag publication; repository R10 owns publication. If a command fails, preserve the exact output and stop dependent slices rather than using local caches, fallback package versions, static fake output, or a reduced route set.

## Locked decisions

| Area | Required implementation |
|---|---|
| Sample artifacts | Six custom `PocokShowcaseBundle` NuGet packages contain ready-to-load plugin trees. Showcase Contracts, Components, and Web remain non-packable. |
| Host compatibility | Every bundle manifest declares `showcaseApiVersion: 1`; the publication tool requires an exact match before staging. |
| Package versions | NuGet-backed modes require one exact `Showcase__PackageVersion`; ranges, latest selection, and per-package maps are forbidden. |
| Resolution time | NuGet download/extraction happens in `Pocok.Showcase.PublishTool` before host startup. The running host never contacts NuGet. |
| Scripting security | Production/public enables JavaScript only. C# and CPython 3.14 require explicit trusted/local configuration and display truthful unavailable states otherwise. |
| Monaco | Pin `BlazorMonaco` 3.5.0 and Monaco assets locally behind one shared wrapper. Keep the existing buffered textarea as the failure/accessibility fallback. |
| Browser gate | Pin `Microsoft.Playwright.NUnit` 1.61.0; run Chromium on Ubuntu. Windows retains build/test/published-host/composition smoke. |
| Render | A reviewed rollout PR records the exact synchronized version and full-NuGet mode. Merge is approval; `checksPass` triggers deployment. |

## Repository capabilities to reuse

| Need | Reuse |
|---|---|
| Plugin discovery | Current `Pocok.Modularity` manifest loader, shared assembly identity, and `Showcase.Plugin.targets`. |
| Host configuration | `LoggingDefaultsConfigurator`, options `ValidateOnStart`, DI, `ReadinessSource`, and current forwarded-header/health setup. |
| Execution | `ShowcaseExecutionControls`, `IShowcaseRunClient`, bounded output/progress, scoped run state, cancellation, and disposal. |
| Text input | `ShowcaseBufferedTextArea`, `ShowcaseCodeAssistEditor`, `BufferedEditorValue`, and `DebouncedValueCommitter<T>`. Monaco wraps the same commit/reset semantics. |
| UI | Existing rails, guide/result components, semantic theme tokens, culture switch, in-app log sink, and global responsive CSS. |
| Real samples | Current Conversion, Scripting, Licensing plugins and their sample-reset tests. |
| Package APIs | Existing AppDefaults, Logging, Readiness, Localization, Licensing, BackgroundWork, Modularity, Signals, and Subscriptions public APIs; do not duplicate them in Showcase infrastructure. |
| Publication | Current Bash/PowerShell publication scripts, `Pocok.Showcase.PublishTool`, smoke script, Dockerfile, and Render Blueprint. |

## Configuration contract

Use standard .NET configuration keys and environment-variable mapping:

```text
Showcase__CompositionMode=LocalLibrariesLocalSamples
Showcase__PackageVersion=
Showcase__NuGetSource=https://api.nuget.org/v3/index.json
Showcase__TrustedScriptEnginesEnabled=false
Showcase__RequireCompleteCatalog=false
```

`CompositionMode` is one of:

1. `LocalLibrariesLocalSamples` — default in source, local CI, and ordinary Docker builds;
2. `NuGetLibrariesLocalSamples` — local sample projects compile against one exact package version;
3. `NuGetLibrariesNuGetSamples` — no sample project build; exact bundle packages are downloaded and extracted.

`PackageVersion` is forbidden in local mode and required in both NuGet-backed modes. `NuGetSource` defaults to nuget.org and accepts either an HTTPS NuGet V3 service index or, for local/CI rehearsal, an existing absolute folder source. Production rejects folder and non-HTTPS sources. Configuration validation fails before host publication on an unknown mode, missing/unparseable version, invalid/unreachable source kind, or source-mode disagreement. Trusted engines default to disabled in every environment; enabling them is an explicit operator assertion that the deployment is trusted and not anonymously public.

## Sequential runbook

| Slice | Depends on | Handoff |
|---|---|---|
| S1 provenance/state/coverage ledger | Repository R6 | Honest current-build acceptance map |
| S2 composition and bundle contracts | S1 plus repository R2 | Versioned validated schemas |
| S3 six real-code plugins and Scripting UI | Repository R4 plus S2 | Complete all-local Showcase |
| S4 six sample bundles | S3 | Catalogued alpha-candidate bundle packages |
| S5 tri-state publication | S4 | Three equivalent temporary-feed modes |
| S6 hardening and browser/Docker gates | S5 | Cross-platform executable evidence |
| S7 documentation and repository handoff | S6 | Inputs for repository R7-R10 |
| S8 clean nuget.org composition | Repository R10 | Public-feed evidence |
| S9 reviewed Render rollout | S8 plus explicit approval | Live deployment or recorded rollback |

## S1. Make state, provenance, and coverage executable

### State ownership

Document and test these existing lifetimes in `showcase/docs/ARCHITECTURE.md`:

- singleton: package/slice/text catalogs, public bounded log sink, runner queue/state, readiness, `TimeProvider`;
- circuit scoped: `ShowcaseUiState`, editor inputs, selected sample/engine, results, progress, cancellation client;
- per run: fresh sample input, execution context, bounded output/progress writers, temporary resources;
- browser local state: theme preference only; no source, result, log, license, or secret persistence;
- immutable publication state: generated package catalog, slice inventory, composition record, and acceptance ledger.

Add regression tests that two circuits do not share editor/sample/result state, a disposed circuit cancels its active run, selecting the same sample advances the reset revision, and singleton logs remain allowlisted/bounded.

### Package-to-scenario ledger

Add `showcase/coverage.json` and a schema validated by `Pocok.Showcase.PublishTool`. Every `state != Retired`, `kind: Library` catalog entry appears exactly once with:

```json
{
  "packageId": "Pocok.Conversion",
  "evidenceKind": "SandboxScenario",
  "owner": "pocok.showcase.conversion",
  "scenario": "strict-integer",
  "publicApis": ["ValueConverter.Convert<T>"]
}
```

Allowed evidence kinds are `SandboxScenario`, `HostExercise`, and `BoundedProbe`. Add `showcase/scripts/invoke-showcase-acceptance.ps1`; it accepts only paths/IDs present in the validated ledger, runs the corresponding existing test/sample/browser filter, and writes `artifacts/showcase-acceptance.json` with commit, composition mode, package assembly informational version, command ID, outcome, and safe duration. It never records stdout containing user data, absolute paths, caches, source, keys, or secrets.

Use this fixed ownership map:

| Packages | Evidence |
|---|---|
| Conversion | Conversion sandbox scenario plus current package tests. |
| Readiness | Readiness sandbox lifecycle scenarios. |
| AppDefaults, AppDefaults.Logging, AppDefaults.Logging.Serilog | Logging sandbox/host exercise plus bounded configurator probes. |
| Modularity.Contracts, Modularity | Real host loading all plugin manifests and shared contracts. |
| AppDefaults.Modularity | Bounded `ModularityDefaultsConfigurator` host-builder probe using a safe temporary plugin directory. |
| BackgroundWork | Bounded observation/debounce/repetition probe and Localization reload exercise. |
| Scripting and JavaScript/CSharp/Python adapters | Scripting sandbox; trusted engines run only in local acceptance. |
| Localization | Localization sandbox provider/culture/fallback scenarios. |
| Signals | Existing deterministic console/runtime probe, including reconnect. |
| Subscriptions | Existing keyed subscription console probe. |
| Licensing | Licensing sign/read/validate sandbox. |
| AppDefaults.Licensing | Bounded host-builder block/warn/revalidation probe using synthetic in-memory license material. |

Showcase evidence supplements the repository package gates; the ledger must link to, not replace, their manifest results.

### Baseline commands

```powershell
dotnet restore showcase/Pocok.Showcase.slnx --locked-mode
dotnet restore showcase/Pocok.Showcase.Samples.slnx --locked-mode
dotnet build showcase/Pocok.Showcase.slnx --configuration Release --no-restore
dotnet build showcase/Pocok.Showcase.Samples.slnx --configuration Release --no-restore
dotnet test showcase/tests/Pocok.Showcase.Tests/Pocok.Showcase.Tests.csproj --configuration Release --no-build
dotnet test samples/Showcase/Pocok.Showcase.Samples.Tests/Pocok.Showcase.Samples.Tests.csproj --configuration Release --no-build
pwsh -File showcase/scripts/publish-showcase.ps1 -OutputPath artifacts/showcase/local
python showcase/scripts/smoke-showcase.py artifacts/showcase/local
pwsh -File showcase/scripts/invoke-showcase-acceptance.ps1 -PublishRoot artifacts/showcase/local
```

**Accept when:** every current library has exactly one honest executable ledger owner, local assembly versions/commit are recorded, and the existing published-host smoke remains green.

## S2. Define versioned composition and bundle contracts

### Composition record

Add DTOs and JSON schema under `showcase/tools/Pocok.Showcase.PublishTool/Composition`. Generate `Content/showcase-composition.json` in all modes:

```json
{
  "schemaVersion": 1,
  "showcaseApiVersion": 1,
  "mode": "LocalLibrariesLocalSamples",
  "repositoryCommit": "<sha>",
  "packageVersion": null,
  "hostLibraries": {
    "Pocok.AppDefaults.Logging": "<assembly-version>",
    "Pocok.Modularity": "<assembly-version>",
    "Pocok.Readiness": "<assembly-version>"
  },
  "plugins": [
    {
      "moduleId": "pocok.showcase.conversion",
      "samplePackageId": "Pocok.Showcase.Conversion",
      "sampleVersion": "<assembly-version>",
      "primaryLibraryId": "Pocok.Conversion",
      "libraryVersions": { "Pocok.Conversion": "<assembly-version>" },
      "directory": "plugins/pocok.showcase.conversion",
      "bundleSha256": null
    }
  ]
}
```

In a NuGet-backed mode, every host, sample, and library version value equals exact `Showcase__PackageVersion`. Local mode records assembly informational versions and the Git commit. Store only forward-slash relative paths. Do not record credentials, source URLs containing credentials, absolute paths, caches, extraction roots, or environment values.

### Bundle manifest

Each sample project owns `showcase.bundle.json` beside `pocok.module.json`:

```json
{
  "schemaVersion": 1,
  "showcaseApiVersion": 1,
  "moduleId": "pocok.showcase.conversion",
  "samplePackageId": "Pocok.Showcase.Conversion",
  "primaryLibraryId": "Pocok.Conversion",
  "libraryPackageIds": ["Pocok.Conversion", "Pocok.Modularity.Contracts"],
  "sharedAssemblies": [
    "Pocok.Showcase.Contracts",
    "Pocok.Showcase.Components",
    "Pocok.Modularity.Contracts"
  ]
}
```

The pack target fills exact library versions and file hashes in the packaged copy. Catalog edges, project/package references, `pocok.module.json`, bundle manifest, and composition record must agree.

Validate exact API version, IDs, unique slug/module/package, safe relative paths, zip-slip paths, duplicate/case-colliding files, entry assembly/deps/manifest existence, host-shared/private conflicts, exact versions, file hashes, and unexpected files before readiness. The host reads only already-validated composition/manifests; it never downloads or selects packages.

**Tests:** schema round trips, unknown versions/fields, malformed IDs, unsafe paths, duplicate/case collisions, missing assets, shared assembly duplication, version disagreement, hash mismatch, and cross-mode logical equivalence.

**Accept when:** all three modes can produce the same logical plugin inventory and every malformed record fails publication before host startup.

## S3. Complete real-code samples and Scripting UI

### S3a. Add three plugins

Create projects under `samples/Showcase`, each with README, invariant/Hungarian resources, manifest, bundle manifest, slice/module/page/editor/model files, fresh sample factories, and sample tests:

- `Pocok.Showcase.AppDefaults.Logging`;
- `Pocok.Showcase.Readiness`;
- `Pocok.Showcase.Localization`.

#### Logging

Use the host's established AppDefaults logging setup and `ILogger`; add no independent global logger configuration. Emit real structured events whose display DTO contains timestamp, level, event ID, shortened category/namespace, message template, rendered message, and allowlisted scalar properties. Newest entries appear first with the existing level colors. Scenarios cover category minimum levels, structured properties, exception-safe rendering, and namespace shortening. Never render exception stacks, arbitrary scopes, source, secrets, paths, or non-allowlisted values.

The page also runs bounded non-global probes that apply `IApplicationConfigurator`/`ConfigureWith(...)`, `LoggingDefaultsConfigurator`, and the Serilog configurator to fresh builders and verify registered providers/options. Dispose every temporary provider. These probes provide coverage; they do not replace the host logger or mutate it.

#### Readiness

Create a fresh `ReadinessSource` per run and demonstrate ready, failed, cancelled startup, shutdown/stopped, and a new restart cycle. Use deterministic `TimeProvider` data and the real snapshot/sequence/failure APIs. Do not mutate the host's readiness singleton. Timeline output comes from actual transitions.

#### Localization

Use `FileStringLocalizer`, `CompositeStringLocalizer`, `ResourceCulture`, and enum translation against bounded synthetic resources staged with the plugin. Scenarios cover English/Hungarian, explicit parent/invariant fallback, missing-key behavior, JSON/RESX parity, and one reload through current BackgroundWork behavior. Use a safe temporary directory from the existing execution context, dispose the localizer, and never expose the path.

### S3b. Strengthen Licensing

Make the default scenario execute, in memory and per run:

1. `LicenseCryptography.CreateSigningKeyPair`;
2. construct a synthetic `LicenseDocument` with deterministic public claims;
3. `LicenseCryptography.Sign` with the ephemeral private key;
4. `LicenseReader.ReadAndVerify` using only the public key;
5. `LicenseValidator.Validate` with deterministic `LicenseValidationContext`;
6. one tampered-signature or untrusted-key rejection.

Dispose key material immediately. Output only validation code, safe claim summary, key ID, algorithm, and bounded timings. Never return or log PEM, private parameters, full envelopes, PSKs, encryption secrets, machine identifiers, or customer-sensitive data. Add a bounded AppDefaults.Licensing probe for block/warn/revalidation using inline synthetic material.

Keep the existing Conversion and Licensing reset-revision rule: explicit Razor expressions such as `Value="@Value.SourceValue"`, typed native `@onchange`, and a unique revision on every sample selection. Prove that selecting each sample updates every field, selecting the same sample resets edits, and edited values change the execution result.

### S3c. Complete the Scripting engine experience

Prerequisite: repository R4a has alpha-candidate core/JavaScript/CSharp/Python packages.

Register engines from adapter descriptors. When `Showcase__TrustedScriptEnginesEnabled=false`, register C#/Python as unavailable descriptors and keep JavaScript executable. Development or another explicitly private/trusted deployment may enable the adapters through startup configuration. Never infer trust from environment name, hostname, request, cookie, query string, or client state, and never expose a runtime/UI toggle.

Keep separate circuit-local source and reset revision per sample/engine. Provide equivalent success, structured object/list result, syntax failure, missing result, timeout, and harmless validator-rejection samples. Rejected JavaScript `eval`, C# reflection/assembly loading, and Python `eval`/dangerous import must produce validation diagnostics without starting execution.

Carry engine ID, descriptor/capabilities, validation diagnostics, enforced limits, progress, result, and safe worker failure code through typed models. Display only limits the descriptor says are enforced.

### S3d. Add the shared Monaco wrapper

Pin `BlazorMonaco` 3.5.0 in `Directory.Packages.props` and reference it only from `showcase/src/Pocok.Showcase.Components`. Add:

- `ShowcaseMonacoEditor.razor` and code-behind;
- `wwwroot/monacoEditor.js` for narrowly scoped model/provider/disposable interop;
- tests in `showcase/tests/Pocok.Showcase.Tests`;
- local script/style registration in the host layout according to the package's pinned asset paths.

The wrapper owns one Monaco model per component instance. Model identity includes engine language plus reset revision; ordinary buffered commits update server state without replacing the model. Dispose models, completion providers, resize observers, and JS references on component disposal/reconnect.

Use the existing buffer/debouncer contract with a 500 ms quiet period. Flush the latest browser value before Run, blur, sample reset, engine switch, fallback switch, and disposal. External resets preserve the caret unless the reset revision changes. Bound source before sending to the server.

Map current dark/light theme tokens into two Monaco themes. Provide syntax modes and small engine-owned completion catalogs only; do not claim Roslyn/Python language-service semantics. Code blocks and completion documentation wrap or scroll without widening the page.

If Monaco initialization, asset loading, or JS interop fails, show one safe diagnostic and switch to `ShowcaseBufferedTextArea` with the latest committed value. No CDN request is permitted. Retain accessible labels, keyboard focus, and ordinary textarea operation without JavaScript.

**Focused validation:** component/unit tests plus sample tests for descriptor tabs, Production unavailability, validator-before-worker, per-engine state, reset/flush, fallback, disposal, themes, localization, and safe diagnostics.

**S3 accept when:** all six plugins stage independently, English/Hungarian resources are complete, local trusted mode runs all three engines, Production runs only JavaScript, and the S1 ledger executes every library with current repository assemblies.

## S4. Package six custom Showcase bundles

Make these projects independently packable with exact IDs and tag prefixes:

| Package | Tag prefix | Primary library |
|---|---|---|
| `Pocok.Showcase.Conversion` | `showcase.conversion-v` | `Pocok.Conversion` |
| `Pocok.Showcase.Scripting` | `showcase.scripting-v` | `Pocok.Scripting` |
| `Pocok.Showcase.Licensing` | `showcase.licensing-v` | `Pocok.Licensing` |
| `Pocok.Showcase.AppDefaults.Logging` | `showcase.appdefaults.logging-v` | `Pocok.AppDefaults.Logging` |
| `Pocok.Showcase.Readiness` | `showcase.readiness-v` | `Pocok.Readiness` |
| `Pocok.Showcase.Localization` | `showcase.localization-v` | `Pocok.Localization` |

Each nuspec declares custom package type `PocokShowcaseBundle` version `1.0.0`. A common `showcase/Showcase.Bundle.targets` publishes the plugin once with exact release properties and packs this deterministic tree:

```text
LICENSE
NOTICE
README.md
tools/pocok-showcase/<module-id>/
  pocok.module.json
  showcase.bundle.json
  <entry assembly>.dll
  <entry assembly>.deps.json
  <entry assembly>.pdb
  Content/...
  <private dependency assemblies and native assets>
  THIRD-PARTY-NOTICES.txt
```

Exclude host-shared `Pocok.Showcase.Contracts`, `Pocok.Showcase.Components`, `Pocok.Modularity.Contracts`, and framework assemblies from the payload; require them in the shared manifest. Include exact private library/adapter dependencies needed to load the plugin, with per-file SHA-256 and third-party notices. Sort entries and normalize timestamps/content so repeated pack produces identical hashes.

Because these are deployment bundles rather than compile packages, they contain no `lib/` compile assets and no `.snupkg`. Keep the plugin portable PDB in the main nupkg and apply the automated Source Link audit to it. Extend `PublicReleaseAudit` through `proofProfile: ShowcaseBundle` instead of weakening library checks.

Add all six schema-v2 catalog entries with `publicationPolicy: PrereleaseOnly`, version properties, consumers, and exact internal edges. Each includes `Pocok.Modularity.Contracts` and every bundled/demonstrated Pocok library; Scripting includes all four Scripting packages.

**Validation:** pack twice and compare hashes; inspect nuspec/package type/tree; verify bundle/file hashes and PDB Source Link; extract into an empty directory; reject traversal/collision/shared-copy fixtures; load through the published host; run package-specific tag resolution; install all six from a clean temporary feed.

**Accept when:** every bundle is alpha-eligible, independently extractable/loadable, contains no host-shared duplicate or secret/path leakage, and its catalog/project/nuspec/bundle metadata agree.

## S5. Implement the three deterministic source modes

Extend `Pocok.Showcase.PublishTool` arguments and both wrapper scripts:

```text
--composition-mode <mode>
--package-version <exact-semver>
--nuget-source <v3-url>
--nuget-packages <isolated-directory>
--output <publish-root>
--require-complete
--no-restore
```

Environment configuration supplies defaults; explicit arguments override it. Reference the centrally pinned `NuGet.Protocol` 7.6.0 from the non-packable PublishTool and use exact `FindPackageByIdResource` download. Do not implement another NuGet service-index/version-selection algorithm.

### LocalLibrariesLocalSamples

Build the host and sample projects from the current checkout using only ProjectReferences. Disable NuGet fallback for Pocok IDs through an isolated NuGet config/source mapping. Record host and sample assembly versions plus Git commit. Missing current outputs fail; do not use global caches as plugin input.

### NuGetLibrariesLocalSamples

Keep ProjectReferences only among non-packable Showcase Contracts/Components/Web and local sample source. In the host, Components, PublishTool where applicable, and every sample csproj, condition each public Pocok dependency:

```xml
<ProjectReference Condition="'$(PocokShowcaseLibrarySource)' == 'Local'" Include="..." />
<PackageReference Condition="'$(PocokShowcaseLibrarySource)' == 'NuGet'"
                  Include="Pocok.Example"
                  Version="[$(PocokShowcasePackageVersion)]" />
```

PublishTool sets `PocokShowcaseLibrarySource=NuGet` and exact `PocokShowcasePackageVersion` before restoring/building the host and samples. Use a clean packages directory and package-source mapping so every relevant `project.assets.json` proves all public Pocok libraries—including host infrastructure such as AppDefaults.Logging, Modularity.Contracts/Modularity, Readiness, BackgroundWork, and Localization—came from the selected feed/version. Build local sample projects, then package/stage their output.

### NuGetLibrariesNuGetSamples

Do not discover, restore, build, or read `samples/Showcase` projects. Build the host from current Showcase source but restore every public Pocok host dependency from the exact selected package version. Download the six exact bundle IDs/version, verify nupkg hash when a release manifest is supplied, validate package/bundle manifests and all file hashes, and safely extract only `tools/pocok-showcase/<module-id>/` into the publish root. The host supplies shared assemblies from those exact NuGet library assets, never local library projects.

All modes write `package-catalog.json`, `showcase-slices.json`, `showcase-composition.json`, and `showcase-acceptance.json`. Startup validates them before readiness. No mode silently changes source or version.

### Temporary-feed proof

Create an isolated temporary folder feed containing the exact library and bundle candidates; protocol/propagation fixtures use an in-process HTTP V3 test server. Use a new isolated `NUGET_PACKAGES` path and generated NuGet config for each mode:

```powershell
pwsh -File showcase/scripts/publish-showcase.ps1 -OutputPath artifacts/showcase/local -CompositionMode LocalLibrariesLocalSamples
pwsh -File showcase/scripts/publish-showcase.ps1 -OutputPath artifacts/showcase/hybrid -CompositionMode NuGetLibrariesLocalSamples -PackageVersion <version> -NuGetSource <temp-feed>
pwsh -File showcase/scripts/publish-showcase.ps1 -OutputPath artifacts/showcase/full -CompositionMode NuGetLibrariesNuGetSamples -PackageVersion <version> -NuGetSource <temp-feed>
python showcase/scripts/smoke-showcase.py artifacts/showcase/local
python showcase/scripts/smoke-showcase.py artifacts/showcase/hybrid
python showcase/scripts/smoke-showcase.py artifacts/showcase/full
```

Compare logical plugin/package IDs, exact versions, routes, resources, and scenarios across records. Paths/hashes may differ only where the schema explicitly permits.

**Accept when:** all three modes pass from clean inputs, hybrid/full restore graphs prove every public Pocok host/sample dependency came from the exact selected source/version, full mode proves no sample build, and the running host makes no NuGet request.

## S6. Add executable browser, publication, and Docker gates

Create `showcase/tests/Pocok.Showcase.BrowserTests` with NUnit and pinned `Microsoft.Playwright.NUnit` 1.61.0. The fixture publishes/starts the host on an OS-assigned loopback port, waits for `/health/ready`, captures console/page/request failures and a trace on failure, and always terminates the child process. It does not rely on an externally running server.

Chromium tests cover:

- every generated installed route and Home/System navigation;
- English/Hungarian cookie rendering;
- dark/light preference and Monaco theme;
- wide/narrow layout without horizontal page overflow;
- sample/reset/edit execution for Conversion and Licensing;
- Monaco multiline typing with mid-document caret preservation;
- debounce and exact flush before Run/reset/engine switch;
- JavaScript success/rejection and Production C#/Python unavailable states;
- local trusted C#/Python success when their prerequisites are enabled;
- expandable Samples/Execution/What-to-notice/Purpose/Quick-start/source sections;
- in-app log toggle/filter/newest-first/resizable behavior;
- SignalR disconnect/reconnect without lost committed editor value;
- no CDN or unexpected external network request.

CI commands:

```powershell
dotnet restore showcase/Pocok.Showcase.slnx --locked-mode
dotnet restore showcase/Pocok.Showcase.Samples.slnx --locked-mode
dotnet format showcase/Pocok.Showcase.slnx --verify-no-changes --no-restore
dotnet format showcase/Pocok.Showcase.Samples.slnx --verify-no-changes --no-restore
dotnet build showcase/Pocok.Showcase.slnx --configuration Release --no-restore
dotnet build showcase/Pocok.Showcase.Samples.slnx --configuration Release --no-restore
dotnet test showcase/tests/Pocok.Showcase.Tests/Pocok.Showcase.Tests.csproj --configuration Release --no-build
dotnet test samples/Showcase/Pocok.Showcase.Samples.Tests/Pocok.Showcase.Samples.Tests.csproj --configuration Release --no-build
dotnet build showcase/tests/Pocok.Showcase.BrowserTests/Pocok.Showcase.BrowserTests.csproj --configuration Release
pwsh showcase/tests/Pocok.Showcase.BrowserTests/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
dotnet test showcase/tests/Pocok.Showcase.BrowserTests/Pocok.Showcase.BrowserTests.csproj --configuration Release --no-build
```

Run Playwright only on Ubuntu. Linux and Windows both run source build/tests, all three publication modes, generated-route smoke, CPython 3.14 trusted-engine tests, and manifest/resource validation. Use the immutable action refs locked in repository plan R2; in particular, replace the floating refs in `showcase.yml` with `actions/checkout@9c091bb21b7c1c1d1991bb908d89e4e9dddfe3e0`, `actions/setup-dotnet@26b0ec14cb23fa6904739307f278c14f94c95bf1`, and `actions/setup-python@83679a892e2d95755f2dac6acb0bfd1e9ac5d548`.

Update `showcase/Dockerfile` to accept non-secret build args for composition mode/version/source. Local Docker defaults remain all-local. Build the full-NuGet image from the temporary feed in CI where reachable, then run health, generated routes, theme/culture, and reconnect smoke. The final image contains no SDK, NuGet cache/config, sample source, feed credentials, compiler output, Python runtime, or trusted-engine enablement. Private adapter/worker assets may be present in the Scripting bundle but are not static web assets and are unreachable while the public Render configuration keeps trusted engines disabled.

**Accept when:** Linux/Windows publication proof, Ubuntu Chromium, local/full Docker, and the existing shell regression suite pass with clean caches and no external browser assets.

## S7. Reconcile documentation and hand back to release work

Update only implemented behavior in root/Showcase READMEs, `showcase/docs/ARCHITECTURE.md`, `showcase/docs/ADDING_A_SLICE.md`, security, Docker/Render, `PUBLICATION.md`, and `docs/current-handoff.md`. Document bundle shape, API version, exact source modes, trusted-engine boundary, CPython 3.14 prerequisite, Monaco ownership/fallback, browser command, and recovery commands. Link package READMEs rather than copying contracts.

Run all S6 commands from a clean checkout and attach the six bundle hashes, graph entries, three composition records, acceptance ledger, and evidence commit to the handoff for repository R7-R10.

**Accept when:** a clean maintainer can reproduce every temporary-feed mode with only the documented commands and no unstated cache, environment, or design choice.

## S8. Prove clean nuget.org composition

After repository R10 publishes the synchronized prerelease, clear/create an isolated NuGet config and packages directory. Publish only in `NuGetLibrariesNuGetSamples` using the exact released version and nuget.org. Do not build or read sample projects.

Verify downloaded nupkg hashes against the global release manifest, exact bundle/library versions, host API version, safe extraction, absence of local assemblies/caches, all generated routes, English/Hungarian resources, Production engine availability, Playwright public-mode tests, and Docker image contents.

**Accept when:** `showcase-composition.json` equals the global release manifest's IDs/versions/hashes and every public-mode smoke passes without local project or cache input.

## S9. Roll out Render through an exact checked PR

**Approval gate:** present the rollout PR commit, synchronized package version, S8 composition hash, Docker image digest from local CI, route list, and previous known-good Render deploy ID. Merge of that exact PR is approval.

The PR sets in `showcase/render.yaml`:

```yaml
autoDeployTrigger: checksPass
envVars:
  - key: ASPNETCORE_ENVIRONMENT
    value: Production
  - key: Showcase__CompositionMode
    value: NuGetLibrariesNuGetSamples
  - key: Showcase__PackageVersion
    value: <exact synchronized version>
  - key: Showcase__NuGetSource
    value: https://api.nuget.org/v3/index.json
  - key: Showcase__TrustedScriptEnginesEnabled
    value: "false"
  - key: Showcase__RequireCompleteCatalog
    value: "true"
```

Render exposes these non-secret values as Docker build arguments; the Dockerfile passes them only to PublishTool. The final runtime consumes the generated composition record and never contacts NuGet.

After checks pass and Render deploys, allow free-tier cold start, then verify `/health/live`, `/health/ready`, every generated route, culture change, theme, JavaScript run/rejection, C#/Python unavailability, log safety, and SignalR reconnect. Compare live composition/version output with the approved record. Confirm no absolute paths, cache/source details, unsafe logs, license material, keys, worker paths/content, or credentials are publicly served or rendered.

If build/readiness fails, Render must keep the previous successful deployment. If post-switch smoke fails, roll back to the recorded previous successful deploy through Render's rollback action, record both deploy IDs and failed check, and leave autodeploy disabled until a corrective PR is approved. Do not change environment values manually to create an unreviewed composition.

If Render credentials/access are unavailable, retain the exact locally smoked image/record and stop S9 as externally blocked; local Docker is not deployment evidence.

**Accept when:** the approved full-NuGet revision is live and matches the release manifest, or rollback is verified and the MVP remains open with the exact corrective action.

## Final acceptance

Showcase MVP completion requires separately labelled evidence for current-local execution, package tests, six bundle artifacts, three temporary-feed modes, Linux/Windows publication, Ubuntu Chromium, local/full Docker, clean nuget.org composition, and Render. Only minor visual tuning, small frontend defects, or explicit `post-mvp-roadmap.md` items may remain.

## External prerequisites and deterministic fallbacks

| Prerequisite | Required behavior | Fallback |
|---|---|---|
| CPython 3.14 | Explicit `POCOK_PYTHON_EXECUTABLE`; Linux/Windows probe passes | Engine is unavailable; trusted local acceptance remains failed. |
| Playwright Chromium | Install pinned browser/dependencies in Ubuntu CI | Unit/published-host smoke may run, but S6 remains failed. |
| NuGet V3 feed | Exact version and clean isolated packages directory | Temporary feed proves S5; nuget.org failure blocks S8. |
| BlazorMonaco assets | Pinned local package assets | Buffered textarea preserves functionality; Monaco acceptance remains failed until repaired. |
| Docker/BuildKit | Build local and full-NuGet images | Published-directory smoke is retained, but Docker gate remains failed. |
| Render service/access | Existing linked Blueprint service and previous deploy | Exact local image/record retained; no deployment claim. |

## Global release integration note

The current schema-v1 `GLOBAL-v*` workflow publishes only catalog entries that are presently releasable. Future Showcase bundle work must add bundles to the existing synchronized orchestrator after schema-v2 catalog and bundle proof are implemented. Do not create a second global release workflow for Showcase packages.
