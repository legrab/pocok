# ADR 0005: Catalog-driven independent package publication

- Status: Accepted, revised after release rehearsal
- Date: 2026-07-14
- Revised: 2026-07-15

## Decision

Use package-specific Git tags, MinVer development versions, and one authoritative package catalog at `eng/packages.json`.

Each active package declares:

- its package ID and project path;
- a unique tag prefix;
- a unique MSBuild release-version property;
- release eligibility and tier;
- internal package dependencies;
- reviewed external dependency IDs;
- a clean external-consumer fixture.

A release tag resolves to exactly one releasable catalog entry. The workflow creates a generated props file before restore and injects:

- the candidate version from the release tag;
- the latest published tag version for every required internal dependency.

The same props file is used for restore, build, test, and pack. This prevents a package from declaring dependencies on unpublished development versions produced by independent MinVer prefixes.

## Package rehearsal

Two clean restore modes are required:

1. **Local closure:** the candidate restores from a local feed containing the complete repository package closure.
2. **Publication:** the candidate restores from a feed containing only the candidate plus nuget.org.

The first mode proves that project references were represented correctly as package dependencies. The second proves that required internal dependencies have actually been released.

Publication selects exact `.nupkg` and `.snupkg` files. Wildcard pushing is forbidden. Trusted publishing provides the temporary NuGet credential.

## Consequences

Independent package versions remain possible without hiding project-reference dependencies or accidentally publishing every artifact from a solution-wide pack. Dependency packages must be tagged and published before their consumers.
