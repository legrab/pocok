# Pocok Showcase revision plan

**Plan date:** 2026-07-18
**Status:** Proposed; reconcile every slice with current source before execution
**Scope:** Showcase sample plugins/packages, source composition, hardening, and deployment

## Required skills and reading

- Use `.agents/skills/pocok-showcase-engineering/SKILL.md` for all Showcase implementation and proof.
- Also use `.agents/skills/pocok-package-engineering/SKILL.md` when a slice changes a demonstrated library or sample package contract.
- Also use `.agents/skills/pocok-release-engineering/SKILL.md` for sample packaging, NuGet composition, catalog, or release handoff.
- Use `.agents/skills/pocok-agentic-workflow/SKILL.md` only for plan revision, delegation, session evidence, or handoff.

Do not reload generic procedure from compatibility prompts. Read this plan, current source, and only the architecture/tests named by the selected skill and slice.

## Goal and completion

Complete the existing package-agnostic Showcase as the MVP browser and current-repository acceptance platform. Completion requires:

- real-code Logging, Readiness, and Localization plugins with English/Hungarian parity;
- a Licensing sign-read-validate smoke using synthetic in-memory material;
- JavaScript, C#, and Python Scripting tabs using the real engine family, equivalent samples, mandatory server validation, and a shared Monaco editor without per-keystroke caret resets;
- one executed current-build Showcase scenario or bounded acceptance probe for every non-retired library, explicitly supplemental to package QA;
- six independently packable, alpha-release-eligible sample packages;
- `LocalLibrariesLocalSamples`, `NuGetLibrariesLocalSamples`, and `NuGetLibrariesNuGetSamples` composition, with all-local as the MVP default;
- real NuGet composition and Render rollout only after the synchronized release in `repository-finalization.md`.

Preserve the current host/plugin architecture, existing Home content, Conversion/Scripting/Licensing behavior, responsive three-column workbench, Docker path, and bounded execution model.

## Boundaries

- Showcase scenarios are integration acceptance, not unit, contract, security, package, Source Link, or platform proof.
- Do not add a marketplace, general NuGet browser, database, plugin hot reload, request-time resolver, CSS framework rewrite, process-wide unauthenticated feature flags, persistent script repository, or multi-file script workspace.
- Do not reintroduce Koyeb.
- No slice authorizes package publication, tags, deployment, credentials, or Render mutation. S9 retains an explicit approval gate.

## Current baseline

The repository already has the package-agnostic host, shared components, manifest discovery through Modularity, English/Hungarian resources, Conversion/Scripting/Licensing plugins, publishing/smoke tooling, Docker, Render, and the widened three-column workbench. The host shell already provides installed-sandbox rail navigation, semantic dark/warm-light themes, and a bounded allowlisted in-app console with operator and per-circuit controls. Reuse these surfaces; their current contract is documented in `showcase/docs/ARCHITECTURE.md`. Treat every other classification in an older plan or handoff as provisional until verified in source.

## Sequential runbook

| Slice | Depends on | Result handed forward |
|---|---|---|
| S1 state and coverage | Repository R1-R7 | State lifetimes, current-build provenance, package-to-scenario ledger |
| S2 composition contract | S1, repository release graph | Validated common composition record |
| S3 sample coverage and Scripting UI | S1 and repository R2 engine/package contracts | Six working plugins and complete local acceptance ledger |
| S4 sample packaging | S2, S3 | Six catalogued alpha-candidate sample packages |
| S5 tri-state composition | S4 | Three temporary-feed-proven source modes |
| S6 hardening | S5 | Linux/Windows, browser, published-host, and Docker evidence |
| S7 current documentation | S6 | Exact handoff to repository R8-R10 |
| S8 real NuGet composition | Repository R10, S5 | Clean public-feed composition proof |
| S9 Render rollout | S8 and explicit approval | Verified deployment or recorded rollback |

