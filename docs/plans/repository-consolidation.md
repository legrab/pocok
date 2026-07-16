# Pocok repository evaluation, implementation retrospective, and stabilization plan

**Original evaluation date:** 2026-07-15
**Current state:** V2 implementation candidate on `dev/bg/est` with consolidation Waves A through D applied.
**Historical inputs:** `origin.zip` (reference only).
**Primary objective:** turn Pocok into a small, credible portfolio of reusable .NET packages and public application-default configurators without preserving low-value abstractions or copying application-specific legacy code.

> **Status update:** A previous .NET 10 and PowerShell 7 stabilization run recorded 182 passing tests. The V2 follow-up finalized pending Modularity WIP and implemented package-semantic Wave C and AppDefaults-policy Wave D. The environment used for those latest edits had no .NET or PowerShell runtime, so the exact HEAD remains an implementation candidate until the acceptance matrix is rerun.

## Evidence levels

Use these labels when reviewing this document:

- **Confirmed structurally:** visible directly from files, project references, package metadata, Git history, and workflow definitions.
- **Confirmed defect by source inspection:** contradictory code or tests that do not require execution to prove the problem.
- **Plausible but unverified:** implementation appears coherent but requires compilation or runtime tests.
- **Not evaluated:** behavior that depends on GitHub Actions, nuget.org trusted publishing, operating-system-specific loading, trimming, or package installation.

The legacy origin remains architectural and behavioral evidence only. Its source contains proprietary headers and must not be copied. Reimplement only independently justified generic behavior.

---

# Executive note

## Current implementation candidate

The generated repository contains eight active packable projects:

| Package | Intended status | Current confidence |
|---|---|---|
| `Pocok.Conversion` | Initial public release | Previously compiled and tested. Candidate-scoped pack, smoke, audit, sample, and trim checks now require one fresh run. |
| `Pocok.Readiness` | Initial public release | Previously compiled and tested, including concurrency coverage. Package-semantic changes require one fresh release rehearsal. |
| `Pocok.AppDefaults` | Initial public release | Previously compiled and tested. Its composition contract remains deliberately small and unchanged. |
| `Pocok.AppDefaults.Logging` | Initial public release after `Pocok.AppDefaults` | Duplicate, options, provider, override, and console semantics are now explicit and tested in source; executable rerun pending. |
| `Pocok.AppDefaults.Logging.Serilog` | Initial public release after `Pocok.AppDefaults` | Sink-free policy and duplicate/options behavior are explicit; executable host and package rerun pending. |
| `Pocok.Modularity.Contracts` | Experimental, non-releasable | Implemented for evaluation. Keep gated until Wave E passes on Linux and Windows. |
| `Pocok.Modularity` | Experimental, non-releasable | Pending WIP contradictions were corrected. Highest-risk runtime area and still not a release candidate. |
| `Pocok.AppDefaults.Modularity` | Experimental, non-releasable | Configurator policy is aligned with other AppDefaults packages, but release depends on Wave E. |

The following package shapes were removed from the active repository:

- `Pocok.Primitives`
- `Pocok.Hosting`
- `Pocok.Conversion.Abstractions`

`Pocok.Primitives` was already published and should be deprecated on nuget.org with a migration link. Do not publish a forwarding package.

## What the generated implementation got right

The candidate establishes a useful target shape:

- package-owned failures replace the generic Primitives dependency;
- Conversion contracts and implementation are consolidated;
- Hosting is renamed to the more accurate Readiness identity;
- AppDefaults is separated from capability packages;
- provider-neutral logging and Serilog policy are separate;
- Modularity is startup-only, trusted, manifest-led, and explicitly non-releasable;
- package metadata is catalog-driven;
- smoke testing distinguishes local closure from publication-shaped restore;
- release workflows select an exact candidate package;
- the repository contains migrations, ADRs, samples, package consumers, and organized Git history;
- internal reusable code is kept out of a public `Common` or `Utils` package.

These design decisions should be preserved unless executable evidence disproves them.

## V2 resolution status

The historical defects below explain why the first one-shot was not sufficient. Their structural remedies are now present:

- stale split-assembly Conversion assertions were replaced by member-level Verify/PublicApiGenerator snapshots;
- `Pocok.Core.slnx` isolates the initial release graph from experimental Modularity;
- package closure is catalog-resolved and drives candidate packing, local-feed smoke, publication smoke, and audit;
- AppDefaults duplicate, options, provider, and override semantics are deliberate rather than accidental;
- Modularity remains non-releasable and has a separate Wave E proof gate.

The remaining task is executable verification of the latest edits, not another broad architectural rewrite.

## What went wrong in the one-shot

The one-shot failed as a release implementation because implementation breadth exceeded available verification.

### 1. No compiler or test runner was available

The environment had neither .NET 10 nor PowerShell 7 and could not obtain them. The repository was therefore synthesized from source inspection alone. Static checking cannot validate:

- C# compilation and analyzers;
- MSBuild project graph behavior;
- test semantics;
- `dotnet pack` output;
- MinVer and package-validation behavior;
- PowerShell syntax and runtime semantics;
- clean NuGet restore;
- GitHub Actions or trusted publishing.

The process should have stopped calling packages “releasable” once executable validation became impossible. The correct label was “intended initial release set.”

### 2. A stale Conversion test survived the assembly merge

`tests/Unit/Conversion.Tests/ConcurrencyAndApiTests.cs` still assumes that `IValueConverter` and `ValueConverter` live in separate assemblies.

After consolidation, both types are in `Pocok.Conversion`. The test simultaneously expects:

- the assembly containing `IValueConverter` to expose only the old abstraction types; and
- the same assembly containing `ValueConverter` to expose only `ValueConverter`.

Both expectations cannot be true. The same test also omits the newly added `MaximumDepth` and `MaximumCollectionItems` properties from its `ConversionContext` member expectation.

This is a confirmed failing test, not merely a risk.

### 3. Release isolation is incomplete

`.github/workflows/publish.yml` restores, builds, tests, and packs `Pocok.slnx`, which includes every experimental Modularity project and fixture. The packaging test project also references every active package.

Consequences:

- a compile or test failure in non-releasable Modularity blocks `Pocok.Conversion`, `Pocok.Readiness`, and AppDefaults releases;
- the promise that Modularity can remain gated while the initial set ships is not operationally true;
- the workflow packs the whole solution even though the original plan explicitly preferred the candidate and its transitive internal closure.

This does not make the package graph concept wrong, but the workflow boundary must be redesigned.

### 4. API compatibility enforcement is weaker than planned

The current `PublicAPI.Shipped.txt` tests compare exported type names only. They do not protect:

- methods and overloads;
- parameter names and default values;
- return types;
- generic constraints;
- accessibility of members;
- nullability annotations;
- enum values;
- constructors and properties.

The stale Conversion test attempted to cover a few members manually and immediately drifted. Replace this mixture with one supported member-level API compatibility mechanism, then keep small semantic tests only for intentional behavioral contracts.

### 5. Completion records overstate certainty

The implementation report calls five packages “release-ready” while also acknowledging that no executable command ran. The Git history is useful, but commit count is not evidence of correctness. The next agent must treat all checked implementation phases as “source changes applied,” not “validated complete.”

## Process rules learned from this attempt

- **Toolchain first.** Verify the exact SDK, PowerShell, restore access, and basic baseline commands before changing architecture.
- **Green checkpoints, not decorative commits.** Every consolidation phase should compile and run its focused tests before the next phase begins. A clean Git history cannot compensate for an unbuilt repository.
- **Search for invalidated assumptions after merges.** When projects, assemblies, namespaces, or public contracts are merged, inspect every test, reflection assertion, smoke consumer, package ID, migration guide, and workflow that encoded the old boundary.
- **Release eligibility is an observed state.** A catalog boolean or missing tag trigger only prevents publication. It does not prove the package can build, pack, restore, or run.
- **Experimental code needs a separate failure domain.** Keeping a package non-releasable is insufficient if its projects are still mandatory dependencies of every release job.
- **Use one API compatibility system.** Parallel handwritten inventories and ad hoc reflection tests drift quickly. Adopt one supported member-level mechanism and use ordinary tests only for semantics.
- **Reduce claims when execution is blocked.** When the environment cannot compile, stop at an explicitly labeled implementation spike. Do not convert static plausibility into “release-ready” language.
- **Prefer a smaller proven release.** It is better to ship Conversion, Readiness, and the AppDefaults base after complete package validation than to carry an unverified plugin framework through every release path.
- **Keep evidence in the repository.** Record command output, first failing commit, deviations, and final green checks in the implementation ledger so another agent does not repeat the same investigation.

## Next executable acceptance session

The next agent should verify the V2 implementation rather than redesign it speculatively.

### Mandatory order

1. Verify the .NET 10 and PowerShell 7 environment.
2. Run restore, formatting, build, focused tests, full tests, and samples on the exact HEAD.
3. Pack the full repository once and run catalog validation, local-closure smoke, and full package audit.
4. Rehearse each initial release candidate with generated release versions, exact closure packing, both smoke modes, and candidate-scoped audit.
5. Fix only factual failures exposed by those commands and preserve the documented package boundaries.
6. Run Linux and Windows CI.
7. Release the five initial packages in dependency order.
8. Treat Wave E as a separate task before reconsidering Modularity release eligibility.

### First commands

