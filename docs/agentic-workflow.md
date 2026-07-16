# Pocok agentic workflow

This document governs how collaborators and agents plan, execute, validate, and hand off work. Stable package and engineering rules remain in the root `AGENTS.md`.

```text
Frame → Inspect → Decide → Act → Verify → Handoff
```

## Pocok defaults

| Area | Default | Meaning |
|---|---|---|
| Autonomy | B, balanced | proceed independently; ask at meaningful design or safety forks |
| Planning | B, brief | use an in-chat plan for multi-step work |
| Validation | B, risk-proportional | focused first; broaden for affected public and package surfaces |
| Learning and records | B, lean | concise explanation; durable records only when they earn reuse or handoff value |

A task-specific request overrides these defaults. No setting authorizes commit, push, publication, release tags, or destructive Git operations.

## Workflow

### Frame

State the outcome, non-goals, important compatibility constraints, and observable completion condition. Use repository evidence instead of asking questions whose answers are already visible.

### Inspect

Read the narrowest relevant source, tests, package metadata, generated surfaces, consumers, scripts, samples, ADRs, and current evidence. For release or consolidation work, read `docs/current-handoff.md` and the implementation ledger.

### Decide

Choose the smallest coherent route. Ask one compact A/B/C/D question only when an unresolved opinionated choice materially changes the result.

- **A. Fast:** proceed end to end; ask only for blockers or irreversible choices.
- **B. Balanced:** proceed and ask at meaningful design forks.
- **C. Reviewed:** show a brief plan before implementation and pause at major scope changes.
- **D. Gated:** pause before implementation and after major phases.

### Act

Implement one coherent, independently reviewable slice at a time. This does not require one file, one command, or one commit per slice. Keep unrelated cleanup out of scope.

### Verify

Start with the closest proof surface. Broaden according to risk:

- internal implementation: focused unit and behavior tests;
- public contract: affected consumers, API snapshots, package metadata, and samples;
- packaging or release: catalog, closure, local-feed smoke, audit, pack, and required platform matrix;
- Modularity: explicit Linux and Windows fixture proof before release eligibility changes.

Keep evidence language precise:

- change applied;
- static inspection completed;
- focused executable checks passed;
- broad or matrix validation passed;
- verification unavailable or incomplete.

### Handoff

Lead with outcome and validation. Include remaining uncertainty, material deviations, and the next concrete action only when one remains.

Use a session record only when work spans agents or environments, captures an acceptance run, contains a significant decision trail, or would otherwise be expensive to reconstruct. See `sessions/README.md`.

## Planning

- Small work needs no formal plan.
- Ordinary multi-step work uses a brief in-chat plan.
- Use `prompts/agent-base.plan.md` for durable high-risk, cross-session, or release work.
- A plan is passive evidence, not authority over current source.
- Review gates are for real design decisions, safety boundaries, external actions, or configured preference, not every phase.

## Commits

Commit only when explicitly requested. A commit may contain one coherent validated slice rather than exactly one plan step.

When the existing Pocok format is requested, use:

```text
vibe(AREA): imperative summary

AI-generated via <actual model name>.
- concise reason or behavior change
- validation performed
```

Prompt-only planning commits may use `prompt(TOPIC): imperative summary`.

## Communication

Use friendly, concise, abstract-first explanations. Show the main mechanism and evidence before file-level detail. In Markdown, use `<details>` for long command matrices, exhaustive file inventories, alternative routes, and secondary rationale. Never collapse a blocker, failed validation, release gate, or required decision.
