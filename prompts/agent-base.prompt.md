# Agent Execution Rules

## One step at a time

- Do not implement a passive plan until the engineer explicitly starts it.
- Execute one plan step at a time.
- When a step is complete and validated, summarize it and offer the prescribed commit.
- Commit only after approval. After a phase, stop for its review gate.

## Plan freshness

The code is the current truth. If a plan differs:

- update harmless path, name, or signature drift in the same step and disclose it;
- stop for approval when the required architecture, scope, behavior, or dependency direction changes.

## Session record

Create or update `sessions/YYYY-MM-DD-<topic>.md` during substantial work. Record:

- objective and approved plan step;
- starting state and relevant constraints;
- decisions and deviations;
- files changed;
- validation performed and results;
- open follow-ups and the exact next step.

Do not place secrets, customer data, internal endpoints, personal data, or copied task history in a session file.

## Commit format

Use:

```text
vibe(AREA): imperative summary

AI-generated via <actual model name>.
- concise reason or behavior change
- validation performed
```

Prompt-only planning commits use `prompt(TOPIC): imperative summary`.

## Required validation

Before offering a commit, verify:

1. **Scope** — every planned file and plausible consumer was inspected; unrelated changes are absent.
2. **Behavior** — implementation matches the approved contract, including errors, cancellation, concurrency, null, time, culture, and lifecycle semantics where relevant.
3. **Consumers** — runtime call sites, tests, scripts, generated artifacts, reflection registrations, fakes, fixtures, and package samples remain consistent.
4. **Package boundary** — dependency direction remains valid and no project reference escapes the repository root.
5. **Public hygiene** — no company, product, customer, historical, operational, credential, or sensitive content appears in source or produced packages.
6. **Verification** — run focused tests, affected package tests, contract tests, packaging smoke, and broader validation in proportion to risk.

## Follow-ups

Record deferred risks in `prompts/<topic>/followups-<topic>.md` with severity, finding, impact, proposal, and reason for deferral. Resolve or promote every follow-up in the final plan step.
