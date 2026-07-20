# ADR 0005: Catalog-driven package and synchronized publication

- Status: Accepted, revised after global release implementation
- Date: 2026-07-14
- Revised: 2026-07-20

## Decision

Use package-specific Git tags, optional synchronized `GLOBAL-v*` tags, MinVer development versions, and one authoritative
package catalog at `eng/packages.json`.

Each package declares:

- its package ID and project path;
- a unique tag prefix;
- a unique MSBuild release-version property;
- API-maturity state and independent release eligibility;
- release tier and internal package dependencies;
- reviewed external dependency IDs;
- a clean external-consumer fixture.

A package-specific release tag resolves to exactly one releasable catalog entry. A synchronized global tag resolves every
current releasable entry and publishes one exact version in deterministic dependency-first order. Both workflows use the
same publication concurrency group and trusted-publishing boundary.

The workflow creates a generated props file before restore and injects:

- the candidate version from the release tag;
- the exact intended version for every required internal dependency.

The same props file is used for restore, build, test, and pack. This prevents independently versioned projects from
producing a candidate that depends on unpublished development versions.

## Package rehearsal

Two clean restore modes are required:

1. **Local closure:** the candidate restores from a local feed containing the complete repository package closure.
2. **Publication:** the candidate restores from a feed containing only the candidate plus nuget.org.

The first mode proves that project references were represented correctly as package dependencies. The second proves that
required internal dependencies have actually been released.

Publication selects exact `.nupkg` and `.snupkg` files. Wildcard pushing is forbidden. Trusted publishing provides the
temporary NuGet credential.

## Synchronized provenance and recovery

A rerun of an immutable global tag may skip an exact public package version only when its nuspec repository URL and commit
match the tag commit. Conflicting or unprovable equal versions stop the release. Same-tag recovery continues only missing
packages and never moves or recreates the tag.

## Consequences

Independent package versions remain possible without hiding project-reference dependencies or accidentally publishing
every artifact from a solution-wide pack. A synchronized prerelease can establish one coherent version across the full
releasable graph. Dependency packages must be publicly available before package-specific dependents, while the global
workflow enforces that order automatically.
