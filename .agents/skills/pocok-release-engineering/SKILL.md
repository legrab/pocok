---
name: pocok-release-engineering
description: Prepare, validate, diagnose, or improve Pocok packaging and release operations. Use for eng/packages.json, package catalog tooling, MinVer/version props, package audits, local or publication-shaped feeds, NuGet dependency closure, Source Link/symbol proof, publish.yml, package/global tags, dependency-wave releases, release eligibility, or partial-release recovery. Do not use for ordinary package implementation or Showcase UI work.
---

# Pocok release engineering

Read root `AGENTS.md`, `docs/current-handoff.md`, `PUBLICATION.md`, and the relevant catalog/workflow/tool source. Read active release plans only when executing or revising them. The repository and `eng/packages.json` are current truth; documentation may describe intended behavior that is not implemented yet.

## Authority and safety

- Treat `eng/packages.json` as the authoritative package identity, dependency, tag-prefix, version-property, consumer, state, and eligibility catalog.
- Never create or push a tag, publish/deprecate a package, use credentials, change release eligibility, or deploy without explicit approval and passing evidence.
- NuGet versions are immutable. Never overwrite or assume rollback; use provenance-checked resume or a higher version after a partial release.
- Keep package-specific and global publication mutually exclusive through shared non-cancelling concurrency control.
- Never use wildcard artifact pushes or allow a cache/public feed to mask a missing internal candidate dependency.
- Keep catalog loading, graph construction, NuGet version precedence, provenance, and recovery-manifest rules in one authoritative implementation. Shell entry points may wrap it but must not maintain competing algorithms.

## Prepare an exact candidate

1. Resolve the exact tag/commit and catalog target set. Reject unknown, duplicate, retired, non-releasable, or graph-inconsistent entries.
2. Compute dependency-first closure or topological waves from actual catalog dependencies; validate those edges against project and packed NuGet metadata.
3. Generate exact release-version properties before restore so candidates cannot reference unpublished development versions.
4. Build, test, and pack once into a clean candidate directory. Record exact artifact names and hashes; do not rebuild between audit and push.
5. Run catalog/metadata tests, local-closure consumers, package-content audit, symbols/Source Link proof, public-hygiene checks, and required Linux/Windows gates.
6. Use publication-shaped restore only when required dependency versions are available on the configured public feed; otherwise use an isolated temporary feed for rehearsal.

Apply proof by catalog package kind. A deployment bundle can have a custom payload/PDB profile without weakening the DLL/XML/snupkg requirements for ordinary library packages.

## Publish safely

- Preflight the requested SemVer against every target and fail before push when an equal or higher version exists; name all conflicts and suggest one valid greater version.
- Publish dependency waves only from the immutable audited artifacts.
- After each push, wait with bounded retry until the exact dependency version is observable and cleanly restorable before advancing dependents.
- Stop on push, propagation, restore, provenance, or graph failure and report published, pending, blocked, and failed packages plus the safe recovery path.
- Keep credentials, absolute paths, and local cache details out of artifacts and summaries.

## Evidence and handoff

Distinguish static inspection, local executable proof, cross-platform CI, temporary-feed rehearsal, nuget.org availability, and actual publication. Update `docs/current-handoff.md` when evidence, eligibility, package closure, or the next release action changes. Do not present a dry run as a release.
