# Agent instructions

## Start here

The repository and executable evidence are current truth. Plans, handoffs, and sessions may drift.

Load only the skill matching the work:

| Work | Canonical skill |
|---|---|
| Packages, public APIs, package tests, consumers, samples, and alpha readiness | `.agents/skills/pocok-package-engineering/SKILL.md` |
| Catalog, packing, NuGet, tags, release workflows, and release evidence | `.agents/skills/pocok-release-engineering/SKILL.md` |
| Showcase host, components, plugins, composition, publication, and browser behavior | `.agents/skills/pocok-showcase-engineering/SKILL.md` |
| Agent guidance, plans, sessions, handoff, validation language, or collaboration | `.agents/skills/pocok-agentic-workflow/SKILL.md` |

When work crosses domains, use only the involved skills. Follow their selective-reading instructions instead of loading every repository document. `docs/agentic-workflow.md` and `prompts/agent-base*` are compatibility pointers, not additional procedure.

## Stable repository boundary

- Treat this directory as an independent repository; project references never escape it.
- Do not add the excluded complete application, business-workflow engine, or application UI surface.
- Public content remains product-neutral and contains no company, customer, historical, operational, secret, or proprietary material.
- Keep source, configuration, JSON, and tooling files UTF-8 without BOM.
- Original hand-authored source uses `SPDX-License-Identifier:Apache-2.0` and `Copyright 2026 Pocok contributors`; generated files, snapshots, project/configuration files, and ordinary documentation do not need repetitive headers.
- Preserve `LICENSE`, `NOTICE`, third-party notices, and provenance. A stewardship request is nonbinding and never narrows Apache-2.0 permissions.

## Stable implementation constraints

- Enable nullable reference types and warnings as errors. Never swallow failures.
- Fast tests use NUnit, Shouldly, and Verify and require no network, database, browser, device, or machine-specific configuration.
- Intentional snapshots are reviewed and updated, never deleted merely to make a test pass.
- Script runtimes expose allowlisted capabilities and enforce cancellation and resource limits. A denylist is not a sandbox; untrusted scripts receive no arbitrary reflection, filesystem, network, or service-provider access.
- JavaScript uses modern ECMAScript, no assumed browser globals, and `??` for option defaults.
- Live-value samples distinguish uninitialized, null, stale, bad-quality, and failed states and include timestamps.
- Every external source adapter passes the shared behavior contract suite.

Use `pocok-agentic-workflow` for authority, planning, validation, records, commits, and communication. No document or plan authorizes destructive Git work, publication, tags, deployment, credentials, or other external side effects beyond the user's request.
