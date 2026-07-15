# ADR 0002: Repository and package shape

- Status: Accepted, revised after consolidation
- Date: 2026-07-14
- Revised: 2026-07-15

## Decision

Keep this repository as one public monorepo with two explicit package families.

### Capability packages

- `Pocok.Conversion`
- `Pocok.Readiness`
- `Pocok.Modularity.Contracts`, experimental and release-gated
- `Pocok.Modularity`, experimental and release-gated

### Maintainer-default packages

- `Pocok.AppDefaults`
- `Pocok.AppDefaults.Logging`
- `Pocok.AppDefaults.Logging.Serilog`
- `Pocok.AppDefaults.Modularity`, experimental and release-gated

Maintainer-default packages are public and published through nuget.org. Their opinionated identity is expressed by package names and documentation, not by a private feed.

The stable dependency graph is:

```text
Pocok.Conversion
Pocok.Readiness

Pocok.AppDefaults
├── Pocok.AppDefaults.Logging
└── Pocok.AppDefaults.Logging.Serilog

Pocok.Modularity.Contracts
└── Pocok.Modularity
    └── Pocok.AppDefaults.Modularity
        └── Pocok.AppDefaults
```

The diagram shows only internal package dependencies. `Pocok.AppDefaults.Logging.Serilog` is an alternative provider policy and intentionally does not depend on provider-neutral `Pocok.AppDefaults.Logging`.

`Pocok.Primitives`, `Pocok.Hosting`, and `Pocok.Conversion.Abstractions` are retired package shapes. Useful behavior is owned by Conversion or Readiness instead of preserved through a generic foundation.

## Internal reuse

Package-local internal code is the default. Tiny identical helpers used by at least four projects may be linked explicitly from `src/Shared`, but no `Common`, `Utils`, `Foundation`, or non-packaged runtime assembly may sit beneath public packages.

## Consequences

The repository can release packages independently while keeping one review, test, and publication system. Capability packages do not depend on AppDefaults. Experimental Modularity projects remain buildable and testable in the repository but cannot be published until their catalog entries are explicitly made releasable.
