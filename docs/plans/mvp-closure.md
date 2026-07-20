# Pocok MVP closure plan

**Plan date:** 2026-07-20  
**Status:** Planned; execute only after [`release-readiness.md`](release-readiness.md) completes  
**Entry condition:** One approved synchronized prerelease of all eighteen library packages, with no unresolved partial publication  
**Produces:** Typed release authority, immutable recovery, NuGet-composed Showcase bundles, public deployment, and final MVP evidence

## Purpose

Turn the already alpha-released library repository into the final richer MVP.

Release Readiness owns basic package eligibility, the Scripting split, local Showcase coverage, and the first library-only `GLOBAL-v*` release. This plan must not repeat that work. It starts from:

- eighteen non-retired library packages;
- all eighteen eligible for alpha publication;
- one completed synchronized prerelease;
- ten working non-packable local Showcase plugins;
- JavaScript/C#/Python Scripting packages;
- one Showcase-internal Monaco wrapper;
- the existing schema-v1 package and global workflows.

MVP Closure adds the higher-cost infrastructure needed for reproducible exact-artifact releases and NuGet-based Showcase deployment:

- typed schema-v2 catalog and release tooling;
- immutable candidate manifests and Source Link proof;
- package-complete Showcase coverage and provenance records;
- ten packable Showcase deployment bundles;
- deterministic local, hybrid, and full-NuGet composition modes;
- workflow capacity for the twenty-eight-package graph;
- browser, Docker, public-feed, and Render evidence;
- a later synchronized release containing all libraries and all Showcase bundles.

## Execution contract

Use:

- `.agents/skills/pocok-release-engineering/SKILL.md` for catalog, release tool, artifacts, Source Link, workflows, NuGet, and recovery;
- `.agents/skills/pocok-showcase-engineering/SKILL.md` for bundle contracts, composition, browser, Docker, and Render;
- `.agents/skills/pocok-package-engineering/SKILL.md` only when a real library defect is found;
- `.agents/skills/pocok-agentic-workflow/SKILL.md` for approval gates and final evidence.

Current source and Release Readiness evidence are authoritative. When implementation begins, reconcile paths and exact package versions without reopening the locked architecture.

Do not:

- reimplement library behavior already proven in Release Readiness;
- weaken ordinary library package audits to accommodate Showcase bundles;
- create another plugin framework or runtime package downloader;
- expose public hostile-workload C# or Python execution;
- publish a reusable Monaco/Scripting UI package;
- infer release or deployment approval;
- rebuild artifacts during recovery;
- claim public-feed or Render evidence from local simulations.

## Locked decisions

| Area | Decision |
|---|---|
| Catalog | Schema v2 separates API maturity from publication policy and package kind |
| Release authority | One non-packable typed `Pocok.ReleaseTool`; PowerShell entry points become thin wrappers |
| Version handling | `NuGet.Versioning` for every parse/comparison; `NuGet.Protocol` for V3 operations |
| Candidate | Build, test, pack, and audit once; manifest and hashes bind artifacts to one commit/tag |
| Recovery | Download and reuse retained exact candidate assets; never rebuild or overwrite |
| Source Link | Automated PDB mapping, source checksum, commit, URL, and installed stack-frame proof |
| Showcase bundles | Ten custom `PocokShowcaseBundle` packages, one per existing plugin |
| Composition | NuGet resolution occurs before host startup; the running Web host never contacts NuGet |
| Version selection | NuGet-backed Showcase modes require one exact synchronized version |
| Workflow capacity | Replace the current single-job eighteen-target limit with immutable prepare and wave-publish stages |
| Browser | Pinned Playwright Chromium on Ubuntu |
| Public Scripting | JavaScript available; C# and Python truthfully unavailable in public Production |
| Deployment | Exact checked Render rollout PR is approval; rollback behavior is explicit |

## Non-goals

