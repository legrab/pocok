# Publication policy

> **Current status:** The initial package family is structurally consolidated. Do not tag a package until the executable acceptance commands in this document pass on the exact commit being tagged.

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

Every active packable project must have exactly one catalog entry. A publication tag must match exactly one releasable entry. `tools/PackageCatalog/Resolve-PackageClosure.ps1` resolves the candidate and its transitive internal dependencies in dependency-first order.

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

## Candidate closure

The publication workflow builds and tests `Pocok.Core.slnx`, then packs only the candidate and its transitive internal package closure. The package directory is cleaned first, so an audit cannot accidentally pass against stale or unrelated artifacts.

The closure is dependency-first. For example:

```text
Pocok.AppDefaults
Pocok.AppDefaults.Logging
```

No retired or experimental package may enter a releasable candidate closure.

## Smoke modes

### Local closure

The candidate consumer restores from:

- a clean local feed containing only the candidate and its transitive Pocok dependencies;
- nuget.org for reviewed external dependencies.

Package source mapping forces `Pocok.*` to the local feed. A missing internal package therefore cannot be hidden by an already-published copy. This proves that the generated package closure is complete and that no project reference is required by an external consumer.

### Publication

The candidate consumer restores from:

- a local feed containing only the exact candidate;
- nuget.org for exact internal dependency IDs and reviewed external dependency families.

This proves that all internal dependencies required by the candidate are already publicly resolvable. Publication mode is expected to fail when a required dependency has not yet been released.

Both modes use isolated package caches and generated `NuGet.Config` files. Both must pass before push.

## Package audit

`tools/PublicReleaseAudit/Invoke-PublicReleaseAudit.ps1` accepts an optional candidate package ID and then audits exactly that closure. It rejects missing, duplicate, stale, and unrelated package artifacts. The audit verifies:

- package identity and exact file names;
- license, project URL, repository metadata, and package README declaration;
- reviewed dependency IDs and concrete versions;
- internal dependency versions matching the closure artifacts;
- package-local README links;
- assembly XML documentation;
- matching symbols packages and portable PDB presence;
- absence of repository-only files, retired projects, and obvious secret material.

NuGet package validation remains enabled in MSBuild. The final release gate also requires clean installation and sample execution because archive inspection alone cannot prove runtime behavior or debugger Source Link behavior.

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

A package is releasable only when, on the exact candidate commit:

- restore, formatting, Release build, and focused tests pass;
- member-level API snapshots and NuGet package validation pass;
- relevant samples run, including the trimmed Conversion sample;
- local-closure and publication smoke tests pass;
- candidate-scoped package-content audit passes;
- packaged README links render outside the source tree;
- symbols, repository metadata, and Source Link behavior are verified;
- dependency IDs match the catalog allowlist;
- the exact candidate `.nupkg` and `.snupkg` are selected;
- Linux and Windows CI pass;
- the catalog entry has `releasable: true`.

Modularity additionally requires its real plugin fixture matrix on Linux and Windows before any release eligibility change.

## Local acceptance

```pwsh
dotnet restore Pocok.slnx
dotnet format Pocok.slnx --verify-no-changes --no-restore
dotnet build Pocok.slnx --configuration Release --no-restore
dotnet test Pocok.slnx --configuration Release --no-build
dotnet pack Pocok.slnx --configuration Release --no-build --output artifacts/packages

./tools/PackageCatalog/Test-PackageCatalog.ps1
./tools/PackageSmoke/Invoke-PackageSmoke.ps1 -NoPack -Mode LocalClosure
./tools/PublicReleaseAudit/Invoke-PublicReleaseAudit.ps1
```

For an actual candidate, generate release-version props and run both smoke modes for that package before pushing the tag.

## Release command

Publication is tag-driven. Create and push an annotated tag only after the dependency packages required by the candidate are already available on nuget.org.

```pwsh
git tag -a appdefaults-v0.1.0 -m "Release Pocok.AppDefaults 0.1.0"
git push origin appdefaults-v0.1.0
```

Never push package artifacts with a wildcard or manually reuse a published version.
