# Agentic workflow documentation change log

Reference source: Codebase Learning Flow v0.5.2 design review of `legrab/pocok`

Pocok source revision reviewed: `cdfb8bfaec5dcc74c62e5f6d5d401222c82dc45a`

Review date: 2026-07-16

## Retained

- strict repository and package boundaries;
- inspection of consumers and proof surfaces before public contract changes;
- risk-based validation including package smoke and release audit;
- explicit distinction between source changes and executable evidence;
- useful session and ledger records for cross-environment handoff;
- existing optional `vibe(AREA)` commit format.

## Lifted restrictions

- ordinary requested implementation no longer needs a separate explicit start;
- work is not forced into one step or one commit at a time;
- commits and phase gates are not automatically offered after every step;
- substantial work does not automatically require a session file;
- durable plan files are reserved for high-risk, long-running, or handoff-heavy work;
- current handoff moved out of stable root instructions;
- communication is shorter, friendlier, abstract-first, and uses collapsible detail where appropriate.

## Added

- a compact Pocok workflow with risk-based defaults;
- optional A/B/C/D autonomy choice at meaningful forks;
- a collaborator learning guide for the repository's agentic setup;
- an explicitly refreshed current-handoff file;
- session-record admission criteria;
- a selectively loaded skill for configuring or explaining the harness.

No existing files need deletion. Existing historical session records remain valid evidence and the two original prompt entry points remain in place as compatibility surfaces.