Run each separately so the first factual failure is preserved:

```pwsh
dotnet --info
$PSVersionTable

dotnet restore Pocok.slnx
dotnet build Pocok.slnx --configuration Release --no-restore
dotnet test Pocok.slnx --configuration Release --no-build
dotnet format Pocok.slnx --verify-no-changes --no-restore
dotnet pack Pocok.slnx --configuration Release --no-build --output artifacts/packages

./tools/PackageCatalog/Test-PackageCatalog.ps1
./tools/PackageSmoke/Invoke-PackageSmoke.ps1 -NoPack -Mode LocalClosure
./tools/PublicReleaseAudit/Invoke-PublicReleaseAudit.ps1
```

Do not add more package content before this matrix is green. Compilation, PowerShell parsing, package restore, and runner behavior remain the authoritative evidence.

## Recommended stabilization waves

### Wave A: make the complete repository compile

Expected first repair:

- update or remove the obsolete split-assembly assertions in `ConcurrencyAndApiTests`;
- retain one authoritative exported API baseline;
- update `ConversionContext` member expectations if semantic tests remain.

Then address every compiler and analyzer failure without broad refactoring.

### Wave B: make the initial release set independently verifiable

Create an explicit core release boundary. Either:

- introduce a small release solution containing the five intended packages, their focused tests, architecture checks, and candidate-scoped packaging checks; or
- make the catalog generate the exact project and test closure for the candidate.

The first option is simpler and more reliable for this repository size.

Do not let experimental Modularity block these releases:

```text
Pocok.Conversion
Pocok.Readiness
Pocok.AppDefaults
Pocok.AppDefaults.Logging
Pocok.AppDefaults.Logging.Serilog
```

Pack only the candidate and the internal package closure required for local-feed testing. Audit only that closure. Publication mode should place only the candidate in the local feed and resolve already released internal dependencies from nuget.org.

### Wave C: validate package semantics

For each initial package:

- install it into a clean consumer from a local feed;
- compile and run the sample;
- inspect the `.nupkg` and `.snupkg`;
- verify repository metadata and Source Link;
- prove no project reference or retired package leaks;
- verify exact dependency versions;
- verify package README rendering outside the repository.

### Wave D: review AppDefaults behavior

Keep the current conservative direction, but explicitly decide and test:

- whether duplicate application is first-call-wins, last-call-wins, merged, or rejected;
- whether a second configurator with different options should be silently ignored;
- how application registrations after defaults override filters and providers;
- whether option objects should be registered directly or through standard options abstractions;
- whether the default simple-console registration is appropriate for libraries named “defaults.”

Do not turn AppDefaults into a second host framework.

### Wave E: prove Modularity separately

Keep all three packages non-releasable until:

- plugin dependencies load from plugin-local directories;
- shared contract identity is preserved;
- required and optional failures are deterministic;
- staged registration is atomic;
- duplicate IDs are diagnosed;
- Windows-only plugins are skipped on Linux and vice versa;
- unmanaged dependency behavior is tested where relevant;
- samples produce a deployable plugin directory automatically;
- Linux and Windows fixture tests pass;
- trimming and NativeAOT limitations are documented;
- trusted-code security boundaries are explicit.

The existing implementation is a useful spike, not a proven public contract.

## Plan-status mapping

The remainder of this document is the original architectural evaluation and implementation plan. Read it as a design record. The generated baseline applied much of it, but no section is considered complete until its acceptance checks execute.

| Original section | Generated state | Next action |
|---|---|---|
| 1.4 to 1.6 global consolidation | Mostly applied structurally | Verify project graph, package boundaries, docs, and tests. |
| 2 release redesign | Concept implemented | Decouple releases from experimental projects and execute both smoke modes. |
| 3 internal reusable code | Policy and architecture tests added | Verify no linked source leaks and avoid adding helpers without repeated use. |
| 4 Primitives | Removed | Deprecate published package and verify no dependency remains. |
| 5 Readiness | Renamed and expanded | Compile, stress test, review state semantics, then package-smoke. |
| 6 Conversion abstractions | Merged | Remove stale split-assembly assumptions and validate migration docs. |
| 7 Conversion | Expanded significantly | Highest immediate test-repair priority; benchmark and trim checks remain unexecuted. |
| 8 AppDefaults | Implemented | Validate host behavior, duplicate semantics, overrides, package consumers. |
| 9 Modularity | Implemented as experimental spike | Keep non-releasable; run full fixture matrix and reconsider public API after evidence. |
| 10 to 12 extraction and target layout | Applied selectively | Confirm no origin source or proprietary naming leaked. |
| 13 to 15 implementation contract and acceptance | Source changes applied | Re-run as executable acceptance, not checklist review. |
| 16 to 18 future questions and references | Still valid | Revisit only after the repository is green. |

---

# 1. Global repository evaluation

## 1.1 Relevance and value

### Personal value

The repository can provide high personal value if it centralizes only repeated, stable decisions:

- strict runtime conversion semantics used across domain-heavy applications;
- restartable readiness coordination for services and integrations;
- consistent logging and configuration setup across many applications;
- startup-time loading of supplier, protocol, codec, or device modules;
- release and package engineering patterns that can be reused as a reference.

The configurator family is especially valuable even when it has limited public novelty. Eliminating ten slightly different logging bootstraps is real maintenance reduction. Public differentiation is not the only value metric for a public package. A public, source-visible, dependency-light default package can be useful precisely because it is boring and predictable.

### Public value

Public value requires more discipline:

- Capability packages must solve a reusable problem better than a short application-local implementation.
- Maintainer-default packages must save setup effort while remaining transparent and easy to replace.
- Every package must have a smaller dependency and conceptual cone than the problem it solves.
- The repository must show judgment about what not to publish.

The current repository already demonstrates release discipline. The next step is to demonstrate architectural discipline by reducing the package count before expanding it.

## 1.2 Architectural strengths

- Projects are small and independently packable.
- The solution avoids a large runtime dependency graph.
- Nullability and warnings are strict.
- Tests are substantial relative to implementation size.
- Packaging includes symbols and repository metadata.
- External-consumer smoke projects are the correct kind of release test.
- Workflow actions are commit-pinned.
- Publication policy, security policy, stewardship, notices, and contribution guidance exist.
- The current Conversion implementation avoids serializer fallback, a strong and explicit design choice.
- Current Hosting is materially better than the origin's simple `TaskCompletionSource` hosted-service base class.

## 1.3 Architectural concerns

### The package graph is driven by extracted code, not public capability boundaries

`Pocok.Primitives` and `Pocok.Conversion.Abstractions` make the portfolio look more modular while adding little independent value. They create versioning, restore, release-order, documentation, and compatibility obligations without creating a real ecosystem boundary.

### Release knowledge is duplicated

Package IDs, project paths, tag prefixes, dependency allowlists, smoke fixtures, workflow names, and publication state are spread across:

- project files;
- four nearly identical publishing workflows;
- `Invoke-PackageSmoke.ps1`;
- `Invoke-PublicReleaseAudit.ps1`;
- `README.md`;
- `PUBLICATION.md`.

This is already inconsistent. `Conversion.Abstractions` is triggered by `conversion.abstractions-v*`, but its project declares `MinVerTagPrefix` as `conversion-v`. Both Conversion projects therefore share the same MinVer prefix despite having separate workflows.

### The current audit hardcodes dependency versions

`Invoke-PublicReleaseAudit.ps1` contains exact internal dependency versions. This is brittle and requires code changes for normal package releases. Dependency IDs can be policy-controlled, but exact versions should be derived from package metadata and staged package artifacts, not manually copied into a script.

### API baselines are custom and incomplete

The reflection-based API baseline tests are useful as an early guard, but they do not fully model:

- nullable annotations;
- generic constraints;
- parameter names and default values;
- `ref`, `in`, and `out`;
- attributes;
- type forwarding;
- assembly identity and compatibility;
- package-level compatibility.

Use supported package validation and API compatibility baselines as the release authority. Custom human-readable API snapshots may remain supplementary.

### Packaged documentation contains repository-relative links

Package READMEs such as Hosting and Primitives link to `../../docs/...`. Those links are valid in the source tree but broken when the README is rendered from the NuGet package. Package READMEs must use canonical absolute repository URLs or include the referenced documentation in the package.

### Documentation is behind the implemented state

The root README and publication policy still describe only Primitives as publishable and refer to old planned packages such as `Pocok.Logging.*`. They do not represent the three added workflows or the intended AppDefaults and Modularity identities.

## 1.4 Global recommendation

**Stabilize and consolidate before adding capability.**

Required order:

1. Fix the release harness without weakening it.
2. Establish one authoritative package catalog and reusable publishing workflow.
3. Preserve existing behavior with migration tests.
4. Remove Primitives dependencies.
5. Merge Conversion.Abstractions into Conversion.
6. Rename and stabilize Hosting as Readiness.
7. Add AppDefaults and Logging.
8. Add Modularity.
9. Add only evidence-backed extensions.
10. Publish after clean-room package, compatibility, documentation, and sample checks pass.

## 1.5 What to change in existing repository code