- no new library family or broad public API redesign;
- no full runtime Showcase demo for BackgroundWork, Modularity, Signals, or Subscriptions;
- no public `Pocok.Scripting.UI.Blazor`;
- no semantic Roslyn/Python editor service;
- no multi-file script workspace;
- no public untrusted C#/Python service;
- no NuGet calls from the running Showcase;
- no SBOM, package signing, canary feed, release dashboard, or selective global subsets;
- no additional deployment platform.

Those items remain in `post-mvp-roadmap.md`.

## Complete graph assumed by this plan

### Libraries

The eighteen Release Readiness libraries remain ordinary compile packages.

### Showcase bundles

Add ten deployment-bundle packages:

| Package | Tag prefix | Version property | Primary local plugin |
|---|---|---|---|
| `Pocok.Showcase.Conversion` | `showcase.conversion-v` | `PocokShowcaseConversionPackageVersion` | Conversion |
| `Pocok.Showcase.Scripting` | `showcase.scripting-v` | `PocokShowcaseScriptingPackageVersion` | Scripting |
| `Pocok.Showcase.Licensing` | `showcase.licensing-v` | `PocokShowcaseLicensingPackageVersion` | Licensing |
| `Pocok.Showcase.AppDefaults.Logging` | `showcase.appdefaults.logging-v` | `PocokShowcaseAppDefaultsLoggingPackageVersion` | AppDefaults.Logging |
| `Pocok.Showcase.Localization` | `showcase.localization-v` | `PocokShowcaseLocalizationPackageVersion` | Localization |
| `Pocok.Showcase.Readiness` | `showcase.readiness-v` | `PocokShowcaseReadinessPackageVersion` | Readiness |
| `Pocok.Showcase.BackgroundWork` | `showcase.backgroundwork-v` | `PocokShowcaseBackgroundWorkPackageVersion` | BackgroundWork |
| `Pocok.Showcase.Modularity` | `showcase.modularity-v` | `PocokShowcaseModularityPackageVersion` | Modularity |
| `Pocok.Showcase.Signals` | `showcase.signals-v` | `PocokShowcaseSignalsPackageVersion` | Signals |
| `Pocok.Showcase.Subscriptions` | `showcase.subscriptions-v` | `PocokShowcaseSubscriptionsPackageVersion` | Subscriptions |

The complete synchronized graph therefore contains twenty-eight packable packages. Workflow capacity must be upgraded before any live release includes bundles.

## Sequential runbook

| Slice | Depends on | Completion handoff |
|---|---|---|
| M1. Typed catalog and release authority | completed Release Readiness | Schema v2 and tested typed tool |
| M2. Exact artifacts and Source Link | M1 | Immutable proven library candidate |
| M3. Typed Showcase coverage and composition contracts | M1 | Validated coverage, bundle, and composition schemas |
| M4. Ten deterministic Showcase bundles | M2-M3 | Twenty-eight catalogued package candidates |
| M5. Three source modes and workflow capacity | M2-M4 | Reproducible full candidate and safe wave publication |
| M6. Showcase deepening, browser, and Docker gates | M5 | Complete local and temporary-feed MVP evidence |
| M7. Full synchronized release and clean nuget.org composition | M6 plus approval | Public twenty-eight-package graph |
| M8. Reviewed Render rollout and final closure | M7 plus approval | Live MVP or exact verified rollback/external block |

## M1. Migrate to a typed catalog and release authority

### M1.1 Catalog schema v2

Create schema v2 while preserving all current package identities and dependencies.

Each non-retired entry includes:

```json
{
  "id": "Pocok.Example",
  "kind": "Library",
  "project": "src/Example/Pocok.Example.csproj",
  "tagPrefix": "example-v",
  "versionProperty": "PocokExamplePackageVersion",
  "family": "Capability",
  "state": "Experimental",
  "publicationPolicy": "PrereleaseOnly",
  "consumer": "ExampleConsumer",
  "internalDependencies": [],
  "allowedExternalDependencies": [],
  "proofProfile": "Library"
}
```

Allowed publication policies:

```text
Disabled
PrereleaseOnly
StableAllowed
```

Rules:

