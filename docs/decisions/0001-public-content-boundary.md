# ADR 0001: Public Content Boundary

- Status: Accepted
- Date: 2026-07-14

## Decision

The library workspace contains reusable infrastructure only. It excludes the complete application, domain-specific orchestration subsystem, application UI/component surface, company/customer/project material, branding, activation, deployment configuration, production data, and runtime content.

Existing API compatibility is explicitly not required. Public contracts may be renamed, split, combined, or removed to improve cohesion, dependency direction, security, and long-term maintenance.

Before reusable implementation is selected, an allowlist-based staging script:

- copies only approved infrastructure and matching tests;
- removes version-control history, build output, IDE state, archives, credentials, databases, logs, packages, and application settings;
- removes copyright header lines and obsolete task identifiers;
- replaces private namespace and product tokens with neutral staging identifiers;
- scans both content and paths before implementation is moved into a package.

The staging tree is internal reference material and is never published directly.

## Consequences

The new libraries optimize for clear public contracts rather than drop-in replacement. Migration adapters, if ever useful, belong outside the stable cores. Every public package is reviewed from its built package contents rather than trusted merely because its project directory looks clean.