- Add machine-readable package catalog metadata.
- Replace four duplicated publishing workflows with thin tag triggers invoking one reusable workflow.
- Add dependency-closure staging and two smoke modes.
- Publish and attach only the exact candidate package and symbols package.
- Derive dependency audits instead of hardcoding exact versions.
- Replace custom API compatibility authority with supported tooling.
- Fix all packaged README links.
- Update README, publication policy, package state, and diagrams.
- Add architecture tests for package dependencies, package classification, shared-source visibility, and packability.
- Remove stale plans that have been superseded.
- Keep net10.0 only until a real consumer requires multi-targeting.

## 1.6 What to extend globally

- One synthetic sample per capability area.
- A small benchmark project for Conversion.
- Plugin fixture builds for Modularity.
- A reference application showing AppDefaults, Readiness, Conversion, and Modularity together without requiring all packages.
- ADRs for package classification, result ownership, configurator behavior, and plugin trust boundaries.
- Upgrade guidance and package deprecation documentation.
- Automated checks that documentation and package catalog agree.

---

# 2. Immediate release failure and required packaging design

## 2.1 Root cause

The current publish workflow for Hosting does this:

```text
pack only Pocok.Hosting
restore a synthetic consumer only from artifacts/packages
```

The produced Hosting package declares `Pocok.Primitives` as a NuGet dependency because project references are represented as package dependencies during packing. `Pocok.Primitives` is not present in that local feed, so restore fails with `NU1101`.

The same issue applies to:

- `Pocok.Conversion.Abstractions` requiring `Pocok.Primitives`;
- `Pocok.Conversion` requiring `Pocok.Conversion.Abstractions`, and transitively Primitives.

This is expected NuGet behavior.

## 2.2 Do not use these incomplete fixes

### Do not bundle referenced project DLLs into the parent package

That hides package boundaries, duplicates assemblies, breaks independent versioning, and creates type identity problems.

### Do not weaken the only smoke test by adding nuget.org and stopping there

It proves the candidate can restore against published dependencies, but no longer proves that the locally built dependency closure is internally consistent.

### Do not pack the whole solution into the release directory and publish a wildcard

Current workflows use:

```text
gh release create "$RELEASE_TAG" artifacts/packages/*
```

If the solution is packed into that directory, the GitHub release will attach unrelated dependency packages. A broad NuGet push wildcard can also select more than intended.

## 2.3 Required staging layout

Use clean directories for every run:

```text
artifacts/
  closure-feed/       # full locally built package closure
  candidate-feed/     # only the exact candidate nupkg and snupkg
  reports/
```

No step may reuse old artifacts.

## 2.4 Required smoke modes

### Mode A: local closure smoke

Purpose: prove the repository produces a coherent set of packages from the same source state.

1. Determine the candidate package's transitive internal package closure from the package catalog/project graph.
2. Pack those projects into `artifacts/closure-feed`.
3. Restore the candidate consumer with only `closure-feed` as a source.
4. Build and run the consumer.
5. Use an isolated packages directory and disable HTTP cache where practical.

### Mode B: publication smoke

Purpose: prove the candidate is publishable independently at this moment.

1. Copy only the exact candidate package into `candidate-feed`.
2. Restore the consumer with:
   - `candidate-feed`;
   - nuget.org.
3. The candidate version must not already exist on nuget.org, ensuring it comes from the local feed.
4. Every Pocok dependency must resolve from nuget.org.
5. Build and run the consumer.

If Mode A passes and Mode B fails, the package graph is internally coherent but one or more dependencies have not been published in the required order.

## 2.5 Required release order while the current graph exists

Temporary release order:

```text
Pocok.Primitives
  -> Pocok.Hosting
  -> Pocok.Conversion.Abstractions
      -> Pocok.Conversion
```

Hosting and Conversion.Abstractions can be released in either order after Primitives. Conversion must follow Conversion.Abstractions.

This release-order burden is one reason to remove Primitives and merge Conversion.Abstractions. The target graph should not require synchronized releases for basic internal DTOs.

## 2.6 Package catalog

Add `eng/packages.json`, or a similarly simple reviewed format:

```json
{
  "packages": [
    {
      "id": "Pocok.Conversion",
      "project": "src/Conversion/Pocok.Conversion.csproj",
      "tagPrefix": "conversion-v",
      "class": "Capability",
      "tier": "Experimental",
      "publish": true,
      "consumer": "ConversionConsumer",
      "allowedPocokDependencies": []
    }
  ]
}
```

Required fields:

- package ID;
- project path;
- tag prefix;
- package class;
- publication tier;
- whether active and publishable;
- smoke consumer;
- allowed internal package dependency IDs;
- replacement package for retired IDs;
- sample path;
- public API baseline path.

The catalog is the source of truth for release scripts, audits, architecture tests, and generated package tables in documentation. Project files retain NuGet metadata, but CI verifies catalog and project metadata agree.

## 2.7 Reusable publishing workflow

Each package keeps a thin trigger workflow because trusted publishing policies identify workflow files. The trigger passes only the package catalog key to a reusable workflow.

The reusable workflow must:

1. verify the tag matches the catalog prefix;
2. restore locked;
3. build and test the whole solution;
4. calculate the candidate version;
5. create clean artifact directories;
6. pack the candidate and its local closure;
7. verify exact filenames and package IDs;
8. run local closure smoke;
9. run publication smoke;
10. run package content, dependency, secret, license, Source Link, symbols, and API compatibility audits;
11. push only the exact candidate `.nupkg` and `.snupkg`;
12. attach only those two exact files to the GitHub release;
13. emit a machine-readable release report.

## 2.8 Specific current defects to correct

- Change `Pocok.Conversion.Abstractions` tag prefix to match its temporary workflow, or remove the workflow as part of the immediate merge. Do not retain a project triggered by `conversion.abstractions-v*` while MinVer searches for `conversion-v*`.
- Remove exact dependency versions from `Invoke-PublicReleaseAudit.ps1`.
- Correct formatting inconsistency in the Hosting dependency allowlist.
- Restrict all release wildcards to the exact package.
- Make package selection deterministic. Do not select the newest file by modification time.
- Fail if zero or multiple candidate versions exist.
- Fail if a candidate package contains an undeclared Pocok dependency.
- Fail if a packable project is absent from the package catalog.
- Fail if a retired package still appears in the solution or publication workflows.

---

# 3. Internal reusable code policy

## 3.1 Decision

Yes, the repository should support reusable internal minicode. No, it should not create a general non-packaged runtime `Common` assembly referenced by public packages.

Use this hierarchy:

1. **Package-local internal implementation**, preferred.
2. **Linked internal source**, for tiny helpers with identical semantics in at least four or five independent call sites.
3. **Non-packaged internal projects**, only for tests, source generators/analyzers, build tooling, samples, or non-packaged applications.

A public packable project must never have a runtime `ProjectReference` to a non-packaged Common project. `PrivateAssets` does not embed the dependency or make its runtime types disappear.

## 3.2 Suggested layout

```text
src/
  Shared/
    Reflection/
    Guarding/
    Collections/

tests/
  Shared/
    Architecture/
    Fixtures/

eng/
  Pocok.RepositoryTooling/
```

Files under `src/Shared` are linked explicitly into consuming projects and compile as `internal`.

## 3.3 Admission rules for shared source

A helper may enter shared source only when all are true:

- it is used in at least four or five places across at least two package areas;
- semantics are identical in every use;
- implementation is small and easier to own than a third-party dependency;
- it has no public types;
- it has no third-party dependency;
- it has focused tests;
- it does not swallow exceptions;
- it does not introduce ambient state;
- it is not a general extension collection;
- promotion and removal are cheap.

Examples that may qualify:

- safe `Assembly.GetTypes()` handling that returns loadable types plus structured loader diagnostics;
- deterministic ordinal type ordering;
- path containment validation;
- narrow guard methods not already clear with standard argument guards.

Examples that do not qualify:

- generic `Result` or `Error`;
- `IsNullOrWhiteSpace` wrappers;
- `TryInvoke` methods that catch every exception and return `default`;
- arbitrary string, task, collection, serializer, or reflection extensions;
- provider-specific logging behavior;
- domain DTOs.

## 3.4 Promotion and demotion

Promote internal source to a public package only when:

- it has an independent public capability;
- at least two real consumers use it without sharing application policy;
- its API is stable enough to version;
- a public alternative is not clearly better;
- documentation and samples can explain it without referring to Pocok internals.

Demote a package when:

- it has no independent consumers;
- its public API exists only to share a few internal lines;
- framework or established package support has made it redundant;
- its release burden exceeds its replacement cost.

Primitives should be demoted conceptually, but its generic Result implementation should not become shared source. Conversion and Readiness need distinct failures, not a hidden generic primitive.

## 3.5 Required architecture checks

- No packable project references a non-packable runtime project.
- Shared source contains no public or protected public types.
- Shared source has no package references.
- Every shared source file has at least two consuming projects and a declared owner.
- No project includes all of `src/Shared/**` by wildcard.
- No `Common`, `Utils`, `Foundation`, or `Extensions` package is active.
- Reflection helpers are referenced only by code that truly needs reflection.

---

# 4. `Pocok.Primitives`

## 4.1 Relevance and value

### Personal use

The package saves a small amount of repetitive code, but that convenience is outweighed by coupling every capability to one generic error representation. Applications already have domain-specific failure semantics, exception policies, validation systems, HTTP result types, or language-level `Try` patterns.

### Public use

The public Result/Error space is saturated with mature libraries and many teams intentionally avoid a universal Result abstraction. Pocok's implementation is correct and tested, but it is too small and undifferentiated to justify a dependency.

