# Agent Plan Format

Create multi-step plans at `prompts/<topic>/plan-<topic>.prompt.md`. Plans are passive until explicitly started.

## Required sections

1. **Goal and success criteria** — the outcome, non-goals, and observable completion conditions.
2. **Architecture** — current contracts and target dependency direction with small code or tree examples.
3. **Impacted scope** — exact projects, files, consumers, tests, generated artifacts, fixtures, and package surfaces.
4. **Risks and decisions** — behavior, compatibility, security, concurrency, persistence, invalidation, identifiers, and dependencies.
5. **Steps** — independently compilable commits grouped by phase and review gates.

## Step template

```markdown
### Step N: Short outcome

- **Status**: Pending
- **Commit title**: `vibe(AREA): imperative summary`
- **Why**: concise reason
- **Files**: exact paths

Current:

```text
Relevant current signature, behavior, or tree.
```

Target:

```text
Precise desired signature, behavior, or tree.
```

Validation:

- focused checks
- affected tests
- package and consumer checks
```

End every phase with a review gate. End every plan with a follow-up review that resolves completed items and promotes genuine future work into a new passive plan.

## Planning rules

- One commit per step.
- Current and Target are always present.
- List exclusions explicitly when scope sounds broad.
- Do not hide an unresolved product decision inside implementation detail.
- Prefer the smallest dependency cone and independently usable vertical slices.
- Public packages require provenance, threat/security notes where relevant, README/sample, API review, and local-feed installation tests.
