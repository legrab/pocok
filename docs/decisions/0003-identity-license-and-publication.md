# ADR 0003: Identity, license, and repository authority

- Status: Accepted, revised after repository consolidation
- Date: 2026-07-14
- Revised: 2026-07-20

## Decision

The repository family uses `Pocok` as its public namespace and NuGet package prefix. `legrab/pocok` is the authoritative
source repository for the current Conversion, Readiness, AppDefaults, Modularity, BackgroundWork, Scripting,
Localization, Signals, Subscriptions, and Licensing package families.

Earlier extraction ideas that placed Scripting or Signals in separate repositories are superseded. Package boundaries,
independent versions, and tag prefixes provide release isolation without splitting source authority or duplicating CI and
publication tooling.

Repository visibility does not bypass package admission. NuGet publication remains separately gated by package content,
API compatibility evidence, dependency closure, consumer smoke, cross-platform validation, and explicit tag approval.

All original Pocok source is licensed under Apache License 2.0. The repository contains the exact license text and a
`NOTICE` file. Hand-authored source files use this compact header:

```text
SPDX-License-Identifier: Apache-2.0
Copyright 2026 Pocok contributors
```

Generated files, snapshots, project/configuration files, and ordinary documentation do not receive repetitive source
headers. Package metadata declares `Apache-2.0`, and packages include the license, notice, README, repository metadata,
symbols, and Source Link.

An optional `STEWARDSHIP.md` may request attribution, upstream improvements, and responsible community participation. It
is explicitly nonbinding and does not narrow Apache-2.0 permissions. An optional trademark policy may prevent
impersonation of the Pocok project without restricting compatible forks.

## Consequences

Commercial, educational, internal, proprietary, and noncommercial consumers may use the libraries. Redistributions and
forks must preserve the license and applicable notices, while applications consuming the libraries do not inherit an
open-source requirement. All current package documentation, issue tracking, and repository provenance point to
`legrab/pocok`.
