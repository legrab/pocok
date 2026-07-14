# ADR 0003: Identity, License, and Publication

- Status: Accepted
- Date: 2026-07-14

## Decision

The repository family uses `Pocok` as its public namespace and NuGet package prefix:

- `legrab/pocok` contains small foundational `Pocok.*` packages;
- `legrab/pocok-scripting` contains `Pocok.Scripting.*` packages;
- `legrab/pocok-signals` contains `Pocok.Signals.*` packages.

All repositories are public from their first standalone commit. NuGet publication is separately gated by package-content, API, dependency, and consumer audits.

All original Pocok source is licensed under Apache License 2.0. Each repository contains the exact license text and a `NOTICE` file. Hand-authored source files use this compact header:

```text
SPDX-License-Identifier: Apache-2.0
Copyright 2026 Pocok contributors
```

Generated files, snapshots, project/configuration files, and ordinary documentation do not receive repetitive source headers. Package metadata declares `Apache-2.0`, and packages include the license, notice, README, repository metadata, symbols, and Source Link.

An optional `STEWARDSHIP.md` may request attribution, upstream improvements, and responsible community participation. It is explicitly nonbinding and does not narrow Apache-2.0 permissions. An optional trademark policy may prevent impersonation of the Pocok project without restricting compatible forks.

## Consequences

Commercial, educational, internal, proprietary, and noncommercial consumers may use the libraries. Redistributions and forks must preserve the license and applicable notices, while applications consuming the libraries do not inherit an open-source requirement. Repository visibility never bypasses the public package admission gate.