Separate plugin directories may be implemented concurrently, but shared Components, shell/CSS, catalogs, publication tooling, and workflows each have one integration owner. The owning skills define collaboration and validation procedure.

## S1. Define state, provenance, and acceptance coverage

Create one authoritative package-to-scenario ledger from `eng/packages.json`. Each non-retired library must map to a direct sandbox, real host exercise, or deterministic bounded probe and name the public behavior invoked. Assembly loading alone is not coverage.

Record source mode, package/assembly version, relative staged path, and current repository provenance without exposing absolute paths. Validate composition configuration at startup.

**Accept when:** the baseline published-host smoke passes, state lifetimes are documented, and every current library has an honest initial ledger classification.

## S2. Define one composition record

Create one machine-readable record shared by all source modes. Per plugin it identifies mode, plugin/sample/library IDs and versions, the authoritative supported range, compatibility state, and safe resolution metadata. It contains no credentials, secret feed URLs, caches, extraction paths, or absolute paths.

Each sample owns one primary-library compatibility declaration; packed dependency metadata and the composition record must agree with it. Reject duplicate IDs, unsafe paths, malformed/disagreeing ranges, missing assets, shared/private assembly conflicts, and unsupported selection before readiness. The host consumes the validated record and manifests; it never resolves packages.

**Accept when:** schema, round-trip, range, path-safety, failure-fixture, and cross-mode equivalence tests pass.

## S3. Complete real-code sample coverage

Add `Pocok.Showcase.AppDefaults.Logging`, `Pocok.Showcase.Readiness`, and `Pocok.Showcase.Localization` plugins. Keep explanations to what is shown, why the result occurred, and one important boundary. The Logging page emits real structured records with timestamp and namespace shortening; Readiness demonstrates deterministic lifecycle/status behavior; Localization demonstrates current culture/provider/fallback behavior.

Extend Licensing so its default smoke creates an ephemeral ECDSA key, signs a synthetic document, reads/verifies it with the public key, validates verified claims/runtime facts, and demonstrates one safe tamper/untrusted-key boundary. Never persist or render private keys, full envelopes, secrets, or machine-sensitive values.

Complete the S1 ledger with bounded shared probes for packages that do not justify a sandbox. Modularity's existing Showcase composition is the primary product-level smoke; add no redundant Modularity page solely for coverage.

### S3a. Complete the Scripting engine experience

Prerequisite: repository R2 provides alpha-candidate `Pocok.Scripting`, `.JavaScript`, `.CSharp`, and `.Python` packages with truthful descriptors and mandatory validators.

- Add engine tabs derived from registered engines, explicit unavailable states, and separate circuit-local source per sample/engine.
- Provide equivalent success/boundary samples for JavaScript, C#, and Python. Include harmless expected validator rejection for JavaScript `eval`, C# reflection/assembly loading, and Python dangerous import or `eval`; rejected code must never reach an engine.
- Carry engine ID, validator policy, truthful enforced limits, progress, result, and diagnostics through typed input/output. Do not claim an unsupported bound.
- Add one shared, locally packaged Monaco component in Showcase Components. It owns browser models, local assets, themes, markers, disposal, resize, and language switching. Python/C# receive syntax and engine-aware completions only unless a real language service exists.
- Keep browser editor state live. Coalesce server synchronization around 500 ms and flush before Run, blur, reset, engine switch, and disposal; external resets must not move the caret during ordinary typing.
- Retain the shared buffered textarea as accessible failure/no-JavaScript fallback. Monaco failure is visible and never discards the latest committed source.
- Use only conceptual insight from the supplied legacy snippets; copy no proprietary source, styling, product names, persistence/import navigation, or unrelated dependencies.

**Accept when:** engine parity, unavailable runtimes, validator-before-invocation, exact pre-action flush, caret stability, source bounds, diagnostics, reconnect/disposal, keyboard labels, themes, resize, and local/package/Docker Monaco assets pass with no runtime CDN request.

