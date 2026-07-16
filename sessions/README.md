# Session records

Existing session files are historical evidence. New session records are optional and should be created only when they materially improve handoff or reproducibility.

Use a session record when work:

- spans agents, environments, or days;
- captures a release or acceptance matrix;
- contains important decisions or deviations not owned by an ADR;
- ends with blocked executable proof that another environment must continue;
- would be expensive to reconstruct from commits and current documentation.

Do not create one for a routine edit, short investigation, or ordinary review.

## Compact format

```markdown
# Session: Topic

- Date and revision
- Objective
- Starting evidence and constraints
- Decisions or deviations
- Changes or inspected surfaces
- Validation and exact results
- Remaining uncertainty
- Next action, only when one exists
```

Keep secrets, customer data, internal endpoints, personal data, and copied task transcripts out of session files. A session record never proves success by itself; executable results must be stated explicitly.