The right replacement is not another public Result package. It is domain-specific outcomes in the packages that need them.

## 4.2 Architectural concerns

- `Error` combines a code, message, and exception for every domain.
- Generic `Map` and `Bind` operations imply a broader functional abstraction that the repository does not intend to develop.
- The package creates transitive release and restore coupling.
- Consumers must accept Pocok's failure representation to use unrelated capabilities.
- Future changes to error metadata become ecosystem-wide changes.

The code is not sloppy. The package boundary is the problem.

## 4.3 Origin/target mismatch

The extraction plan overvalued a small utility because it was repeated in the origin. Repetition in a monolith does not automatically define a public package. In the target portfolio, Conversion and Readiness have different failure semantics and should not be forced through one legacy-shaped contract.

## 4.4 Misses and alternatives

Adding more generic functional helpers would make the package larger but not more valuable. It would compete with established Result libraries and language patterns rather than improving the actual Pocok capabilities.

## 4.5 Swappability

It is highly swappable by:

- package-specific result/failure types;
- `Try...` patterns;
- exceptions for exceptional conditions;
- application-owned Result libraries where already standardized.

## 4.6 Recommendation

**Retire.**

## 4.7 What to change

- Create `ConversionFailure` and `ConversionResult<T>` in `Pocok.Conversion`.
- Create `ReadinessFailure` and an atomic readiness snapshot in `Pocok.Readiness`.
- Preserve existing semantic tests during migration.
- Remove project references to Primitives.
- Remove Primitives from the active solution, package catalog, smoke consumers, and workflows.
- Add `docs/migrations/primitives.md`.
- If the NuGet package is already public, mark it deprecated on nuget.org with replacements and a migration link. Do not retain an active project solely to keep publishing it.

## 4.8 What to extend

Nothing. Do not add generic validation errors, option types, discriminated unions, or functional combinators.

---

# 5. `Pocok.Hosting`, target `Pocok.Readiness`

## 5.1 Relevance and value

### Personal use

The current implementation solves a recurring problem in integration-heavy services: consumers need to wait for a subsystem to become ready, observe failure or shutdown, and handle repeated start/stop cycles. This is valuable in device interfaces, protocol gateways, line controllers, background workers, and modular hosts.

### Public use

The .NET Generic Host already provides lifecycle callbacks, and ASP.NET Core provides readiness/liveness health checks. Those do not fully replace a focused in-process restartable readiness signal. The package is publicly useful if it remains narrow and integrates cleanly with standard hosting rather than attempting to replace it.

## 5.2 Architectural concerns

- `Pocok.Hosting` is too broad a package name for one readiness capability.
- The public dependency on generic Primitives is unnecessary.
- State and failure should be observed as one atomic snapshot.
- Concurrency semantics need explicit linearization guarantees.
- Exception types and Result failures overlap unless responsibilities are documented.
- The package needs stress and model-based lifecycle tests, not only example transitions.
- Public API snapshotting is currently too weak.

The core design is otherwise sound and materially stronger than the origin.

## 5.3 Origin/target mismatch

The origin's `AwaitableRunningHostedService` couples readiness to inheritance from a hosted service and replaces its `TaskCompletionSource` on stop. Current Pocok has already improved this into composition and restartable cycles. Do not reintroduce inheritance, logging ownership, or hosted-service orchestration into the core package.

## 5.4 Misses

A solid public readiness package should include:

- immutable snapshot of cycle, state, and failure;
- documented transition table;
- cancellation and shutdown distinction;
- deterministic behavior for concurrent transition attempts;
- waiting for a specific cycle;
- diagnostics useful to health-check adapters;
- stress tests;
- sample integration with `BackgroundService` or `IHostedLifecycleService`.

It should not include:

- general host bootstrapping;
- retries;
- health-check UI;
- distributed coordination;
- service discovery;
- process supervision.

## 5.5 Swappability

For simple one-shot startup, use standard hosting primitives directly. For web health endpoints, use standard Health Checks. Keep Pocok only for repeated in-process readiness coordination where a short local `TaskCompletionSource` implementation would otherwise be duplicated and under-specified.

## 5.6 Recommendation

**Stabilize, rename, and keep.**

## 5.7 What to change

- Rename package and namespace to `Pocok.Readiness` before 1.0.
- Replace Primitives with:
  - `ReadinessFailure`;
  - `ReadinessSnapshot`;
  - documented exceptions only where awaiting must throw.
- Expose one atomic snapshot as the primary observation.
- Define every legal transition and the result of duplicate or conflicting transitions.
- Use `RunContinuationsAsynchronously`.
- Audit synchronization for race-free cycle replacement and terminal completion.
- Add supported API compatibility baseline.
- Add migration documentation from `Pocok.Hosting`.
- Mark `Pocok.Hosting` deprecated if already released.

## 5.8 What to extend

- A synthetic sample with a restartable communicator.
- Optional adapter code only after the core is stable:
  - health-check projection;
  - hosted lifecycle logging.
- Do not create an adapter package until its dependency cone and at least two consumers justify it.

---

# 6. `Pocok.Conversion.Abstractions`

## 6.1 Relevance and value

The policies, culture, context, error codes, and `IValueConverter` are useful. Their separation into a distinct NuGet package is not.

A contract package is justified when multiple implementations, plugins, or independently deployed components must reference a stable boundary without taking the implementation. That ecosystem does not exist here.

## 6.2 Architectural concerns

- It depends on Primitives, so the “abstractions” package is not actually minimal.
- It creates coordinated versioning between two packages.
- It has a separate release workflow, smoke consumer, API baseline, README, and dependency audit for roughly 225 lines of contract code.
- Conversion and Abstractions share the same MinVer tag prefix while having separate publication triggers.
- A mockable `IValueConverter` does not require a separate package.

## 6.3 Origin/target mismatch

The split follows an enterprise extraction instinct where interfaces are placed in separate assemblies by default. That is not automatically SOLID. Dependency inversion is about dependency direction and stable policy, not maximizing assembly count.

## 6.4 Misses and alternatives

The package would need a genuine alternate implementation ecosystem to justify itself. Fabricating one would be architecture theater.

## 6.5 Swappability

Merge the public types into `Pocok.Conversion`. Applications can still depend on `IValueConverter`, use a test double, or wrap the implementation.

## 6.6 Recommendation

**Merge and retire.**

## 6.7 What to change

- Move public policies, context, errors, and interface into `Pocok.Conversion`.
- Replace the generic Result dependency with conversion-specific results.
- Keep namespaces coherent under `Pocok.Conversion`.
- Remove project, workflow, smoke fixture, API baseline, and package catalog entry.
- Add migration documentation and package deprecation if already released.
- Use only `conversion-v*` after the merge.

## 6.8 What to extend

Nothing as an independent package. Extend the merged Conversion capability instead.

---

# 7. `Pocok.Conversion`

## 7.1 Relevance and value

### Personal use

This is a strong reusable capability for applications that receive values from configuration, UI editors, scripts, protocols, devices, databases, dynamic documents, or plugin boundaries. Explicit culture, overflow, enum, temporal, null, and numeric-loss policy prevents each application from developing a slightly different conversion swamp.

### Public use

The package is useful if positioned as strict, policy-driven, serializer-free runtime conversion. It should not claim to replace parsing, mapping, serialization, validation, or object projection libraries.

Its value over `Convert.ChangeType` and `TypeConverter` is explicit policy, structured failure, collection support, and deterministic behavior. Its value over serializers is refusing to use a serializer as an implicit conversion engine.

## 7.2 Architectural concerns

- Public contract is split without need.
- Generic Result leaks an unrelated abstraction.
- One large converter class risks accumulating branches.
- Recursive conversion needs explicit depth and collection-size limits.
- Failure location should identify nested list index, tuple item, or dictionary key/value.
- Extension behavior is not yet clearly bounded.
- Identity conversion and assignable reference behavior must be explicit.
- Reflection and generic construction need trimming/AOT documentation.
- Static API compatibility tests are incomplete.

## 7.3 Origin/target mismatch

The origin contains a broader converter ecosystem and serializer fallback behavior. Treat it as a behavior inventory, not the target design. Do not port global converter managers, ambient configuration, serializer fallback, or one-interface-per-conversion clutter.

The current Pocok implementation is correctly narrower. Preserve that identity.

## 7.4 Misses

Prioritize:

- path-aware failures;
- maximum recursion depth;
- maximum collection item count;
- deterministic duplicate-key policy;
- generic math for numeric conversions where it reduces reflection and handwritten matrices;
- explicit identity/assignability policy;
- nullable and optional target semantics;
- immutable configured converter instances;
- a narrow custom strategy registration mechanism;
- detailed tests for cultures, boundaries, nested structures, enums, temporal values, and user-defined strategies;
- benchmarks against relevant BCL baselines;
- trimming analysis and documented limitations.

Do not add:

- automatic object-to-object property mapping;
- arbitrary JSON deserialization fallback;
- implicit service-provider access;
- global mutable converter registry;
- expression evaluation;
- validation;
- business-unit conversion;
- mapping profiles;
- hidden culture fallback.

## 7.5 Swappability

Use BCL parsing directly for local typed parsing. Use `TypeConverter` for component-model conversions. Use serializers for serialization contracts. Use mapping libraries for object mapping.