**S3 complete when:** all six plugins use current public APIs, stage independently, appear through generic discovery, and the local ledger executes every non-retired library with current repository assemblies.

## S4. Package the six samples

Create these independently versioned packages:

- `Pocok.Showcase.Conversion`
- `Pocok.Showcase.Scripting`
- `Pocok.Showcase.Licensing`
- `Pocok.Showcase.AppDefaults.Logging`
- `Pocok.Showcase.Readiness`
- `Pocok.Showcase.Localization`

Each has its own tag prefix/version property, README and metadata, manifest/resources/assets, compatibility declaration, exact private dependencies, and `eng/packages.json` graph entry. Shared host/framework assemblies must not be duplicated accidentally. Keep package-specific tags and global-release participation.

**Accept when:** every exact package passes content, compatibility, isolated extraction, clean temporary-feed installation, symbols/metadata, and alpha-eligibility proof defined by the package/release skills.

## S5. Implement the three source modes

Use one validated enum/string setting:

1. `LocalLibrariesLocalSamples` — default for MVP development and CI.
2. `NuGetLibrariesLocalSamples` — local sample projects compile against exact NuGet libraries.
3. `NuGetLibrariesNuGetSamples` — host composition uses packaged samples and compatible packaged libraries.

Local mode accepts only current repository outputs and never falls back to cache/NuGet. Hybrid mode proves package references were used. Full mode resolves/extracts before startup, selects only compatible versions, and reports newer incompatible libraries without silently substituting them. Make build-time versus runtime evaluation explicit; credentials never enter records or images.

**Accept when:** all three modes generate equivalent logical records and start the published host from deterministic temporary feeds; local mode also completes the current-build acceptance ledger.

## S6. Harden completed behavior

Extend existing CI, published-host smoke, Docker, and browser coverage to the new plugins, generated installed-route set, composition modes, resources, Scripting editor/engine behavior, readiness, and existing host-shell regression suite. Configure CPython explicitly where claimed and report unavailable runtimes truthfully.

**Accept when:** Linux and Windows Showcase workflows, clean publication, generated-route smoke, browser behavior, and Docker proof pass under the Showcase skill's evidence rules.

## S7. Reconcile current documentation

Update only documentation whose behavior is now implemented: root/Showcase READMEs, architecture, adding-a-slice, security, Docker/Render, publication, and current handoff. Document the three modes, engine availability/security, Monaco ownership/fallback, and sample compatibility concisely. Link to package READMEs instead of copying contracts.

**Accept when:** a clean maintainer can reproduce each proven mode and the handoff names the exact commit, package IDs, graph, and evidence for repository R8-R10.

## S8. Prove real NuGet composition

After repository R10, compose from nuget.org with clean caches and no sample-project build. Verify selected versions/ranges/source, the controlled newer-incompatible state, clean publish/image contents, readiness, all installed routes, and the existing host-shell smoke.

**Accept when:** no local project/cache masks the released packages and the public-feed composition record matches the release manifest.

## S9. Roll out Render

**Approval required:** obtain approval for the exact commit/image, versions, and composition record before changing or deploying Render.

Keep all-local as the repository recovery/default mode. Configure full-NuGet composition before startup, preserve current health/port/forwarding/stateless constraints, verify every route and SignalR reconnect, and confirm no secrets, paths, caches, unsafe logs, or license material are public. Record deployed inputs and rollback state.

**Accept when:** the approved revision is healthy and verified, or rollback succeeds with the failed check and next action recorded.

## Final evidence

Completion requires separately labelled package proof, local current-build Showcase acceptance, temporary-feed composition, Linux/Windows and browser evidence, real NuGet composition, and deployment evidence. After S9 return to R11 in `repository-finalization.md`. Only minor visual tuning, small frontend defects, or items already recorded in `post-mvp-roadmap.md` may remain.
