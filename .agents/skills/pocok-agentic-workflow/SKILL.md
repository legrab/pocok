---
name: pocok-agentic-workflow
description: Configure, explain, review, or improve Pocok's agentic documentation and collaboration flow. Use for AGENTS, prompts, plans, sessions, handoff state, approval behavior, validation policy, or teaching a collaborator how the repository harness works. Do not use as a second engineering procedure for ordinary package work.
---

# Pocok agentic workflow

Read root `AGENTS.md`. Load `docs/agentic-learning.md` only for explanation work, `docs/current-handoff.md` only for current release/package-closure state, and an active plan or session only when the task executes or revises it. The compatibility files in `docs/agentic-workflow.md` and `prompts/` are pointers to this skill and add no procedure.

## Explain

1. Lead with the layer map from `docs/agentic-learning.md`.
2. Point to stable rules, workflow behavior, task prompts, and temporary evidence.
3. Ask the optional experience A/B/C/D question only when it materially changes useful depth.
4. Use one representative package task to show how instructions resolve.
5. End with a compact recap and at most one explain-back check.

## Configure or review

1. Preserve Pocok-specific package, public-hygiene, compatibility, and validation rules.
2. Keep temporary handoff outside stable root instructions.
3. Prefer balanced autonomy, brief plans, risk-proportional validation, and lean records unless the task requires stricter gates.
4. Remove duplicated or ceremonial requirements rather than adding another layer.
5. Keep evidence labels precise and never equate a checked task with executable proof.

## Plan and hand off

- Use an in-chat plan for ordinary multi-step work. Create a durable plan only for high-risk, cross-session, multi-agent, public-contract, or release work.
- Keep durable plans limited to goal, current evidence, non-goals, dependencies, coherent slices, acceptance evidence, real decisions, and external-action gates.
- Before marking a plan implementation-ready, resolve every material design spike and replace research/choice placeholders with the selected files, contracts, failure behavior, commands, and acceptance result. Keep only genuine external prerequisites, each with a deterministic fallback and an explicit effect on completion.
- Treat plans as passive evidence and reconcile them with current source before execution.
- Create a session record only for cross-agent/environment continuity, acceptance evidence, significant deviations, or work that would be expensive to reconstruct. Use `sessions/session-template.md` for its shape.
- Commit only when explicitly requested. Use the repository commit format recorded in Git history or requested by the user; do not make commit formatting an execution dependency.

## Communication

Use friendly, concise, abstract-first language. Put optional detail in collapsible Markdown sections, but keep failed checks, release gates, and required decisions visible.
