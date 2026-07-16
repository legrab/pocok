# Agent plan guidance

Use a durable plan at `prompts/<topic>/plan-<topic>.prompt.md` only when work is high-risk, spans sessions or agents, changes public package contracts, affects release behavior, or is too large to coordinate safely in chat.

A normal multi-step task needs only a brief in-chat plan.

## Durable plan content

Include the smallest useful set:

1. **Goal and success criteria**
2. **Current evidence and uncertainty**
3. **Target behavior and non-goals**
4. **Impacted packages, consumers, tests, generated artifacts, and package surfaces**
5. **Material risks and unresolved decisions**
6. **Coherent implementation slices and validation**
7. **Handoff or review gates only where a real decision or external action exists**

## Optional slice template

```markdown
### Slice N: Short outcome

- Why this slice exists
- Relevant files and consumers
- Behavior or contract preserved or introduced
- Validation required
- Decision gate, only when needed
```

Current and target examples are useful when they clarify a contract, but are not mandatory boilerplate for every slice.

## Rules

- Current source and executable evidence outrank the plan.
- Keep the dependency cone small and slices independently reviewable.
- Do not hide product or architecture decisions inside implementation detail.
- Do not require one commit per slice. Commit boundaries are chosen only when a commit is requested.
- Public packages still require provenance, compatibility and security notes where relevant, README and sample review, API proof, package inspection, and local-feed installation tests.
- End with evidence status and genuine follow-ups, not a ceremonial phase gate.
