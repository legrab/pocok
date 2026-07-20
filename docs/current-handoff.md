# Current repository handoff

**Status revision:** Release Readiness Phase 2 implementation candidate prepared against PR #32 head `da13c00c8086b3ae86d368966a14f0f5efe19362`; executable proof remains pending.

## Current candidate state

- The repository continues to use schema-v1 `state`, `releaseTier`, and `releasable` metadata.
- The library graph contains eighteen non-retired packages: the original fifteen plus `Pocok.Scripting.JavaScript`, `Pocok.Scripting.CSharp`, and `Pocok.Scripting.Python`.
- All eighteen entries are candidate alpha-publication targets while API maturity remains unchanged. `Experimental` is not treated as a publication prohibition.
- Package-specific workflow triggers cover all eighteen tag prefixes.
- The synchronized `GLOBAL-v*` workflow retains the shared `pocok-publication` queue, deterministic dependency order, provenance-aware resume, and the current eighteen-package safety cap.
- The local Showcase has ten package-owned plugins covering all eighteen libraries. Multi-package ownership is explicit through `IShowcasePackageCoverage`.
- Logging and Localization execute bounded real package paths. Readiness, BackgroundWork, Modularity, Signals, and Subscriptions provide constrained source-accurate recipe builders.
- JavaScript remains the public Scripting engine. C# and Python remain trusted/local only and report truthful unavailability otherwise.

## Required proof before merge or release approval

1. Resolve the existing PR #32 CI and Showcase failures and obtain green Linux, Windows, public-release, Showcase, and Docker checks.
2. Review all new generated recipe snippets against the final public APIs.
3. Run `tools/ReleaseReadiness/Invoke-ReleaseReadinessRehearsal.ps1` with an unused prerelease version.
4. Review the exact eighteen package artifacts, symbols, candidate manifest, local-closure smoke, public audit, and complete Showcase publish/smoke output.
5. Confirm NuGet trusted-publishing registrations and external prerequisites without publishing.
6. Obtain explicit approval before creating one annotated `GLOBAL-v<version>` tag.

See [release-readiness-phase2-candidate.md](release-readiness-phase2-candidate.md) and [implementation/release-readiness-package-gate-audit.md](implementation/release-readiness-package-gate-audit.md).

MVP Closure remains responsible for schema-v2 typed tooling, immutable exact-artifact recovery, Showcase bundle packages, NuGet-backed Showcase composition, capacity beyond eighteen, browser/Docker public-feed proof, and Render rollout.
