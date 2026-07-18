# Pocok post-MVP roadmap

**Started:** 2026-07-18

**Status:** Living collection; revisit after `repository-finalization.md` and `showcase-revision.md` complete

Use the matching skill under `.agents/skills/` when promoting an item into executable work. This roadmap records product ideas and deferral reasons only; it intentionally contains no engineering procedure.

## Purpose

Keep valuable ideas visible without expanding the MVP plans or pretending they are already designed. Items here need fresh repository evidence, prioritization, and a new implementation plan before work begins.

Do not move a correctness, security, public-contract, dependency, package-proof, release, or deployment blocker here merely to declare the MVP complete.

## Showcase and learning experience

| Idea | Value | Why deferred / prerequisite |
|---|---|---|
| Add real-code slices for BackgroundWork, Signals, Subscriptions, AppDefaults, Serilog defaults, Licensing defaults, and proven Modularity capabilities | High | First finish and observe the six MVP sample packages, shared static-sample pattern, and remote composition cost. |
| Add a third `System` theme state that follows browser preference continuously | Medium | MVP needs only a browser-derived initial choice plus explicit Light/Dark override. |
| Add more languages and translator tooling | Medium | English/Hungarian parity and resource ownership must remain stable first. |
| Add richer package compatibility/history views | Medium | Requires enough independently versioned sample/library releases to justify a dashboard. |
| Add guided comparisons between related packages | Medium | Avoid coupling package pages until individual sample contracts are stable. |
| Add optional screenshots or visual regression baselines | Medium | Establish stable theme/layout behavior and a maintainable cross-platform renderer first. |
| Add semantic Python and C# Monaco language services | Medium | MVP provides syntax support and truthful engine-aware completions. Full semantics require a maintained Python language server and Roslyn service/protocol with lifecycle, resource, deployment, and versioning proof. |
| Migrate Monaco from the pinned BlazorMonaco AMD integration to an ESM asset pipeline | Medium | MVP deliberately pins a locally served working integration and avoids adding Node. Migrate before an upstream/package upgrade removes the AMD distribution, with an explicit Node/bundler/security/CI ownership plan. |
| Add multi-file Monaco workspaces, fullscreen editing, and richer editor commands | Medium | First prove the single-document shared editor, accessibility, Interactive Server synchronization, and asset packaging across all composition modes. |
| Evaluate client-side/WASM presentation for static examples | Low | Current Interactive Server host and startup plugin model already solve the MVP; a second runtime has substantial duplication. |

## Logging and diagnostics

| Idea | Value | Why deferred / prerequisite |
|---|---|---|
| Extract a reusable public bounded in-app logging provider/sink | Medium | The MVP sink is Showcase-internal. Promote it only after at least two real non-Showcase consumers justify API, package, threading, security, and compatibility costs. |
| Authenticated/private operational log viewer | Medium | Public MVP deliberately shows only synthetic allowlisted demo events. Real operational logs require authentication, authorization, tenant isolation, retention, privacy, and incident-response policy. |
| Export or download sanitized demo logs | Low | First validate that the small rail console is useful and does not encourage operational-log misuse. |
| Pluggable formatting themes for console records | Low | Two accessible application themes and stable structured record semantics come first. |

## Release engineering

| Idea | Value | Why deferred / prerequisite |
|---|---|---|
| Signed provenance attestations, SBOM publication, and package-signing expansion | High | Complete the exact-artifact global release and Source Link proof first; then choose standards and signing authority deliberately. |
| Canary feed or staged mirror before nuget.org | Medium | The MVP global workflow uses complete pre-push proof and dependency waves. A second feed adds credentials, retention, and promotion policy. |
| Selective global release subsets | Medium | `GLOBAL-v*` intentionally means every non-retired packable package. Subsets complicate version and dependency guarantees and should follow real usage evidence. |
| Automated rollback orchestration | Low | NuGet packages are immutable and cannot be rolled back transactionally. Focus on provenance-checked resume and forward versions first. |
| Release dashboard with historical propagation timing | Low | Collect several global and independent releases before building reporting around them. |
| Multi-repository release graph | Low | Current repository is the independent boundary; do not create cross-repository project or release coupling without a proven ecosystem need. |

## Package and runtime evolution

