# Publication policy

> **Current status:** `eng/packages.json` contains eighteen non-retired NuGet packages and marks all eighteen releasable.
> `Active` and `Experimental` describe API maturity; neither state bypasses the release gates below. Do not create or
> push a tag until the executable acceptance commands pass on the exact commit and release approval is explicit.

All public Pocok packages use nuget.org. Package intent is expressed through IDs, family metadata, catalog state, and
documentation rather than separate authenticated feeds.

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

Every active packable project must have exactly one catalog entry. A package-specific publication tag must match exactly
one releasable entry. `tools/PackageCatalog/Resolve-PackageClosure.ps1` resolves the candidate and its transitive internal
dependencies in dependency-first order.

## Current release graph

The catalog currently describes this internal package graph:

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

The diagram shows internal dependencies, not a required single publication wave. Independent packages may be released in
any order. Dependents may be published only after their exact internal dependency versions are publicly resolvable.

`Pocok.AppDefaults.Logging` and `Pocok.AppDefaults.Logging.Serilog` are alternatives at the provider-policy layer. The
Serilog package intentionally depends on `Pocok.AppDefaults`, not on provider-neutral logging defaults.

`Pocok.Licensing` has no internal NuGet dependency. `Pocok.Licensing.Keygen` and
`Pocok.Licensing.LicenseChecker` remain non-packable executables: their tags produce self-contained Windows, Linux, and
macOS GitHub Release archives and never enter the NuGet package catalog.

## Version resolution

Development builds use MinVer with package-specific prefixes. Release builds additionally generate
`artifacts/release-versions.props` before restore.

The generated file pins:

- the candidate package to the version encoded by its tag;
- every required internal dependency to the latest valid release tag, or to the synchronized global candidate version
  when the global workflow is used.

The same file is supplied to restore, build, test, and pack. This prevents independently versioned projects from producing
a candidate that depends on an unpublished development version of another Pocok package.

## Candidate closure

The package-specific workflow builds and tests `Pocok.Core.slnx`, then packs only the candidate and its transitive
internal package closure. The package directory is cleaned first, so an audit cannot accidentally pass against stale or
unrelated artifacts.

A candidate closure may contain `Active` or `Experimental` packages. It may not contain retired, unknown, or
`releasable: false` entries.

## Smoke modes

### Local closure

The candidate consumer restores from:

- a clean local feed containing only the candidate and its transitive Pocok dependencies;
- nuget.org for reviewed external dependencies.

Package source mapping forces `Pocok.*` to the local feed. A missing internal package therefore cannot be hidden by an
already-published copy. This proves that the generated package closure is complete and that no project reference is
required by an external consumer.

### Publication

The candidate consumer restores from:

- a local feed containing only the exact candidate;
- nuget.org for exact internal dependency IDs and reviewed external dependency families.

This proves that all internal dependencies required by the candidate are already publicly resolvable. Publication mode is
expected to fail when a required dependency has not yet been released.

Both modes use isolated package caches and generated `NuGet.Config` files. Both must pass before push.

## Package audit

`tools/PublicReleaseAudit/Invoke-PublicReleaseAudit.ps1` accepts an optional candidate package ID and audits exactly that
closure. It rejects missing, duplicate, stale, and unrelated package artifacts. The audit verifies:

- package identity and exact file names;
- license, project URL, repository metadata, and package README declaration;
- reviewed dependency IDs and concrete versions;
- internal dependency versions matching the closure artifacts;
- package-local README links;
- assembly XML documentation;
- matching symbols packages and portable PDB presence;
- absence of repository-only files, retired projects, and obvious secret material.

NuGet package validation remains enabled in MSBuild. The final release gate also requires clean installation and sample
execution because archive inspection alone cannot prove runtime behavior or debugger Source Link behavior.

## Current package-specific tag prefixes

Each prefix below triggers `.github/workflows/publish.yml` when followed by a SemVer-compatible version:

```text
conversion-v<version>
readiness-v<version>
appdefaults-v<version>
appdefaults.logging-v<version>
appdefaults.logging.serilog-v<version>
modularity.contracts-v<version>
modularity-v<version>
appdefaults.modularity-v<version>
backgroundwork-v<version>
scripting-v<version>
scripting.javascript-v<version>
scripting.csharp-v<version>
scripting.python-v<version>
localization-v<version>
signals-v<version>
subscriptions-v<version>
licensing-v<version>
appdefaults.licensing-v<version>
```

The GitHub-only licensing executable prefixes are:

```text
licensing.keygen-v<version>
licensing.licensechecker-v<version>
```

They are handled by `.github/workflows/publish-licensing-tool.yml`, not by the NuGet publication workflow.

## Retired packages

`Pocok.Primitives` is retired without a forwarding package. Its existing nuget.org listing should be deprecated with a
migration link. `Pocok.Hosting` and `Pocok.Conversion.Abstractions` were consolidated before publication and must not be
introduced as compatibility packages.

## Release gates

A package is releasable only when, on the exact candidate commit:

- restore, formatting, Release build, and focused tests pass;
- member-level API snapshots and NuGet package validation pass;
- relevant samples run, including the explicit trimmed-array Conversion smoke fixture;
- trim-incompatible public APIs surface `RequiresUnreferencedCode` rather than leaking internal linker warnings;
- local-closure and publication smoke tests pass;
- candidate-scoped package-content audit passes;
- packaged README links render outside the source tree;
- symbols, repository metadata, and Source Link behavior are verified;
- dependency IDs match the catalog allowlist;
- the exact candidate `.nupkg` and `.snupkg` are selected;
- Linux and Windows CI pass;
- the catalog entry has `releasable: true`.

Modularity additionally retains its trusted startup-only boundary and cross-platform clean-room fixture coverage. Scripting
adapters retain their engine-specific runtime, worker-integrity, validation, and isolation gates. Licensing tool releases
additionally require the keygen-to-checker round trip and successful self-contained publication for every declared runtime
identifier.

Release archives contain the executable, `LICENSE`, `NOTICE`, and the licensing guide; private keys and generated licenses
are never release assets.

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

## Package-specific release command

Publication is tag-driven. Create and push an annotated tag only after the dependency packages required by the candidate
are already available on nuget.org.

```pwsh
git tag -a localization-v0.1.0-alpha.1 -m "Release Pocok.Localization 0.1.0-alpha.1"
git push origin localization-v0.1.0-alpha.1
```

GitHub-only licensing tools use the same annotated-tag discipline:

```pwsh
git tag -a licensing.keygen-v0.1.0-alpha.1 -m "Release Pocok Licensing Keygen 0.1.0-alpha.1"
git push origin licensing.keygen-v0.1.0-alpha.1
```

Never push package artifacts with a wildcard or manually reuse a published version.

## Synchronized global tags

`GLOBAL-v<major.minor.patch[-prerelease]>` publishes every currently releasable package at one exact version through
`.github/workflows/publish-global.yml`. The graph is derived from `eng/packages.json` and released sequentially in
dependency-first order. Existing exact versions are skipped only when their nuspec repository commit matches the global
tag commit. Conflicting or unprovable equal versions fail before any push. The workflow does not create package-specific
tags and requires no PAT.

See [docs/global-release.md](docs/global-release.md) for provenance-safe resume, capacity, approval, and recovery rules.