Keep Pocok.Conversion where a common, runtime-typed, explicit policy engine is repeatedly needed. There is no striking standard package that makes the current focused capability pointless.

## 7.6 Recommendation

**Merge, stabilize, and extend. This is the primary public capability.**

## 7.7 What to change

- Merge Abstractions.
- Replace `Result<T>` with a focused `ConversionResult<T>` and `ConversionFailure`.
- Split internal behavior into cohesive strategies or handlers without creating one public type per branch.
- Make recursion context internal and carry:
  - current path;
  - depth;
  - item budget;
  - culture and policies;
  - cancellation only if conversion can perform meaningful cancellable work. Otherwise omit it.
- Add immutable builder or constructor configuration.
- Define strategy ordering and duplicate strategy behavior.
- Keep built-ins deterministic and stateless.
- Add API/package compatibility baselines and migration docs.
- Keep package dependencies minimal.

## 7.8 Suggested public extension shape

The exact names may be refined during implementation, but the design must preserve these properties:

```csharp
public interface IValueConverter
{
    ConversionResult<object?> Convert(
        object? value,
        Type targetType,
        ConversionContext? context = null);
}

public sealed class ValueConverter : IValueConverter
{
    public static ValueConverter Default { get; }

    public ValueConverter(IEnumerable<IConversionStrategy> additionalStrategies);
}
```

Custom strategies:

- are explicitly supplied;
- are ordered deterministically;
- do not access a service locator;
- receive a safe continuation for nested conversion rather than the converter's mutable internals;
- cannot silently replace built-ins unless the caller explicitly chooses precedence;
- report structured not-applicable versus failed outcomes.

Avoid exposing the entire internal recursive session as a mutable public object.

## 7.9 What to extend

- Nested dictionaries, pairs, tuples, and collections only where semantics are explicit.
- Modern numeric coverage using generic math.
- `DateOnly`, `TimeOnly`, `DateTimeOffset`, `TimeSpan`, `Guid`, enum, nullable, and common collection matrices.
- Benchmark project with representative scalar and nested conversions.
- Property-based tests for numeric boundaries and round-trip-safe cases.
- Fuzz tests for text inputs with bounded resource usage.
- Sample demonstrating configuration parsing and plugin option conversion.

---

# 8. Public AppDefaults configurator family

## 8.1 Identity

These packages are public, opinionated maintainer defaults. They are not private packages and do not require a private feed.

Their purpose is to configure infrastructure the application does not own:

- standard dependency injection;
- standard configuration;
- standard logging abstractions;
- Serilog where explicitly selected;
- standard options validation;
- Pocok.Modularity where explicitly selected.

They must remain transparent, composable, and replaceable.

## 8.2 Common configurator contract

Start with a deliberately small contract:

```csharp
public interface IApplicationConfigurator
{
    void Configure(IHostApplicationBuilder builder);
}
```

Provide explicit composition:

```csharp
builder.ConfigureWith(
    new StandardLoggingConfigurator(options),
    new ModularityConfigurator(options));
```

Rules:

- caller order is execution order;
- no assembly scanning for configurators;
- no implicit dependency graph;
- no hidden service provider;
- no build of an intermediate provider;
- no static mutable application identity;
- no configuration after the host is built;
- repeat application is either idempotent or fails with a documented duplicate-configuration error;
- applications can override defaults after calling the configurator;
- options are validated on start;
- secrets are never logged.

Do not add `Order`, `DependsOn`, auto-discovery, or a configurator container until real applications prove explicit ordering insufficient.

## 8.3 `Pocok.AppDefaults`

### Value

This package creates a common shape for configuring cross-cutting concerns across many applications. Its public novelty is intentionally low. Its personal maintenance value is high.

### Scope

- `IApplicationConfigurator`;
- explicit composition extension;
- duplicate-application marker helpers where truly shared;
- standard option validation helpers only if they are not trivial wrappers;
- package classification and documentation.

### Non-scope

- custom DI container;
- custom configuration provider;
- service locator;
- module loader;
- logging provider;
- resilience framework;
- application framework.

### Recommendation

**Implement and keep tiny.**

## 8.4 `Pocok.AppDefaults.Logging`

### Value

Provide a provider-neutral baseline for Microsoft.Extensions.Logging:

- bind a clearly named configuration section;
- environment-aware default levels;
- activity tracking options;
- optional built-in console formatting;
- filters for noisy framework categories;
- explicit replace-versus-append provider behavior;
- startup validation;
- an application identity supplied by options, not static state.

Do not clear providers by default. Do not assume console, file, or telemetry is always wanted.

### Recommendation

**Implement as a small standard baseline.**

## 8.5 `Pocok.AppDefaults.Logging.Serilog`

The origin's large `LogConfigurer` validates repeated needs:

- consistent application naming;
- enrichers;
- console formatting;
- rolling file output;
- compact structured output;
- OpenTelemetry export;
- optional custom sinks;
- self-log handling;
- retention settings.

It also demonstrates what not to preserve:

- static mutable `AppName`;
- one package pulling console, file, MongoDB, OpenTelemetry, XML formatting, and custom sinks together;
- direct database index management inside logging configuration;
- service-provider-driven configuration;
- silently owned global Serilog self-log lifetime;
- proprietary templates and naming.

### Version 1 scope

- configure Serilog from standard configuration;
- enrich from log context, environment, process, and activity;
- console sink;
- optional compact JSON rolling file sink;
- optional OpenTelemetry sink only if its dependency and behavior remain modest;
- explicit application name;
- documented bootstrap logger behavior;
- documented self-log ownership;
- no MongoDB sink;
- no XML formatter;
- no direct storage administration;
- no custom in-memory sink in the production package.

If OpenTelemetry logging can be configured cleanly through standard Microsoft/OpenTelemetry packages, prefer that over Serilog-specific OTLP coupling.

### Recommendation

**Implement as a provider-specific public defaults package.**

## 8.6 Testing requirements

- minimal host builder applies defaults;
- app settings bind correctly;
- app overrides win after defaults;
- duplicate application behavior is deterministic;
- no duplicate providers on repeated application;
- invalid options fail at startup;
- default configuration does not create files or network connections;
- opt-in file logging uses temporary directories;
- log output contains expected structured properties;
- no secret configuration values are emitted;
- clean local-feed consumer tests for each package;
- provider-neutral package does not transitively depend on Serilog.

## 8.7 What to extend later

Only after repeated use:

- `Pocok.AppDefaults.Observability`, preferably composing OpenTelemetry and standard health checks;
- `Pocok.AppDefaults.Http`, preferably composing standard resilience handlers;
- an aggregate `Pocok.AppDefaults.Standard` after at least three applications use the exact same package set.

Do not create an aggregate package first.

---

# 9. Public Modularity family

## 9.1 Problem definition and terminology

Use consistent terminology:

- **Plugin:** an independently built and deployed directory containing one entry assembly and its private dependencies.
- **Module:** the plugin entry point that registers services into the host.
- **Contract:** a stable assembly shared by the host and plugin.
- **Modularity:** discovery, validation, loading, registration, and diagnostics.
- **Configurator:** opinionated host policy for setting up Modularity.

The user's target use case is valid: a cross-platform host should discover optional codec, communicator, device, or supplier implementations without statically referencing Windows-only or supplier-specific projects. Application services consume standard DI collections such as `IEnumerable<IDeviceCommunicator>`.

## 9.2 Origin evidence

The origin directly demonstrates this problem:

- `ImplementationLoader` scans selected assemblies and creates implementations with `Activator.CreateInstance`.
- seeding registration searches already loaded assemblies and contains a TODO to load assemblies from an external folder;
- registration builds an intermediate `ServiceProvider`, resolves `ISeedable`, and then mutates the original service collection;
- platform-specific implementations rely on compile-time and runtime checks;
- assembly naming and loaded-AppDomain state drive discovery.

These are useful requirements but poor target patterns.

Do not copy:

- `Assembly.GetCallingAssembly()` discovery;
- scanning all loaded assemblies;
- naming-convention interface matching;
- `Activator.CreateInstance` as dependency construction;
- intermediate `BuildServiceProvider`;
- hardcoded references whose only purpose is to force assembly loading;
- catch-all reflection behavior;
- platform checks after incompatible assemblies have already been loaded.

## 9.3 Public packages

### `Pocok.Modularity.Contracts`

Contains only the stable module entry contract and metadata required by both host and plugin:

```csharp
public interface IServiceModule
{
    void ConfigureServices(
        IServiceCollection services,
        ModuleContext context);
}
```

`ModuleContext` should expose immutable, host-supplied information such as:

- plugin ID;
- plugin root path;
- configuration section;
- host environment;
- diagnostic recorder or logger abstraction only if needed during registration.

Use standard Microsoft abstractions. Do not invent a Pocok DI abstraction.

### `Pocok.Modularity`

Owns:

- manifest discovery;
- path validation;
- platform and architecture filtering before managed assembly loading;
- dependency isolation;
- shared contract type identity;
- module type loading;
- deterministic service registration;
- immutable module catalog;
- structured diagnostics;
- required versus optional module failure policy;
- startup-only lifecycle.

### `Pocok.AppDefaults.Modularity`

A thin configurator that applies personal defaults:

