# Synchronized global package release

`GLOBAL-v<major.minor.patch[-prerelease]>` publishes every non-retired package whose current catalog entry has `releasable: true` at one exact version.

The current workflow derives a dependency graph from `eng/packages.json`, rejects malformed graphs before authentication, and processes packages sequentially in deterministic dependency-first order. Independent packages are intentionally not published in parallel.

Package-specific and global publication share:

```yaml
concurrency:
  group: pocok-publication
  cancel-in-progress: false
  queue: max
```

This prevents overlapping publication while retaining queued release tags instead of replacing an older pending publication.

## Provenance-safe resume

A rerun for the same immutable global tag queries nuget.org before pushing anything. An exact package version is skipped only when its nuspec identifies `https://github.com/legrab/pocok` and the repository commit equals the global tag commit.

An equal version from another commit, or one without sufficient repository metadata, stops the run because NuGet versions cannot be replaced.

Existing package-specific tags at the same version are accepted only when they resolve to the global tag commit. The global workflow never creates package-specific tags and requires no PAT or GitHub App token.

For each missing package the workflow pushes the explicit `.nupkg`, waits up to fifteen minutes for flat-container visibility, downloads and rechecks provenance, then runs publication-shaped restore before allowing dependents to continue.

## Capacity and recovery

The hosted job timeout is 345 minutes. The current schema-v1 workflow rejects more than eighteen target packages so the configured propagation bound and repository validation retain safety margin below the hosted-job limit.

The candidate, graph, preflight state, and final state are retained as Actions artifacts. A draft GitHub Release is finalized only after every package is publicly verified. Rerunning the same global tag recomputes public state and continues missing packages when provenance matches.

The current implementation may rebuild candidates on a rerun. It is suitable for the first library-only synchronized prerelease after [`plans/release-readiness.md`](plans/release-readiness.md) expands the graph to exactly eighteen libraries and completes their gates.

[`plans/mvp-closure.md`](plans/mvp-closure.md) owns the later upgrades:

- schema-v2 catalog and typed release authority;
- immutable candidate manifest and hashes;
- automated Source Link and installed-candidate proof;
- rebuild-free retained-artifact recovery;
- ten Showcase bundle packages;
- capacity beyond eighteen targets;
- full library-plus-bundle synchronized release.

That work upgrades this workflow rather than creating a competing global release path.

## Approval and release command

Only after all Release Readiness gates pass on the exact commit, present the commit, proposed version, target list, dependency order, and zero-push evidence for explicit approval.

After approval:

```pwsh
git tag -a GLOBAL-v0.2.0-alpha.1 -m "Release synchronized Pocok packages 0.2.0-alpha.1"
git push origin GLOBAL-v0.2.0-alpha.1
```

Never move or recreate a pushed global tag. When provenance or version preflight conflicts, select a new version and obtain approval again.

NuGet has no transactional rollback. On partial publication, retain the immutable tag, record published and pending packages, and use only the workflow's provenance-safe same-tag resume.
