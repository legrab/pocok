# Publication policy

> **Current status:** Initial consolidation applied. Follow the stabilization plan in `docs/plans/repository-consolidation.md` before tagging or publishing any package.

All public Pocok packages use nuget.org. Package intent is expressed through IDs, family metadata, and documentation rather than separate authenticated feeds.

## Authority

`eng/packages.json` is the authoritative package catalog. It defines:

- package ID and project path;
- package-specific tag prefix;
- package family, state, and release tier;
- release eligibility;
- internal package dependencies;
- reviewed external dependency IDs;
- release-version MSBuild property;
- clean external-consumer fixture.

Every active packable project must have exactly one catalog entry. A publication tag must match exactly one releasable entry.

## Initial release set and order

Release independent tier-zero packages first, in any order:

```text
Pocok.Conversion
Pocok.Readiness
Pocok.AppDefaults
```

After `Pocok.AppDefaults` exists on nuget.org, release:

```text
Pocok.AppDefaults.Logging
Pocok.AppDefaults.Logging.Serilog
```

The two logging packages are alternatives at the provider-policy layer. The Serilog package intentionally depends on `Pocok.AppDefaults`, not on provider-neutral `Pocok.AppDefaults.Logging`.

The Modularity family is not part of the initial public release. Its projects remain packable for clean-room verification, but `releasable` is false and no publication tag trigger exists.

## Version resolution

Development builds use MinVer with package-specific prefixes. Release builds additionally generate `artifacts/release-versions.props` before restore.

The generated file pins:

- the candidate package to the version encoded by its tag;
- every required internal dependency to its latest valid release tag.

The same file is supplied to restore, build, test, and pack. This prevents independently versioned projects from producing a candidate that depends on an unpublished development version of another Pocok package.

## Smoke modes

### Local closure

The candidate is restored from a clean local feed containing every `.nupkg` produced by the repository. This proves that the package dependency graph is complete and that no project reference is required by an external consumer.

### Publication

The candidate is restored from a feed containing only that candidate plus nuget.org. This proves that every internal dependency is already publicly resolvable.

Both modes use isolated package caches. Both must pass before push.

## Current tag formats

Only these prefixes trigger publication:

```text
conversion-v0.2.0
readiness-v0.1.0
appdefaults-v0.1.0
appdefaults.logging-v0.1.0
appdefaults.logging.serilog-v0.1.0
```

Modularity prefixes are reserved in the package catalog but deliberately absent from `.github/workflows/publish.yml` until the release gate is removed.

## Retired packages

`Pocok.Primitives` is retired without a forwarding package. Its existing nuget.org listing should be deprecated with a migration link. `Pocok.Hosting` and `Pocok.Conversion.Abstractions` were consolidated before publication and must not be introduced as compatibility packages.

## Release gates

A package is releasable only when:

- restore, formatting, Release build, and tests pass;
- package validation and the reviewed API inventory pass;
- local-closure and publication smoke tests pass;
- package-content audit passes;
- packaged README links render outside the source tree;
- symbols and Source Link are present;
- dependency IDs match the catalog allowlist;
- the exact candidate `.nupkg` and `.snupkg` are selected;
- the catalog entry has `releasable: true`.

Modularity additionally requires its real plugin fixture matrix on Linux and Windows before any release eligibility change.

## Release command

Publication is tag-driven. Create and push an annotated tag only after the dependency packages required by the candidate are already available on nuget.org.

```pwsh
git tag -a appdefaults-v0.1.0 -m "Release Pocok.AppDefaults 0.1.0"
git push origin appdefaults-v0.1.0
```

Never push package artifacts with a wildcard or manually reuse a published version.