- default plugin directory;
- configuration section name;
- logging integration;
- required/optional defaults;
- diagnostics behavior;
- standard manifest naming;
- no separate runtime behavior.

## 9.4 Implementation strategy

Use established assembly-loading infrastructure rather than inventing it casually.

Preferred process:

1. Spike `McMaster.NETCore.Plugins` 2.x against net10.0 with:
   - one plugin;
   - conflicting private dependency versions;
   - shared contract type;
   - Windows-only plugin skipped on non-Windows.
2. If the spike passes and the dependency is acceptable, wrap it behind an internal loader adapter.
3. If it fails a documented requirement, implement the minimum internal adapter with `AssemblyLoadContext` and `AssemblyDependencyResolver` following Microsoft's plugin guidance.
4. Keep the public Pocok API independent of the chosen loader implementation.

Scrutor is useful for scanning assemblies already available to the application. It does not solve external plugin deployment, dependency isolation, or shared contract identity. Do not use it as the module loader.

## 9.5 Version 1 boundaries

- trusted plugins only;
- startup-time discovery and registration only;
- one directory per plugin;
- manifest read before assembly load;
- no runtime install;
- no hot reload;
- no unload promise;
- no plugin marketplace;
- no remote package download;
- no child service containers;
- no service locator;
- no arbitrary dependency injection into the module constructor;
- no attempt to sandbox untrusted code;
- no NativeAOT support claim;
- trimming limitations documented.

Untrusted code must run out of process or in an OS/virtualization security boundary.

## 9.6 Manifest

Use a small JSON manifest, for example:

```json
{
  "id": "example.supplier-codec",
  "entryAssembly": "Example.SupplierCodec.Plugin.dll",
  "moduleType": "Example.SupplierCodec.Plugin.SupplierCodecModule",
  "supportedRuntimes": ["win-x64"],
  "contracts": {
    "Example.Communicators.Abstractions": "1.2.0"
  }
}
```

Policy:

- ID unique with ordinal comparison;
- paths must remain inside plugin root;
- entry assembly and module type explicit;
- runtime filter evaluated before loading;
- manifest versioned;
- unknown required fields fail;
- unknown optional fields follow a documented forward-compatibility rule;
- host configuration decides whether a plugin is required;
- contract compatibility is explicit and diagnostic;
- duplicate service semantics remain standard DI semantics unless the application's own contract defines otherwise.

## 9.7 Registration and consumption

Host:

```csharp
builder.Services.AddPocokModules(options =>
{
    options.AddDirectory("plugins");
    options.ShareContractAssembly(typeof(IDeviceCommunicator).Assembly);
});
```

Application:

```csharp
public sealed class DeviceCoordinator(
    IEnumerable<IDeviceCommunicator> communicators,
    IModuleCatalog modules)
{
}
```

Module:

```csharp
public sealed class SupplierModule : IServiceModule
{
    public void ConfigureServices(
        IServiceCollection services,
        ModuleContext context)
    {
        services.AddSingleton<IDeviceCommunicator, SupplierCommunicator>();
    }
}
```

The module registers standard services. It does not return a custom module container.

## 9.8 Diagnostics

Expose an immutable catalog containing:

- discovered plugin ID and path;
- manifest status;
- platform compatibility;
- load status;
- module type;
- registration status;
- structured failures with stage and exception;
- versions of plugin, entry assembly, contracts, and loader;
- required/optional disposition.

Never silently skip a malformed plugin. Optional means startup continues with visible diagnostics, not that failure disappears.

## 9.9 Tests

Build real plugin fixture outputs, not only mocks:

- valid plugin;
- two plugins implementing one shared contract;
- plugin with private dependency version conflicting with host;
- plugin with missing dependency;
- plugin with wrong contract assembly version;
- duplicate plugin ID;
- malformed manifest;
- path traversal attempt;
- wrong operating system/architecture;
- module type missing;
- module type not implementing contract;
- module throws during registration;
- optional failure continues;
- required failure stops;
- deterministic ordering;
- standard `IEnumerable<T>` resolution;
- clean external consumer sample that copies plugin output into a plugin directory.

## 9.10 Recommendation

**Implement and publish.**

This capability is more valuable than a generic reflection library because it owns a complete, testable runtime behavior and solves a repeated deployment boundary.

---

# 10. Origin extraction map

| Origin area | What it proves | What may be reused | What must not be copied | Target |
|---|---|---|---|---|
| Conversion implementation and tests | Many runtime value shapes need consistent semantics | Behavior matrix, edge-case ideas, independently rewritten tests | serializer fallback, global managers, application contracts | `Pocok.Conversion` |
| `AwaitableRunningHostedService` | Services need readiness awaiting | Lifecycle scenarios and migration sample | inheritance-based service base class, replaceable TCS pattern, logging ownership | `Pocok.Readiness` |
| `LogConfigurer` | Multiple applications need consistent logging policy | Requirements such as identity, enrichment, console, rolling files, telemetry, validation | static state, Mongo administration, proprietary formats, all-sinks package | `Pocok.AppDefaults.Logging.*` |
| `ImplementationLoader` | Implementations are discovered repeatedly | Need for type discovery and structured diagnostics | calling-assembly behavior, naming heuristics, direct Activator construction | Modularity internals |
| `SeedingServiceRegistrar` | Optional assemblies and service registration need orchestration | External plugin folder requirement and ordered registration scenarios | loaded-AppDomain scanning, hardcoded assembly forcing, intermediate provider | `Pocok.Modularity` |
| Platform-specific registrars | Cross-platform host must avoid incompatible implementations | Pre-load runtime filtering requirements | compile-time references from host to every platform implementation | Modularity manifest/runtime filter |
| Localization compositor and tests | Deterministic multi-source localization may have value | Test ideas for duplicates, fallback, precedence, missing resources | database/domain resource integration and project resources | `Pocok.Localization` experimental alpha |
| Keyed subscription registry | Multiple consumers need keyed typed listeners with filtering | Thread-safe listener ownership, typed mapping, and disposal scenarios | transport lifecycle, retry timers, logging, and network adapters | `Pocok.Subscriptions` experimental alpha |
| JSON DTO schema generator | Contract artifacts can aid integrations | Concrete consumer requirement and documentation linkage idea | app-specific DTO paths and generic schema package | use BCL `JsonSchemaExporter`; defer differentiated manifest |
| `Reflections` and Common.Utils | Small helpers accumulate in a monolith | A few narrow internal behaviors after repeated use | catch-all extensions, swallowed failures, generic invocation helpers | linked internal source only |
| Seeding, scripting, signals, activation, UI | Large independent domains exist | Future problem inventory | importing application frameworks into Pocok | separate future repositories or reject |

## Extraction principle

Port tests and requirements before implementation. Write new Pocok tests in neutral terminology. Then implement the smallest public capability that satisfies them. The legacy source is not a template.

---

# 11. Planned and rejected package areas

## 11.1 `Pocok.Numerics`

**Decision: never implement as a general package.**

Use modern generic math such as `INumberBase<T>` and checked/saturating/truncating creation. Keep conversion-specific numeric policy in `Pocok.Conversion`. A separate package is justified only for a genuinely distinct mathematical domain.

## 11.2 Generic contracts and JSON schema

**Decision: do not implement now.**

Modern `System.Text.Json.Schema.JsonSchemaExporter` maps .NET serialization metadata to JSON schema. A Pocok wrapper that only forwards to it would be noise.

Reconsider only for a concrete package that adds all of:

- explicit type allowlist;
- deterministic manifest ordering;
- stable fingerprints;
- compatibility comparison;
- documentation source integration;
- artifact generation for CI;
- no application-specific DTOs.

Until then, use the BCL directly in application tooling.

## 11.3 Generic reflection package

**Decision: never implement.**

Reflection is an implementation mechanism, not the public capability. Put narrowly tested reflection code inside Modularity or linked internal source.

## 11.4 Localization

**Decision: extract the neutral compositor as experimental alpha; defer providers.**

The requested extraction now carries only deterministic composition of standard `IStringLocalizer` providers. Database, filesystem, resource-assembly discovery, caching, and application-specific registration remain deferred until an independent requirement justifies them.

## 11.5 Keyed subscriptions

**Decision: extract the neutral listener registry as experimental alpha; defer retry orchestration.**

The requested extraction carries the reusable keyed subscription behavior from the origin: multiple listeners per key, typed mapping, filtering, synchronous delivery, snapshots before handler invocation, and idempotent disposal. Timer-based retry and network lifecycle remain deferred because they need explicit cancellation, time, ownership, and failure-isolation contracts rather than a direct copy of the legacy implementation.

## 11.5 General logging package

**Decision: never implement.**

Do not build a logging framework. Implement AppDefaults packages that configure Microsoft logging and explicit providers.

## 11.6 General seeding package

**Decision: do not extract from origin.**

The origin's seeding orchestration is domain and persistence specific. Modularity can load seeding modules, but Pocok should not own database seeding semantics without a separate proven use case.

---

# 12. Target repository structure