| Idea | Value | Why deferred / prerequisite |
|---|---|---|
| Modularity unload, hot reload, or marketplace-style discovery | Medium | Startup-only trusted loading must first pass its Linux/Windows public release gate and gain real consumers. |
| Additional Signals protocol adapters, persistence, or caching | High for specific consumers | Keep the neutral runtime small until concrete adapters can pass the shared behavior suite without product leakage. |
| Hardened public C#/Python execution service | High for untrusted workloads | MVP child workers provide killable trusted/local execution, not an OS sandbox, and public Production exposes only Jint. Anonymous execution requires a separate container/process identity, filesystem/network isolation, quotas, abuse controls, cleanup, deployment, monitoring, and threat model. |
| Policy-grade scripting analysis and adversarial corpus | High for hostile workloads | MVP uses parser/AST/semantic guardrails before trusted execution. Taint/data-flow analysis, fuzzing, bypass research, signed policy packs, and formal policy compatibility need dedicated security engineering and still do not create a sandbox. |
| Persistent/local script repository and import workspace | High for script-heavy hosts | MVP preserves only current bounded JavaScript imports. Stored scripts, transitive cross-language imports, persistence, file watching, versioning, search, Monaco tabs/navigation, cache invalidation, and conflict handling need a separate storage and security design. |
| Additional CPython versions, environments, and third-party modules | Medium | MVP supports configured CPython 3.14 only with allowlisted standard-library samples. More versions, virtual environments, package installation, native-wheel compatibility, locking, vulnerability scanning, and deployment caching require an explicit compatibility matrix. |
| Additional scripting engines | Medium | Stabilize the neutral engine contract and gather JavaScript/C#/Python usage evidence before adding more runtimes or widening capability semantics. |
| Localization database/remote providers and advanced caching | Medium | Current explicit JSON/RESX/provider composition should gain usage evidence first. |
| BackgroundWork distributed coordination | Low | The package intentionally owns in-process helpers, not a scheduler or queueing platform. |
| Subscription transport lifecycle | Medium | Transport, retry, persistence, and network ownership are deliberately outside the current keyed in-process registry. |

## Licensing extensions

| Idea | Value | Why deferred / prerequisite |
|---|---|---|
| Online leases, revocation, floating seats, and rollback-resistant checkpoints | High for commercial deployments | Requires stateful/online infrastructure and a new security/availability model beyond offline MVP licensing. |
| Cloud KMS, HSM, PKCS#11, and certificate-store issuer adapters | Medium | Keep issuer keys outside the client and first prove the repository CLI lifecycle. |
| ASP.NET Core endpoint-filter/middleware helpers | Medium | Add only with a real host consumer; operation-level enforcement remains application-owned in MVP. |
| Source generator for `[RequiresLicense]` placement | Low | Attribute usage and analyzer value need evidence before adding compiler/tooling surface. |
| Tested trimming and NativeAOT compatibility tiers | Medium | Complete standard package release proof first, then define supported limitations explicitly. |

## Deployment, performance, and accessibility

| Idea | Value | Why deferred / prerequisite |
|---|---|---|
| Additional supported public deployment targets | Medium | Render is the MVP supported path. Validate operational demand before maintaining another platform matrix. |
| Custom domain, CDN, or multi-region deployment | Low | The stateless single-replica demo does not yet justify the operational surface. |
| Formal load/performance budgets for Showcase | Medium | Gather real package-resolution, startup, SignalR, and log-buffer measurements after remote composition is live. |
| Expanded automated accessibility audits | High | MVP includes keyboard, focus, contrast, and responsive checks; add broader tooling once the revised UI stabilizes. |
| Firefox/WebKit and Windows browser matrices | Medium | MVP uses pinned Playwright Chromium on Ubuntu plus Windows published-host proof. Add browsers/platforms after the interaction suite stabilizes and measured value justifies the CI cost. |
| Trimming/NativeAOT for the Showcase host | Low | Runtime plugin loading and Razor/Interactive Server make this a separate compatibility project, not an MVP optimization. |

## How to maintain this roadmap

- Add an item only with a short value statement and deferral reason.
- Link future plans or ADRs when an item becomes active; remove it from this list once another document owns execution.
- Re-rank after real releases and user feedback, not by document age.
- Keep product-specific, company-specific, customer-specific, and excluded application features outside this public repository.
