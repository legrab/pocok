# ADR 0002: Repository and Package Shape

- Status: Accepted
- Date: 2026-07-14

## Decision

Use three independent public repository roots:

- `legrab/pocok` for small foundational `Pocok.*` packages and an internal convenience facade;
- `legrab/pocok-scripting` for embedded JavaScript infrastructure;
- `legrab/pocok-signals` for live-value integration infrastructure.

Each root has its own solution, build settings, package management, `.gitignore`, CI workflows, documentation, tests, samples, and agent harness. Cross-repository dependencies are consumed from a local package feed during development and from a versioned package feed after release. Relative project references never cross repository boundaries.

Within a repository, code remains internal until it has a cohesive contract, focused tests, a real consumer, and an independent reason to version. Avoid both one-method packages and dependency-heavy “everything common” assemblies.

## Dependency direction

```text
Pocok.Primitives ─┬─> Pocok.Conversion
                  ├─> Pocok.Hosting
                   └─> other coherent Common packages

Pocok packages ──> Pocok.Scripting packages
Pocok packages ──> Pocok.Signals packages

selected Pocok packages ──> Pocok.Foundation (internal leaf)
```

Scripting and Signals cores do not depend on each other. A future integration package may depend on both after each core is stable.