```text
/
  eng/
    packages.json
    schemas/
      packages.schema.json
    scripts/
      Get-PackageGraph.ps1
      Invoke-PackageRelease.ps1
      Test-PackageCatalog.ps1

  src/
    Conversion/
      Pocok.Conversion.csproj
      README.md

    Readiness/
      Pocok.Readiness.csproj
      README.md

    AppDefaults/
      Pocok.AppDefaults.csproj
      README.md

    AppDefaults.Logging/
      Pocok.AppDefaults.Logging.csproj
      README.md

    AppDefaults.Logging.Serilog/
      Pocok.AppDefaults.Logging.Serilog.csproj
      README.md

    Modularity.Contracts/
      Pocok.Modularity.Contracts.csproj
      README.md

    Modularity/
      Pocok.Modularity.csproj
      README.md

    AppDefaults.Modularity/
      Pocok.AppDefaults.Modularity.csproj
      README.md

    Shared/
      Reflection/
      Guarding/

  tests/
    Conversion.Tests/
    Readiness.Tests/
    AppDefaults.Tests/
    AppDefaults.Logging.Tests/
    AppDefaults.Logging.Serilog.Tests/
    Modularity.Contracts.Tests/
    Modularity.Tests/
    AppDefaults.Modularity.Tests/
    Architecture.Tests/
    Shared/

  samples/
    Conversion.Sample/
    Readiness.Sample/
    AppDefaults.Sample/
    ModularCommunicator.Host/
    ModularCommunicator.Contracts/
    ModularCommunicator.WindowsPlugin/
    ModularCommunicator.PortablePlugin/

  benchmarks/
    Pocok.Conversion.Benchmarks/

  tools/
    PackageSmoke/
    PublicReleaseAudit/

  docs/
    architecture/
    decisions/
    migrations/
    package-catalog.md
```

Do not create empty projects merely to match this layout. Each project must satisfy its section's admission criteria and have real implementation, tests, package smoke coverage, and documentation.

---

# 13. Workflow for the next 1-step improvement session

## 13.1 Mission

Stabilize the consolidated repository in one session. Verify behavior, repair any remaining release engineering gaps, and ensure the repository is buildable, testable, and packable. Handle this as a local agentic job: edit relevant files and run tests locally without relying on external ZIP handoffs.

## 13.2 Inputs

- Current state: locally available repository.
- Legacy evidence: `origin.zip` (reference only).
- This document is the architectural authority when existing plans conflict.

## 13.3 Non-negotiable constraints

- Do not copy proprietary origin code or terminology.
- Do not publish packages or create remote releases.
- Do not preserve an abstraction solely because it exists.
- Do not introduce a custom DI container, configuration system, logging framework, service locator, serializer fallback, or plugin sandbox claim.
- Do not add package projects without completing code, tests, README, sample or smoke consumer, catalog metadata, API baseline, and release support.
- Do not use broad catch-and-return-default reflection helpers.
- Do not build an intermediate `ServiceProvider` during registration.
- Do not make global mutable registries.
- Do not use wildcard release artifacts.
- Do not reduce strict compiler, deterministic build, lock-file, or source-link standards.
- Keep public API smaller than the implementation.
- Prefer framework or respected third-party behavior when it already solves the non-differentiating part.
- Preserve behavior through tests before removing old projects.
- A failing check must be fixed or explicitly documented with evidence. Do not comment it out.

## 13.4 Default decisions

Use these without stopping for clarification:

1. Rename Hosting to Readiness before stable release.
2. Retire Primitives rather than expand it.
3. Merge Conversion.Abstractions into Conversion.
4. Keep net10.0 only.
5. Publish all active packages to nuget.org.
6. Keep one monorepo.
7. Use explicit configurator order.
8. Implement Serilog as an optional provider-specific defaults package.
9. Implement trusted startup-only plugins.
10. Spike McMaster.NETCore.Plugins first and fall back to a minimal BCL adapter only with documented reasons.
11. Defer Localization and generic Contracts.
12. Do not implement Numerics or Reflection packages.

## 13.5 Required agent output

At completion provide:

- concise change summary;
- ordered commit list;
- package graph before and after;
- tests, builds, packs, smoke modes, audits, samples, and benchmarks executed;
- any behavior/API migrations;
- any third-party package added and why;
- remaining non-blocking limitations;
- exact commands to release each active package later.

---

# 14. Ordered implementation and commit plan (Stabilization Session)

> **Current status:** The structural changes described in the phases below have been applied to the current local baseline. The next agentic session is a 1-step improvement job to stabilize the repository, verify tests, and finalize the consolidation locally.

## Phase 0: verify executable baseline

### Commit 1: `test(baseline): lock current package behavior and repository inventory`

**Intent:** preserve useful behavior before structural changes.

**Changes:**

- run full restore, build, test, and pack;
- record current public package graph and public APIs;
- add missing characterization tests discovered from source;
- add a generated or checked package inventory report;
- ensure origin is outside project inclusion and never copied.

**Acceptance:**

```shell
dotnet restore --locked-mode
dotnet build Pocok.slnx -c Release --no-restore
dotnet test Pocok.slnx -c Release --no-build
dotnet pack Pocok.slnx -c Release --no-build -o artifacts/baseline
```

## Phase 1: repair packaging and release rehearsal

### Commit 2: `fix(release): stage transitive package closures for smoke tests`

**Intent:** fix the reported NU1101 failure correctly.

**Changes:**

- add clean `closure-feed` and `candidate-feed`;
- compute internal transitive package closure;
- pack closure into local feed;
- copy exact candidate into candidate feed;
- add local closure smoke;
- add publication smoke with candidate feed plus nuget.org;
- isolate package caches;
- deterministic exact package selection;
- retain current package workflows temporarily.

**Tests:**

- Primitives succeeds in both modes;
- Hosting closure smoke succeeds;
- Hosting publication smoke succeeds only when required Primitives version is available publicly;
- Conversion.Abstractions behaves similarly;
- Conversion requires published Abstractions in publication mode;
- intentionally missing dependency produces a focused failure.

### Commit 3: `refactor(release): centralize package catalog and reusable workflow`

**Changes:**

- add validated `eng/packages.json`;
- add reusable publish workflow;
- reduce package workflows to thin triggers;
- derive project, tag, consumer, class, tier, and allowed dependencies from catalog;
- remove hardcoded exact dependency versions from audit;
- push and attach exact package files only;
- add catalog/documentation consistency tests;
- fix temporary Conversion.Abstractions tag prefix mismatch.

**Acceptance:**

- dry-run every package trigger locally where possible;
- workflow lint passes;
- every active packable project has one catalog entry and one thin trigger;
- no release script contains a manually copied internal dependency version.

### Commit 4: `docs(release): align package state and packaged links`

**Changes:**

- update README and PUBLICATION to current state;
- use capability versus maintainer-default classification;
- fix repository-relative links in packaged READMEs;
- explain two smoke modes and release order;
- document package deprecation policy.

## Phase 2: remove generic Primitives coupling

### Commit 5: `refactor(conversion): introduce conversion-specific outcomes`

**Changes:**

- add `ConversionFailure`, path representation, and `ConversionResult<T>`;
- migrate Conversion.Abstractions and Conversion behavior;
- preserve all current failure codes and semantics unless an ADR justifies improvement;
- add result-specific tests;
- avoid generic Map/Bind API.

### Commit 6: `refactor(readiness): introduce readiness-specific failures and snapshots`

**Changes:**

- add `ReadinessFailure`;
- add atomic `ReadinessSnapshot`;
- migrate current behavior away from Primitives;
- add concurrency characterization tests.

### Commit 7: `chore(primitives): retire Pocok.Primitives`

**Changes:**

- remove all references;
- remove active project, tests, smoke consumer, workflow, and API baseline;
- mark retired in package catalog;
- add migration guide;
- document nuget.org deprecation step if package exists publicly.

**Acceptance:**

- no active package dependency on Primitives;
- no production reference to its namespace;
- full build/test/pack passes.

## Phase 3: consolidate and strengthen Conversion

### Commit 8: `refactor(conversion): merge abstractions into Pocok.Conversion`

**Changes:**

- move policies, context, error codes, and interface;
- remove Abstractions project and workflow;
- retain one coherent namespace;
- update package consumers and docs;
- add migration guide;
- only `conversion-v*` remains.

### Commit 9: `refactor(conversion): separate bounded conversion strategies`

**Changes:**

- split monolithic internals by cohesive conversion category;
- add recursion depth and item budget;
- add path-aware nested failures;
- define identity/assignability semantics;
- define duplicate dictionary key behavior;
- preserve serializer-free policy;
- no service provider or global registry.

### Commit 10: `feat(conversion): add explicit custom strategy composition`

**Changes:**

- add minimal deterministic extension API;
- explicit precedence;
- distinguish not-applicable from failed;
- safe continuation for nested conversion;
- immutable configured converter;
- tests for conflicts, ordering, recursion, and custom failures.

### Commit 11: `test(conversion): add property, fuzz, trim, and benchmark coverage`

**Changes:**

- numeric boundary/property tests;
- culture matrix;
- bounded malformed-input fuzz tests;
- benchmark project;
- trimming analysis test or sample;
- package compatibility baseline.

**Acceptance:**

- no unbounded recursion or collection allocation;
- baseline behavior remains documented;
- benchmark results are informational, not arbitrary pass/fail thresholds.

## Phase 4: stabilize Readiness

### Commit 12: `refactor(readiness): rename and harden lifecycle coordination`

**Changes:**

- rename project/package/namespace from Hosting to Readiness;
- formal transition table;
- atomic snapshot;
- deterministic concurrent transitions;
- cycle-specific waiting;
- `RunContinuationsAsynchronously`;
- migration guide and deprecated old package instructions.

