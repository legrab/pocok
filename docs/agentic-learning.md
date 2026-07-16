# Learning Pocok and its agentic setup

This guide helps collaborators understand both the repository and the way agents are expected to work inside it.

## Layer map

```text
AGENTS.md
  stable Pocok architecture, safety, code, testing, and package rules
        ↓
docs/agentic-workflow.md
  planning, autonomy, validation, records, commits, and handoff
        ↓
prompts/ and optional skills
  task-specific execution or durable planning support
        ↓
docs/current-handoff.md and sessions/
  time-sensitive state and historical evidence
```

Current code and executable evidence outrank plans and summaries when they disagree.

## Tailor explanation when useful

When experience cannot be inferred and it would materially change the explanation, ask one choice:

- **A. Advanced student or newer developer:** explain package vocabulary, build flow, and proof surfaces explicitly.
- **B. Experienced developer, new to Pocok or package engineering:** emphasize repository-specific architecture and release mechanics.
- **C. Senior .NET or package maintainer:** emphasize contracts, edge cases, dependency closure, compatibility, and evidence gaps.
- **D. Adaptive:** infer from the task and adjust without storing a level.

Do not ask routinely or repeat the question after enough context exists.

## Suggested learning path

1. Read the package identity and current package table in `README.md`.
2. Read root `AGENTS.md` for stable boundaries.
3. Read `docs/agentic-workflow.md` for collaboration behavior.
4. Trace one package from source to unit tests, sample, package catalog, pack output, smoke consumer, and publication gate.
5. Use the implementation ledger to distinguish source changes from executable validation.
6. End with a short recap of the package boundary, decisive proof, current uncertainty, and a nearby transfer.

Use at most one explain-back, trace, or prediction check unless a deliberate quiz was requested. Reading is exposure, not proof of understanding.

<details>
<summary>Example explain-back</summary>

Explain why marking Modularity as non-releasable is insufficient if experimental projects still participate in the release candidate's mandatory build and test graph.

</details>