- `state` remains API/product maturity;
- `publicationPolicy` controls channels;
- retired packages are `Disabled`;
- alpha-released experimental libraries become `PrereleaseOnly`;
- existing stable candidates retain `StableAllowed` only after their exact evidence is reconfirmed;
- remove `releasable`;
- remove hand-maintained `releaseTier`;
- derive dependency waves from `internalDependencies`;
- add `kind: Library | ShowcaseBundle`;
- add `proofProfile: Library | ShowcaseBundle`;
- add `family: Showcase`;
- validate project/package/tag/version-property agreement;
- validate unique identities and safe paths;
- validate known non-retired dependency edges and cycles;
- validate real project references and packed dependency metadata;
- reject a disabled target in a requested graph rather than silently omitting it.

### M1.2 Typed release tool

Create non-packable projects:

```text
tools/Pocok.ReleaseTool/Pocok.ReleaseTool.csproj
tests/Tooling/Pocok.ReleaseTool.Tests/Pocok.ReleaseTool.Tests.csproj
```

Add them to `Pocok.slnx`, not the package catalog.

Use centrally pinned:

- `NuGet.Protocol`;
- `NuGet.Versioning`.

Use .NET's `System.Reflection.Metadata` for portable PDB inspection unless current SDK evidence requires an explicit package.

The typed tool is authoritative for:

```text
catalog validate --catalog <path>
tag resolve --catalog <path> --tag <tag> [--output <json>]
graph write --catalog <path> --scope global|package --package-id <id> --output <json>
version preflight --catalog <path> --tag GLOBAL-v<version> --source <v3-url> --output <json>
manifest create --graph <json> --artifacts <dir> --commit <sha> --tag <tag> --output <json>
manifest verify --manifest <json> --artifacts <dir>
nuget verify --manifest <json> --source <v3-url> [--published-only]
nuget wait --package-id <id> --version <version> --source <v3-url> --timeout <timespan>
sourcelink verify --manifest <json> --repository-root <path> [--connected]
```

Use stable exit meanings:

- `0`: success;
- `2`: invalid invocation/configuration;
- `3`: policy or preflight rejection;
- `4`: unavailable or malformed external service.

Keep existing PowerShell entry points where they are public maintainer interfaces, but convert them to thin wrappers. They must not retain competing graph, SemVer, version-preflight, or provenance algorithms.

PackageSmoke and PublicReleaseAudit may remain PowerShell consumers of typed graph/manifest JSON.

### M1.3 Version policy

Read the NuGet V3 service index and flat-container/version resources through `NuGet.Protocol`. Include listed and unlisted versions where the service exposes them.

Use `NuGetVersion` and NuGet precedence for all comparisons.

Reject build metadata in release tags because it cannot create a distinct immutable NuGet identity.

For conflicts:

- name every conflicting package and observed maximum;
- preserve the requested prerelease channel where possible;
- return the lowest deterministic valid global version greater than every target;
- do not silently select or publish it.

Package-specific tag resolution becomes catalog-driven. `.github/workflows/publish.yml` may use a broad `*-v*` trigger excluding `GLOBAL-v*`, with the typed resolver rejecting unknown, retired, disabled, malformed, wrong-channel, or ambiguous tags before restore or authentication.

Keep the shared concurrency group and `queue: max`.

### M1 proof

Include offline fixtures for:

- empty/404 package histories;
- lower/equal/higher versions;
- stable and prerelease precedence;
- normalized and unlisted versions;
- malformed service responses;
- timeout;
- metadata rejection;
- disabled and wrong-channel targets;
- graph roots, diamonds, adapters, bundles, cycles, and deterministic order;
- wrapper parity.

Connected tests are read-only and explicitly categorized.

**Accept when:** schema v2 describes the current library graph, the typed tool is the sole release authority, wrappers have parity, and no `releasable` or hand-maintained tier logic remains.

## M2. Prove immutable exact artifacts and Source Link

### Candidate contract

For a candidate commit and tag:

1. write the exact graph and dependency waves;
2. generate exact synchronized version properties before restore;
3. restore/build/test once from clean inputs;
4. pack once into a clean candidate directory;
5. audit every package;
6. create a manifest containing:
   - commit and tag;
   - normalized version;
   - package IDs, kinds, policies, nodes, edges, and waves;
   - exact artifact names;
   - SHA-256 hashes;
   - nuspec repository metadata;
   - dependency metadata;
   - proof profile;
   - Source Link result;
   - consumer result;
7. never rebuild after manifest creation.

Ordinary `Library` packages require:

- `.nupkg` and `.snupkg`;
- DLL and XML documentation under the supported TFM;
- README, LICENSE, and NOTICE;
- exact dependency metadata;
- repository URL and commit;
- deterministic hashes;
- installed-package consumer proof.

Do not weaken this profile for bundles.

### Automated Source Link proof

Implement `sourcelink verify`:

1. open each portable PDB from the exact symbol package or bundle payload;
2. read Source Link mappings and document checksums;
3. require nuspec repository URL and commit to equal the candidate manifest;
4. resolve each mapped document at the exact local Git commit;
5. compare PDB checksums;
6. with `--connected`, fetch at least one commit-specific source URL per package and compare it;
7. install the candidate into a clean consumer;
8. produce a controlled stack frame;
9. verify file/line maps to a proven document.

If raw source access is unavailable, retain local Git-object proof but mark connected proof failed. Live release remains blocked.

### Candidate retention and recovery

Before the first package push:

- upload graph, manifest, hashes, nupkg/snupkg files, bundle files, and proof summaries as one Actions candidate artifact;
- attach the same exact files to a draft GitHub Release for the existing immutable tag;
- verify the uploaded/downloaded hashes.

Recovery:

- downloads retained assets;
- verifies tag commit, manifest, and hashes;
- compares already-published packages by repository provenance and package bytes/content identity where available;
- continues only missing packages;
- never rebuilds;
- never deletes, unlists, overwrites, or moves a tag.

**Accept when:** one immutable manifest ties every library artifact, symbol/PDB, dependency, repository commit, source checksum, and installed consumer to the same candidate.

## M3. Add typed Showcase coverage, composition, and bundle contracts

### Coverage ledger

Add:

```text
showcase/coverage.json
showcase/coverage.schema.json
```

Every non-retired `kind: Library` entry appears exactly once with a primary owner:

```json
{
  "packageId": "Pocok.Conversion",
  "evidenceKind": "SandboxScenario",
  "owner": "pocok.showcase.conversion",
  "scenario": "strict-integer",
  "publicApis": ["ValueConverter.Convert<T>"]
}
```

Allowed evidence kinds:

- `SandboxScenario`;
- `HostExercise`;
- `BoundedProbe`;
- `RecipeBuilder`.

Use `RecipeBuilder` for the lightweight Readiness, BackgroundWork, Modularity, Signals, and Subscriptions coverage where appropriate. The ledger must state that recipe evidence supplements package gates and is not runtime execution proof.

Add an acceptance runner that accepts only validated ledger IDs and writes a safe record containing:

- commit;
- composition mode;
- package/slice version;
- command/scenario ID;
- result;
- bounded duration.

Do not record source, arbitrary stdout, keys, secrets, credentials, absolute paths, cache roots, or user data.

### Composition record

Add versioned DTOs/schema to the existing Showcase publication tool. Every publish writes:

```text
Content/showcase-composition.json
```

The record includes:

- schema and Showcase API versions;
- composition mode;
- repository commit for local mode;
- exact package version for NuGet modes;
- host library versions;
- plugin IDs, bundle IDs, primary libraries, demonstrated library versions, relative directories, and bundle hashes.

Use only normalized forward-slash relative paths. Never include credentials, absolute paths, cache roots, extraction roots, or environment values.

### Bundle manifest

Each plugin owns:

```text
showcase.bundle.json
```

It declares:

- schema version;
- exact Showcase API version;
- module ID;
- sample package ID;
- primary library ID;
- all demonstrated/bundled library IDs;
- host-shared assemblies.

The pack process fills exact versions and per-file hashes in the packaged copy.

Validate before host startup:

- schema/API versions;
- IDs and uniqueness;
- safe relative paths;
- zip-slip and case-collision paths;
- entry assembly, deps file, module manifest, and content;
- host-shared versus private dependency conflicts;
- exact versions and hashes;
- unexpected files;
- catalog/project/nuspec/manifest/composition agreement.

The host reads only already-validated local records and files. It never downloads or chooses packages.

**Accept when:** the ten-plugin logical inventory is represented by validated coverage, composition, and bundle contracts, and malformed records fail before startup.

## M4. Package ten deterministic Showcase bundles

### Package profile

Each plugin becomes independently packable with package type:

```text
PocokShowcaseBundle
```

The bundle contains a deterministic tree such as:

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

Bundles are deployment artifacts, not compile packages:

- no `lib/` compile assets;
- no `.snupkg`;
- portable PDB remains in the main nupkg;
- bundle proof uses `proofProfile: ShowcaseBundle`;
- ordinary library requirements remain unchanged.

Exclude host-shared:

- `Pocok.Showcase.Contracts`;
- `Pocok.Showcase.Components`;
- `Pocok.Modularity.Contracts`;
- framework assemblies;
- other explicitly host-supplied identities.

Include exact private dependencies needed by the plugin. Sort entries, normalize timestamps/content, and require repeated pack to produce identical hashes.

### Exact internal edges

Every bundle has a direct edge to `Pocok.Modularity.Contracts` and all demonstrated/bundled Pocok libraries.

Required mapping:

| Bundle | Required demonstrated library edges |
|---|---|
| Conversion | `Pocok.Conversion` |
| Scripting | `Pocok.Scripting`, `Pocok.Scripting.JavaScript`, `Pocok.Scripting.CSharp`, `Pocok.Scripting.Python`, `Pocok.Conversion` |
| Licensing | `Pocok.Licensing`, `Pocok.AppDefaults.Licensing`, `Pocok.AppDefaults` |
| AppDefaults.Logging | `Pocok.AppDefaults`, `Pocok.AppDefaults.Logging`, `Pocok.AppDefaults.Logging.Serilog` |
| Localization | `Pocok.Localization`, `Pocok.BackgroundWork` |
| Readiness | `Pocok.Readiness` |
| BackgroundWork | `Pocok.BackgroundWork` |
| Modularity | `Pocok.Modularity.Contracts`, `Pocok.Modularity`, `Pocok.AppDefaults.Modularity`, `Pocok.AppDefaults` |
| Signals | `Pocok.Signals`, `Pocok.Conversion` |
| Subscriptions | `Pocok.Subscriptions` |

Inspect actual project/package references before finalizing edges. Add only dependencies genuinely required by code or payload; do not omit demonstrated direct libraries.

### Proof

For every bundle:

- pack twice and compare hashes;
- inspect package type, nuspec, and payload tree;
- validate file hashes and Source Link;
- reject traversal/collision/shared-copy fixtures;
- extract into an empty directory;
- load through the published host;
- run its acceptance entry;
- install/download from an isolated temporary feed;
- resolve its package-specific tag;
- verify catalog, project, nuspec, bundle, coverage, and composition agreement.

**Accept when:** all ten bundles are alpha-eligible, independently extractable and loadable, deterministic, and free of host-shared duplication and path/secret leakage.

## M5. Implement three deterministic source modes and expand release capacity

### Configuration

Use standard .NET configuration:

```text
Showcase__CompositionMode=LocalLibrariesLocalSamples
Showcase__PackageVersion=
Showcase__NuGetSource=https://api.nuget.org/v3/index.json
Showcase__TrustedScriptEnginesEnabled=false
Showcase__RequireCompleteCatalog=false
```

Modes:

1. `LocalLibrariesLocalSamples`;
2. `NuGetLibrariesLocalSamples`;
3. `NuGetLibrariesNuGetSamples`.