### Commit 13: `test(readiness): add lifecycle model and concurrency stress tests`

**Changes:**

- model-based transition tests;
- repeated restart cycles;
- concurrent start/fail/stop/cancel attempts;
- waiter cancellation;
- no leaked waiters;
- sample hosted service integration;
- API compatibility baseline.

## Phase 5: formalize internal shared source

### Commit 14: `chore(shared): enforce internal minicode policy`

**Changes:**

- add shared-source folders only for helpers already duplicated after refactoring;
- add architecture checks;
- remove generic or trivial extensions;
- add safe reflection type enumeration only if Modularity needs it;
- document promotion/demotion rules.

**Acceptance:**

- no non-packaged runtime project reference from active package;
- no public shared type;
- no wildcard inclusion.

## Phase 6: implement AppDefaults and logging

### Commit 15: `feat(appdefaults): add explicit application configurator composition`

**Changes:**

- implement `IApplicationConfigurator`;
- implement explicit ordered composition;
- deterministic duplicate behavior;
- sample host;
- package README and ADR;
- package catalog and smoke consumer.

### Commit 16: `feat(logging): add provider-neutral logging defaults`

**Changes:**

- configuration-bound options;
- environment-aware filters;
- activity tracking;
- optional standard console provider;
- append/replace behavior;
- startup validation;
- application override tests;
- no Serilog dependency.

### Commit 17: `feat(logging-serilog): add focused Serilog defaults`

**Changes:**

- selected enrichers;
- console;
- opt-in compact rolling file;
- optional telemetry only if dependency assessment remains favorable;
- application identity through options;
- bootstrap and self-log lifetime documented;
- no MongoDB, XML, direct index management, or proprietary templates;
- test sinks and temporary filesystem integration tests.

## Phase 7: implement Modularity

### Commit 18: `feat(modularity-contracts): add stable service module contract`

**Changes:**

- `IServiceModule`;
- immutable `ModuleContext`;
- manifest model/version;
- minimal standard DI dependency;
- compatibility policy;
- plugin author sample.

### Commit 19: `spike(modularity): validate loader isolation strategy`

**Changes:**

- executable spike tests for McMaster loader;
- conflicting private dependency versions;
- shared contract identity;
- platform filtering;
- ADR selecting McMaster or BCL fallback;
- no public spike API.

This commit may remain as tests/ADR plus the selected internal adapter. Do not keep two production loaders.

### Commit 20: `feat(modularity): add manifest discovery and startup registration`

**Changes:**

- path-contained directory discovery;
- manifest validation before assembly load;
- platform/architecture filter;
- loader adapter;
- module type validation;
- service registration;
- immutable catalog and diagnostics;
- required/optional failure policy;
- deterministic ordering.

### Commit 21: `test(modularity): add real plugin fixtures and clean consumer`

**Changes:**

- portable and platform-specific fixture plugins;
- malformed and incompatible fixtures;
- missing dependencies;
- duplicate IDs;
- path traversal;
- registration failure;
- standard `IEnumerable<T>` consumer;
- local-feed host sample.

### Commit 22: `feat(appdefaults-modularity): add opinionated modular host defaults`

**Changes:**

- implement configurator;
- default directory and configuration section;
- explicit overrides;
- diagnostics/logging integration;
- no new loading behavior;
- sample composing logging and modularity.

## Phase 8: finish the showcase and release system

### Commit 23: `refactor(api): adopt supported compatibility baselines`

**Changes:**

- use package validation/API compatibility tooling as authority;
- retain custom snapshots only as readable supplementary documents;
- establish baseline versions for stable packages;
- test breaking-change workflow.

### Commit 24: `docs(showcase): complete architecture, samples, and tradeoffs`

**Changes:**

- package dependency diagram;
- capability versus defaults explanation;
- why Primitives and Abstractions were retired;
- why Numerics, Reflection, and generic Contracts were rejected;
- origin extraction methodology;
- samples;
- trust and security boundaries;
- support and compatibility policy;
- contribution map.

### Commit 25: `chore(release): verify all active packages clean-room`

**Changes:**

- update catalog and workflows for final active set;
- run both smoke modes for every package;
- inspect every nupkg and snupkg;
- vulnerability and license review;
- Source Link verification;
- package README rendering check;
- final release dry-run report.

---

# 15. Acceptance matrix

## 15.1 Repository

- locked restore passes;
- Release build passes with zero warnings;
- all tests pass;
- pack produces only cataloged active packages;
- no proprietary origin text or identifiers appear;
- package graph matches documented graph;
- every package has owner, tier, class, README, sample, smoke consumer, and API baseline;
- no stale workflow or retired project remains active.

## 15.2 Release

- full local closure smoke passes;
- publication smoke passes using candidate plus nuget.org;
- exact package and symbols files selected;
- no wildcard publishing;
- all dependencies are catalog-allowlisted;
- no manually hardcoded dependency versions in audit;
- tag prefix and package version agree;
- release dry run emits an auditable report.

## 15.3 Conversion

- existing behavior preserved or migration explicitly documented;
- no Primitives or Abstractions dependency;
- bounded depth and item count;
- path-aware errors;
- deterministic custom strategy behavior;
- serializer-free;
- property/fuzz tests and benchmarks present;
- no global mutable registry.

## 15.4 Readiness

- renamed package;
- no Primitives dependency;
- atomic lifecycle snapshot;
- documented transition table;
- stress tests pass;
- repeated cycles work;
- cancellation, failure, and stopping are distinct;
- no hosted-service inheritance requirement.

## 15.5 AppDefaults

- explicit ordered configuration;
- transparent options;
- application overrides work;
- duplicate behavior deterministic;
- no hidden scanning;
- provider-neutral logging has no Serilog dependency;
- defaults cause no file/network side effect;
- provider-specific behavior is in provider-specific package.

## 15.6 Modularity

- one directory per plugin;
- manifest checked before load;
- host does not reference optional implementation projects;
- private plugin dependencies isolate correctly;
- shared contract type identity works;
- wrong-platform plugin skipped before load with diagnostics;
- no intermediate provider;
- no hot reload or sandbox claim;
- real fixture plugins prove registration and consumption.

---

# 16. Questions worth revisiting after implementation

These are not blockers. The implementation defaults above should be used now.

1. **Should Readiness gain a separate hosting/health-check adapter package?**
   Recommended answer: only after two applications need the same adapter. Keep the first release in the core sample.

2. **Should Serilog OpenTelemetry export be included in version 1?**
   Recommended answer: prefer standard OpenTelemetry logging integration. Include Serilog OTLP only if it materially reduces configuration without coupling the base package.

3. **Should Pocok multi-target net8.0 or net9.0?**
   Recommended answer: no. Keep net10.0 until a real maintained consumer requires another target.

4. **Should Modularity support unload or hot reload?**
   Recommended answer: no. Startup-only registration is the stable and useful first boundary.

5. **Should a Localization package be the next addition?**
   Recommended answer: only after AppDefaults and Modularity are used in real applications and two independent localization consumers demonstrate the same compositor requirement.

6. **Should AppDefaults have one aggregate package?**
   Recommended answer: not initially. Add `Pocok.AppDefaults.Standard` only after at least three applications use the exact same composition.

---

# 17. Final recommendation

Do not treat the current four packages as four assets that all need polishing. Treat them as evidence from which two strong capability packages should emerge:

- Conversion;
- Readiness.

Then add two high-value capability areas:

- public application defaults for repeated cross-cutting configuration;
- public startup-time modularity for optional implementations and platform-specific dependencies.

The repository becomes a stronger portfolio project by deleting Primitives and the artificial Conversion abstraction split than it would by publishing more small libraries. The resulting story is credible:

- framework-first;
- third-party-first where infrastructure is already solved;
- custom code only around differentiated policy;
- explicit package boundaries;
- clean dependency graph;
- strong tests and package rehearsal;
- transparent opinionated defaults;
- trusted modular runtime;
- documented rejection of low-value abstractions.

That is systemic reusable design rather than a polished utility drawer.

---

# 18. Reference basis

Primary public references for implementation validation:

- NuGet PackageReference and transitive restore:
  https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files
- NuGet package restore behavior:
  https://learn.microsoft.com/en-us/nuget/consume-packages/package-restore
- NuGet pack and restore MSBuild targets:
  https://learn.microsoft.com/en-us/nuget/reference/msbuild-targets
- NuGet dependency resolution:
  https://learn.microsoft.com/en-us/nuget/concepts/dependency-resolution
- MinVer project-specific tag prefixes and independent package versioning:
  https://github.com/adamralph/minver
- .NET plugin tutorial and trust warning:
  https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support
- `AssemblyDependencyResolver`:
  https://learn.microsoft.com/en-us/dotnet/api/system.runtime.loader.assemblydependencyresolver
- Assembly unloadability limitations:
  https://learn.microsoft.com/en-us/dotnet/standard/assembly/unloadability
- McMaster.NETCore.Plugins:
  https://github.com/natemcmaster/DotNetCorePlugins
- Scrutor scope:
  https://github.com/khellang/Scrutor
- JSON schema exporter:
  https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/extract-schema
- Generic Host:
  https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host
- ASP.NET Core health checks:
  https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks
- Generic math:
  https://learn.microsoft.com/en-us/dotnet/standard/generics/math
