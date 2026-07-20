# Current repository handoff

**Status revision:** Documentation-aligned final implementation snapshot. This record describes the repository shape; it
does not claim that a package or global release tag has been approved or pushed.

## Current implementation state

- The schema-v1 package catalog contains eighteen non-retired packages and three retired package records.
- All eighteen non-retired entries are `releasable: true`. `Active` and `Experimental` describe API maturity rather than
  whether alpha publication is permitted.
- Package-specific workflow triggers cover all eighteen NuGet tag prefixes.
- `GLOBAL-v*` resolves the complete eighteen-package graph in deterministic dependency-first order, uses the shared
  `pocok-publication` queue, and supports provenance-aware same-tag resume.
- Scripting is split into an engine-neutral core plus JavaScript, C#, and Python adapter packages.
- JavaScript is available in the public Showcase. C# and Python are trusted-local only and report truthful unavailability
  unless an operator explicitly enables them and supplies the required runtimes and worker paths.
- The local Showcase has ten manifest-loaded plugins covering all eighteen libraries. Coverage by a multi-package plugin
  is explicit in its module metadata.
- Conversion, AppDefaults/Logging, Scripting, Licensing, and Localization provide bounded real demonstrations.
  Readiness, BackgroundWork, Modularity, Signals, and Subscriptions provide constrained source-accurate recipe builders.
- The root README now lists all package badges in their functional rows and contains collapsible technical quick starts
  corresponding to every console sample family.

## Documentation authority

Use current source and these active documents for ordinary work:

- [`README.md`](../README.md) for package discovery and quick starts;
- [`PUBLICATION.md`](../PUBLICATION.md) for package-specific and global release policy;
- [`docs/ci.md`](ci.md) for affected-slice and complete-graph CI behavior;
- [`docs/global-release.md`](global-release.md) for synchronized release and recovery;
- [`showcase/README.md`](../showcase/README.md) and its deployment guides for the ten-plugin Showcase;
- package-local `src/*/README.md` files for API boundaries and focused usage.

Files under `docs/plans`, `docs/implementation`, `docs/migrations`, and `sessions` are retained design, migration, or
execution records. Their historical statements are not a substitute for the current catalog or source.

## Verification and release boundary

Before merge or release approval on an exact commit:

1. Obtain green Linux, Windows, public-release, Showcase, and Docker checks.
2. Run `tools/ReleaseReadiness/Invoke-ReleaseReadinessRehearsal.ps1` with an unused prerelease version.
3. Review all eighteen package artifacts and symbols, the candidate manifest, local-closure smoke, public audit, and
   complete Showcase publication/smoke output.
4. Confirm NuGet trusted-publishing registrations and all external runtime prerequisites without publishing.
5. Obtain explicit approval before creating one annotated package-specific or `GLOBAL-v<version>` tag.

MVP Closure remains responsible for schema-v2 typed release authority, immutable exact-artifact recovery, package-backed
Showcase bundles, capacity beyond eighteen targets, public-feed browser/Docker proof, and final deployment rollout.