Rules:

- local mode forbids `PackageVersion`;
- NuGet modes require one exact SemVer;
- ranges, latest selection, and per-package version maps are forbidden;
- source is HTTPS V3 in Production;
- a local absolute folder feed is allowed only for local/CI rehearsal;
- invalid mode/version/source disagreement fails before publication;
- trusted script engines default false in every environment.

### LocalLibrariesLocalSamples

Build host and plugins from the checkout using ProjectReferences.

- isolate NuGet configuration and packages;
- prevent public Pocok packages from masking missing local outputs;
- record current assembly informational versions and Git commit;
- do not use stale global caches as plugin input.

### NuGetLibrariesLocalSamples

Keep local Showcase source but condition every public Pocok dependency between:

- local ProjectReference;
- exact PackageReference.

Publish the host and local plugin source against one exact package version from an isolated feed.

Prove every public Pocok dependency came from that source/version through assets files and generated composition.

### NuGetLibrariesNuGetSamples

Do not discover, restore, build, or read plugin source projects.

- build the host against exact NuGet library packages;
- download ten exact bundle IDs/version through `NuGet.Protocol`;
- verify candidate/release hashes when a manifest is supplied;
- validate package and bundle manifests;
- safely extract only the expected `tools/pocok-showcase/<module-id>/` tree;
- stage the exact plugin inventory;
- ensure the running host makes no NuGet request.

### Workflow capacity upgrade

The complete graph has twenty-eight packages and cannot use the current eighteen-target single-job model.

Upgrade the existing global workflow; do not create a competing release path.

Use two immutable stages:

#### Prepare stage

- resolve and preflight the complete graph;
- restore/build/test/pack/audit once;
- run library and bundle proof;
- create graph, waves, manifest, and hashes;
- upload the complete candidate as an Actions artifact;
- attach all assets to a draft GitHub Release;
- expose only manifest identifiers and hashes to later jobs.

#### Publish stage

- download the exact candidate;
- verify artifact and manifest hashes;
- authenticate immediately before publication;
- process deterministic dependency waves;
- push explicit manifest paths only;
- wait with bounded backoff and `Retry-After` support;
- require exact flat-container visibility;
- download and verify repository commit and package identity;
- run clean generated consumers or bundle load proof;
- stop later waves on failure.

Split jobs or reusable workflow calls so hosted-job duration remains bounded. Do not merely delete the eighteen-package guard.

Recovery reruns the publish stage from retained exact assets. No build is allowed in recovery mode.

Both package/global workflows retain:

```yaml
concurrency:
  group: pocok-publication
  cancel-in-progress: false
  queue: max
```

### Temporary-feed proof

Create one isolated folder candidate feed and an in-process HTTP V3 fixture for propagation/recovery tests.

Publish all three Showcase modes from clean isolated caches and compare:

- package/plugin IDs;
- exact versions;
- routes;
- resources;
- scenarios;
- coverage;
- logical composition.

Paths and hashes may differ only where the schema permits them.

**Accept when:** all modes are equivalent, full mode reads no plugin source, the host makes no NuGet request, and the twenty-eight-package workflow can prepare once and safely publish/resume exact retained artifacts.

## M6. Deepen the remaining MVP scenarios and add browser/Docker gates

### Licensing scenario

Strengthen the existing Licensing plugin with a real bounded per-run lifecycle:

1. generate an ephemeral signing key pair;
2. construct a deterministic synthetic license;
3. sign it;
4. read and verify with the public key;
5. validate against deterministic context;
6. reject one tampered signature or untrusted key.

Dispose key material immediately.

Display only:

- safe validation code;
- bounded public claim summary;
- key ID;
- algorithm;
- bounded timing.

Never return or log PEM, private parameters, complete envelopes, PSKs, encryption secrets, machine identifiers, or customer data.

Add a bounded `Pocok.AppDefaults.Licensing` block/warn/revalidation host-builder probe using synthetic in-memory material.

