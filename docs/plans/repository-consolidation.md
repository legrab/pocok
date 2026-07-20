# Archived repository consolidation retrospective

**Status:** Historical design and implementation evidence; not an active plan  
**Successor plans:** `release-readiness.md` followed by `mvp-closure.md`

Do not load this file to initialize ordinary work. Use the matching skill under `.agents/skills/` and current source. Git history retains the former detailed evaluation and implementation checklist.

## Why this record remains

The consolidation review established the repository's public boundary and recorded why extracted application code was reshaped into cohesive packages rather than copied wholesale. It also captured the limits of one-shot implementation before complete executable proof was available.

## Decisions carried forward

- Pocok is an independent public repository; application/business UI and product-specific workflow code remain excluded.
- The generic `Pocok.Primitives` package was retired instead of becoming a dependency sink.
- Conversion abstractions were consolidated into `Pocok.Conversion` with explicit policy and failure semantics.
- Hosting lifecycle behavior became `Pocok.Readiness` rather than a broad hosting grab bag.
- Small internal reusable code follows a controlled shared-source policy instead of an internal convenience package leaking into public dependencies.
- AppDefaults packages remain explicit composition helpers with integrations depending on abstractions.
- Modularity is a trusted startup plugin model with contracts separated from loading and host defaults.
- `eng/packages.json` is the release/package identity source and package closure must be proven from artifacts rather than assumed from solution builds.
- Source changes, static inspection, executable proof, package proof, cross-platform proof, publication, and deployment are distinct claims.

Current ADRs, package READMEs, source, tests, catalog metadata, and executable results own the detailed truth for these decisions.

## Historical outcome

The consolidation created or reshaped the repository/package structure, tests, samples, catalog, audits, smoke tooling, AppDefaults family, Modularity family, and compatibility evidence. Later work added packages and changed their evidence state, so the former phase/commit checklist and acceptance matrix are intentionally not active.

## Where active work lives

- Package implementation and alpha readiness: `.agents/skills/pocok-package-engineering/SKILL.md`
- Release graph, artifacts, tags, and NuGet: `.agents/skills/pocok-release-engineering/SKILL.md`
- Showcase host and samples: `.agents/skills/pocok-showcase-engineering/SKILL.md`
- Collaboration and evidence records: `.agents/skills/pocok-agentic-workflow/SKILL.md`
- Immediate library and local-Showcase readiness: `docs/plans/release-readiness.md`
- Later exact-artifact, NuGet-Showcase, and deployment closure: `docs/plans/mvp-closure.md`
- Deferred product ideas: `docs/plans/post-mvp-roadmap.md`
- Current time-sensitive evidence: `docs/current-handoff.md`

The repository and executable evidence outrank this retrospective.
