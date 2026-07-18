---
name: pocok-showcase-engineering
description: Implement, diagnose, review, or extend the Pocok Showcase host, shared components, sandbox plugins, and Showcase publication/smoke tooling. Use for files under showcase/ or samples/Showcase/, package routes, manifests, plugin discovery/staging, sandbox layouts and editors, localization, themes, logging UI, sample execution, composition modes, Docker/Render behavior, or adding a new Showcase slice. Do not use for ordinary library internals or repository release orchestration.
---

# Pocok Showcase engineering

Read root `AGENTS.md`, `showcase/docs/ARCHITECTURE.md`, and the affected host/component/plugin/tests. Read `showcase/docs/ADDING_A_SLICE.md` when adding or restructuring a slice. Read active Showcase plans only when executing or revising planned work; current source and published-host behavior remain authoritative.

## Preserve the architecture

- Keep `Pocok.Showcase.Web` package-agnostic. It must not reference concrete sample projects or switch on package IDs.
- Put reusable UI and execution behavior in `Pocok.Showcase.Components`; keep package-specific input, output, samples, guides, and visualization in the plugin under `samples/Showcase`.
- A plugin references Contracts, Components, Modularity.Contracts, and the real demonstrated package, never the Web project.
- Preserve manifest-based discovery, shared assembly identity, isolated plugin directories, and deterministic staging/publication.
- Resolve and extract package-backed plugins during publication, never in the running host. Require exact versions, a validated host API version, safe relative paths/hashes, and no host-shared assembly copies in a deployment bundle.
- Keep module descriptors/guides immutable, sample factories fresh, and editor/result state circuit-local.

## Build real samples

1. Exercise the package's current public API with bounded deterministic synthetic input; avoid static output that can drift from behavior.
2. Keep explanations concise: what is shown, why it happened, and one important boundary.
3. Use `ShowcaseExecutionControls` for run/progress/cancellation/disposal and shared result/guide components before adding new infrastructure.
4. Use `ShowcaseBufferedTextArea` or `ShowcaseCodeAssistEditor` for per-keystroke text. Do not add raw plugin `oninput` handlers or independent debounce timers; flush buffered state before explicit actions.
5. Localize prose, labels, sample descriptions, tips, and result labels in invariant English and Hungarian. Keep code, package IDs, API names, error codes, and manifest values untranslated.
6. Use stable global CSS classes and shared theme tokens. Do not add runtime-dependent scoped CSS or package-specific layout hacks when a shared component owns the behavior.
7. Bind string-valued component parameters as C# explicitly (`Value="@Value.SourceValue"`) or with `@bind-Value`; `Value="Value.SourceValue"` is a literal string. In the sample plugin Razor toolchain, bind native controls with explicit `value`/`checked` plus typed `@onchange`; native `@bind:get`/`@bind:set` can survive into published HTML as inert literal attributes.
8. On every sample selection, replace the typed input and advance a unique render/reset revision so selecting the same sample also resets native and buffered fields. Prove both displayed sample values and edited-input execution.

## Respect runtime boundaries

- Treat plugins as trusted startup modules, not as a sandbox for untrusted assemblies or scripts.
- Preserve bounded output, cancellation, timeout, private progress/result delivery, scoped run services, and disposal.
- Do not store mutable editor/results globally or expose arbitrary process logs, paths, secrets, license material, or user-authored operational data.
- Showcase success is integration acceptance, not code-level package assurance.

## Verify

Run the narrowest component/plugin tests first, then validate both Showcase solutions as affected. For a slice or staging change, publish to a clean directory and run the real-process smoke script. Inspect the generated plugin manifest, dependency closure, localized content, and catalog. Use automated browser proof for caret/focus, navigation, resize, reconnect, themes, accessibility, or responsive layout when those behaviors change; do not leave interaction correctness as a manual-only check. Add Docker and source-mode proof when publication/composition changes.

Report source changes separately from test, published-host, browser, Docker, and remote-deployment evidence. Do not publish packages, change Render, commit, push, or create tags without explicit approval.