### Readiness scenario

Retain the Release Readiness recipe builder and add one real deterministic scenario that creates a fresh `ReadinessSource` per run and demonstrates:

- ready;
- failed startup;
- cancelled startup;
- shutdown/stopped;
- a new restart cycle.

Use deterministic `TimeProvider` and real snapshots/transitions. Never mutate the host's readiness singleton.

### Other recipe plugins

BackgroundWork, Modularity, Signals, and Subscriptions remain recipe builders for MVP. Do not add full runtime demonstrations unless a real implementation defect requires a bounded proof fixture. Package tests and consumers remain their technical gates.

### Browser tests

Create a pinned Playwright NUnit project.

Chromium runs on Ubuntu and starts a published host on an OS-assigned loopback port. It waits for readiness, captures console/page/request failures and a trace on failure, and always terminates the process.

Cover:

- all generated routes and Home/System navigation;
- English/Hungarian rendering;
- light/dark theme;
- wide/narrow layout without page overflow;
- sample selection, same-sample reset, edits, and execution;
- Monaco multiline typing and mid-document caret preservation;
- debounce and exact flush before Run/reset/engine switch;
- JavaScript success and validator rejection;
- public C#/Python unavailable states;
- explicit trusted/local C#/Python success;
- Licensing lifecycle;
- Readiness lifecycle;
- generated recipe changes for all five builders;
- in-app bounded log behavior;
- SignalR reconnect without lost committed editor value;
- no CDN or unexpected external browser request.

### Docker

Update the Docker path for all composition modes.

The final runtime image contains no:

- SDK;
- NuGet cache/config;
- plugin source;
- feed credential;
- compiler output directory;
- Python runtime;
- trusted-engine enablement.

Private adapter/worker assets may exist in the Scripting bundle but are not static assets and remain disabled publicly.

Build and smoke:

- all-local image;
- full-NuGet image from temporary feed;
- health/live and health/ready;
- generated routes;
- culture/theme;
- JavaScript run/rejection;
- public C#/Python unavailability;
- reconnect.

**Accept when:** Linux/Windows source and composition proof, Ubuntu Chromium, and local/full-NuGet Docker pass with clean caches and no external editor assets.

## M7. Rehearse and perform the complete synchronized release

### Zero-push rehearsal

At one unused prerelease version:

- construct the complete twenty-eight-package candidate once;
- run all library and bundle proof;
- run all three Showcase modes from clean inputs;
- create the draft-release-shaped asset set locally;
- exercise publication, delayed visibility, partial failure, and resume against disposable feeds;
- verify manifest reruns are byte-identical;
- confirm no live tag/package/release was created.

The rehearsal manifest includes:

- commit/tag/version;
- policy for every target;
- nodes, edges, and waves;
- exact artifact names and hashes;
- package kinds;
- repository and Source Link proof;
- installed library consumer proof;
- bundle load proof;
- coverage ledger;
- three composition records;
- browser/Docker results.

It contains no credentials, absolute paths, caches, source content, or secret feed URLs.

### Approval

Present:

- exact commit;
- proposed global tag;
- twenty-eight targets;
- graph/waves;
- manifest hash;
- zero-push summary;
- public version preflight;
- previous library-only release relationship;
- external prerequisites.

Do not create or push the tag without explicit approval.

### Publication

After approval:

1. rerun connected version preflight;
2. stop and request new approval if the valid version changes;
3. create one annotated immutable `GLOBAL-v*` tag;
4. run prepare stage;
5. verify retained candidate assets;
6. publish dependency waves;
7. finalize the GitHub Release only after all targets are verified.

On failure, use only retained-candidate recovery. Report published, verified, failed, blocked, and pending targets.

### Clean nuget.org Showcase composition

After successful publication:

- create an isolated NuGet config and packages directory;
- publish only `NuGetLibrariesNuGetSamples`;
- use exact released version and nuget.org;
- do not build or read local plugin projects;
- compare downloaded hashes with the release manifest;
- verify all routes, bilingual resources, coverage, public script availability, browser tests, and Docker contents.

