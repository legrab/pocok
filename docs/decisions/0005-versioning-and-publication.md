# ADR 0005: Tag-derived package versions and publication

## Decision

Pocok package versions are derived from Git tags with MinVer. Each package owns
a tag prefix so a monorepo can release libraries independently. `Pocok.Primitives`
uses `primitives-v`, for example `primitives-v0.1.0-alpha.1`.

The corresponding release workflow validates the repository, packs the selected
project, publishes the package to nuget.org through trusted publishing, and
attaches the package and symbols to a GitHub Release.

## Rationale

Tag-derived versions keep the release commit, assembly metadata, package
metadata, and release name aligned without a version-bump commit. MinVer also
provides deterministic development versions between tags and does not require a
runtime dependency in the published package.

NuGet trusted publishing keeps long-lived API keys out of the repository and
uses a short-lived credential scoped to the configured GitHub workflow.

## Release contract

- Release tags use SemVer 2.0 after the package prefix.
- A package version is never reused after publication.
- CI must use a full Git history so MinVer can resolve tags.
- The NuGet trusted-publishing policy must name the exact repository and
  workflow file.
- A package is published only after tests, package smoke tests, and the public
  release audit pass.
