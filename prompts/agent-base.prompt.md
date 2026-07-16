# Agent execution rules

This file remains the compatibility entry point for agent execution. Follow root `AGENTS.md` and `docs/agentic-workflow.md` as the canonical rules.

## Default execution

- An explicit implementation request authorizes ordinary reversible work within scope.
- Use a short in-chat plan for multi-step work; do not wait for a separate start unless the user asks for a gate.
- Ask only at meaningful architecture, compatibility, dependency, safety, or irreversible forks.
- Implement coherent reviewable slices. One slice may touch several files and does not imply one commit.
- Reconcile harmless path or signature drift from current source and disclose it. Stop when drift changes architecture, behavior, dependency direction, or scope.

## Evidence and validation

Before handoff, check in proportion to risk:

1. scope and plausible consumers;
2. observable behavior and public semantics;
3. tests, scripts, generated artifacts, reflection registrations, fakes, fixtures, and samples;
4. package boundary and dependency direction;
5. public hygiene and provenance;
6. focused checks followed by affected package, contract, packaging, and broader validation when warranted.

Report separately what changed, what was inspected, what executable checks passed, and what remains unverified.

## Plans, sessions, and follow-ups

- Use `prompts/agent-base.plan.md` only when a durable plan earns its cost.
- Create or update a session record only when the criteria in `sessions/README.md` are met.
- Keep small follow-ups in the handoff. Create a durable follow-up file only when ownership, severity, or future scheduling needs persistence.

## Commits and external actions

Do not commit, push, publish, merge, or create release tags unless explicitly requested. When a commit is requested, use the repository format documented in `docs/agentic-workflow.md` unless the user specifies another format.

## Communication

Lead with the outcome and evidence. Keep the main explanation concise and conceptual. Put optional rationale, long command output, alternatives, and exhaustive inventories in collapsible Markdown sections where supported. Keep blockers and required actions visible.
