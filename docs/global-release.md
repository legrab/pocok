# Synchronized global package release

`GLOBAL-v<major.minor.patch[-prerelease]>` publishes every non-retired package whose current catalog entry has `releasable: true` at one exact version.

The workflow derives a dependency graph from `eng/packages.json`, rejects malformed graphs before authentication, and processes packages sequentially in deterministic dependency-first order. Independent packages are intentionally not published in parallel.

## Provenance-safe resume

A rerun for the same immutable global tag queries nuget.org before pushing anything. An exact package version is skipped only when its nuspec identifies `https://github.com/legrab/pocok` and the repository commit equals the global tag commit. An equal version from another commit, or one without sufficient repository metadata, stops the run because NuGet versions cannot be replaced.

Existing package-specific tags at the same version are accepted only when they resolve to the global tag commit. The global workflow never creates package-specific tags and requires no PAT or GitHub App token.

For each missing package the workflow pushes the explicit `.nupkg`, waits up to fifteen minutes for flat-container visibility, downloads and rechecks provenance, then runs publication-shaped restore before allowing dependents to continue.

## Capacity and recovery

The hosted job timeout is 345 minutes. The interim workflow rejects more than eighteen target packages so the configured per-package propagation bound and repository validation retain safety margin below GitHub's six-hour hosted-job limit.

The candidate, graph, preflight state, and final state are retained as Actions artifacts. A draft GitHub Release is finalized only after every package is publicly verified. Rerunning the same global tag recomputes public state and continues missing packages.

This schema-v1 implementation may rebuild candidate packages on a rerun. `docs/plans/repository-finalization.md` still owns migration to the typed release tool, schema v2, immutable candidate manifests and hashes, Showcase bundles, and artifact-reuse recovery.

## Release command

Only after all repository release gates pass on the exact commit:

```pwsh
git tag -a GLOBAL-v0.2.0-alpha.1 -m "Release synchronized Pocok packages 0.2.0-alpha.1"
git push origin GLOBAL-v0.2.0-alpha.1
```

Never move or recreate a pushed global tag. When provenance conflicts, select a new globally approved version.