**Accept when:** all twenty-eight packages and the GitHub Release are public at one exact version, and clean full-NuGet composition matches the release manifest without local project/cache input.

## M8. Roll out Render through an exact checked PR

### Rollout PR

Prepare a PR that records the exact synchronized version and uses full-NuGet composition.

Expected non-secret configuration:

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

The exact reviewed PR commit is the deployment approval. Do not mutate Render values manually to create an unreviewed composition.

Before merge, present:

- rollout commit;
- package version;
- composition hash;
- Docker image digest;
- route list;
- previous known-good Render deployment ID.

### Live proof

After checks pass and Render deploys:

- allow for expected cold start;
- verify live and ready health;
- verify every generated route;
- verify culture and theme;
- run JavaScript success/rejection;
- verify C#/Python unavailability;
- verify log and diagnostic safety;
- verify reconnect;
- compare live composition/version output with the approved record;
- confirm no paths, cache/source details, license secrets, keys, worker content, or credentials are served.

### Failure and rollback

- If build/readiness fails, require Render to keep the previous successful deployment.
- If post-switch smoke fails, roll back to the recorded previous deploy.
- Record both deployment IDs and the failed check.
- Disable automatic rollout until a corrective PR is approved.
- If Render access is unavailable, retain exact locally smoked image and composition and classify M8 as externally blocked. Local Docker is not deployment evidence.

**Accept when:** the approved full-NuGet composition is live and matches the release manifest, or rollback is verified and the exact corrective action is recorded.

## External prerequisites and deterministic fallbacks

| Prerequisite | Required behavior | Fallback and effect |
|---|---|---|
| .NET 10 and PowerShell 7 | Repository-pinned versions | Record environment block; no closure claim |
| CPython 3.14 | Trusted/local browser and composition proof | Public mode remains unaffected, but trusted test gate stays open |
| NuGet V3 | Connected preflight, download, publication, clean restore | Temporary feed permits rehearsal only |
| Trusted Publishing | Authenticate immediately before push | Candidate remains retained and draft release remains unpublished |
| GitHub Actions/Release | Prepare/publish stages and retained assets | Local rehearsal only; no live release claim |
| Raw commit source access | Connected Source Link checksum | Local Git proof retained; live release blocked |
| Playwright Chromium | Pinned Ubuntu browser install | Component/smoke evidence retained; M6 open |
| Docker/BuildKit | Local and full-NuGet images | Published-directory smoke retained; deployment gate open |
| Render access | Checked rollout and rollback | Exact local image retained; M8 externally blocked |

## Final MVP acceptance

- [ ] Release Readiness evidence remains current or is reconfirmed after material library changes.
- [ ] Schema v2 separates maturity, publication policy, kind, and proof profile.
- [ ] Typed release tool is the only graph/version/provenance authority.
- [ ] Exact library and bundle artifacts are bound to one immutable manifest.
- [ ] Source Link and installed-candidate proof pass.
- [ ] Ten deterministic Showcase bundles are catalogued and independently loadable.
- [ ] Twenty-eight-package capacity is implemented through prepare and exact wave-publish stages.
- [ ] Recovery reuses retained candidate assets without rebuild.
- [ ] Coverage, bundle, composition, catalog, nuspec, and project metadata agree.
- [ ] All three Showcase source modes are equivalent.
- [ ] Full mode builds no plugin source and the running host contacts no NuGet service.
- [ ] Licensing and Readiness real scenarios pass.
- [ ] Recipe builders remain honest constrained syntax demonstrations.
- [ ] Ubuntu Chromium and local/full-NuGet Docker pass.
- [ ] One approved global prerelease contains all libraries and all ten bundles.
- [ ] Clean nuget.org composition equals the release manifest.
- [ ] The exact reviewed Render revision is live, or a verified rollback/external block is recorded.
- [ ] Remaining work is explicitly post-MVP and does not hide a correctness, release-safety, or deployment blocker.
