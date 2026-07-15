# Publication policy

All public packages are published to nuget.org. Package intent is expressed through IDs and catalog metadata, not separate authenticated feeds.

## Authority

`eng/packages.json` is the authoritative package catalog. It defines:

- package ID and project path;
- tag prefix;
- package family and release tier;
- release eligibility;
- internal package dependencies;
- reviewed external dependency IDs;
- external-consumer fixture.

A packable project must have exactly one catalog entry. A tag must match exactly one releasable entry.

## Release order

Internal dependencies must be released before their consumers. The current tiers are validated by `tools/PackageCatalog/Test-PackageCatalog.ps1`.

## Smoke modes

### Local closure

The candidate is restored from a clean local feed containing every package produced by the repository. This proves that the nupkg dependency graph is complete and usable without project references.

### Publication

The candidate is restored from a feed containing only that candidate plus nuget.org. This proves that every internal dependency declared by the candidate is already publicly resolvable.

Both modes use isolated package caches. The publication workflow must pass both modes before pushing.

## Tag format

Tags use the catalog prefix followed by a semantic version, for example:

```text
conversion-v0.2.0
readiness-v0.1.0
appdefaults-v0.1.0
appdefaults.logging-v0.1.0
appdefaults.logging.serilog-v0.1.0
modularity.contracts-v0.1.0
modularity-v0.1.0
appdefaults.modularity-v0.1.0
```

## Retired packages

`Pocok.Primitives` is retired without a forwarding package. Its nuget.org listing should be deprecated with a link to `docs/migrations/primitives-retirement.md`. A retired ID must not remain packable merely to preserve a poor dependency boundary.

## Release gates

A package is releasable only when:

- restore, formatting, build, and tests pass;
- package validation and API compatibility pass;
- local-closure and publication smoke tests pass;
- package audit passes;
- README links render outside the source tree;
- symbols and Source Link are present;
- dependency IDs match the reviewed catalog;
- the package's catalog entry has `releasable: true`.

Modularity packages remain non-releasable until their real plugin fixture matrix passes on supported operating systems.
