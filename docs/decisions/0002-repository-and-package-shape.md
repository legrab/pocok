# ADR 0002: Repository and package shape

- Status: Accepted, revised after release-readiness implementation
- Date: 2026-07-14
- Revised: 2026-07-20

## Decision

Keep Pocok as one public monorepo with two explicit package families.

### Capability packages

- `Pocok.Conversion`
- `Pocok.Readiness`
- `Pocok.Modularity.Contracts`
- `Pocok.Modularity`
- `Pocok.BackgroundWork`
- `Pocok.Scripting`
- `Pocok.Scripting.JavaScript`
- `Pocok.Scripting.CSharp`
- `Pocok.Scripting.Python`
- `Pocok.Localization`
- `Pocok.Signals`
- `Pocok.Subscriptions`
- `Pocok.Licensing`

### Maintainer-default packages

- `Pocok.AppDefaults`
- `Pocok.AppDefaults.Logging`
- `Pocok.AppDefaults.Logging.Serilog`
- `Pocok.AppDefaults.Modularity`
- `Pocok.AppDefaults.Licensing`

Maintainer-default packages are public and published through nuget.org. Their opinionated identity is expressed by package
names and documentation, not by a private feed. `Active` and `Experimental` describe API maturity; release eligibility is
an independent catalog field.

The internal dependency graph is:

```text
Pocok.Conversion
├── Pocok.Scripting
│   ├── Pocok.Scripting.JavaScript
│   ├── Pocok.Scripting.CSharp
│   └── Pocok.Scripting.Python
└── Pocok.Signals

Pocok.Readiness

Pocok.BackgroundWork
└── Pocok.Localization

Pocok.Subscriptions

Pocok.Modularity.Contracts
└── Pocok.Modularity
    └── Pocok.AppDefaults.Modularity

Pocok.AppDefaults
├── Pocok.AppDefaults.Logging
├── Pocok.AppDefaults.Logging.Serilog
├── Pocok.AppDefaults.Modularity
└── Pocok.AppDefaults.Licensing

Pocok.Licensing
└── Pocok.AppDefaults.Licensing
```

The diagram shows package dependencies, not ownership or mandatory release waves. The two logging packages are alternative
provider policies. The Scripting core owns no language runtime; adapters are separate packages. Capability packages do not
depend on AppDefaults.

`Pocok.Primitives`, `Pocok.Hosting`, and `Pocok.Conversion.Abstractions` are retired package shapes. Useful behavior is
owned by focused packages instead of preserved through a generic foundation.

## Internal reuse

Package-local internal code is the default. Tiny identical helpers used by at least four projects may be linked explicitly
from `src/Shared`, but no `Common`, `Utils`, `Foundation`, or non-packaged runtime assembly may sit beneath public packages.

## Consequences

The repository can release packages independently or through one synchronized global tag while keeping one review, test,
and publication system. Experimental packages remain explicit alpha surfaces and may be published only when their catalog
entry is releasable and the same package, API, consumer, platform, and security gates pass.
