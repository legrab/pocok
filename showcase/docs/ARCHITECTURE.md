# Showcase architecture

## Goal

The Showcase is the repository-level sandbox and visualization framework for Pocok libraries. A slice should make a package understandable and directly testable through its real public API without turning the Web host into a package-specific application. Documentation-only slices are valid when runtime interaction would be artificial, but executable libraries should normally provide bounded samples and editable inputs.

## Boundaries

```text
Pocok.Showcase.Contracts
        ^
        |
Pocok.Showcase.Components
        ^
        |
Pocok.Showcase.Web

Contracts + Components + Pocok.Modularity.Contracts + real package <- each slice
```

The executable host has no reference to a concrete slice, including package-id checks. Before `builder.Build()`, it points `Pocok.Modularity` at `SHOWCASE_PLUGIN_DIR` or `<content-root>/plugins` and shares the Contracts and Components assemblies. Each trusted module registers one immutable `IShowcaseSlice`, its resource root, and any stateless helpers through `IServiceModule`. The generic `/packages/{slug}` route renders the module-owned page through `DynamicComponent`.

`ShowcaseExecutionControls` owns the common run lifecycle for slice pages: submission, private progress consumption, cancellation, result delivery, and handle disposal. Package pages own only their editor state and package-specific visualization. `ShowcaseResultPanel` accepts a preview language so C#, JavaScript, and later formats do not leak assumptions into shared UI.

## Catalog modes

Publication generates `Content/package-catalog.json` from the current non-retired entries in `eng/packages.json` and `Content/showcase-slices.json` from discovered plugin manifests. Partial mode is the normal incremental default and marks absent slices as planned. `Showcase:RequireCompleteCatalog=true` makes startup and publication reject missing current slices.

## Execution isolation

A singleton bounded channel feeds one hosted worker. Every Blazor circuit receives a scoped `IShowcaseRunClient`. Every submission owns a private completion source, private progress channel, linked cancellation token, cloned read-only cultures, per-run dependency-injection scope, bounded output, timeout, and temporary-directory factory. No mutable editor state or results are stored globally. Correlation IDs exist only for logs.

## Localization

The shell and each module register a `ShowcaseResourceRegistration`. `FileStringLocalizer` reads invariant and culture-specific JSON without file watching. Language changes set the standard ASP.NET Core culture cookie and perform a full local redirect. No process-global default culture is changed.

## Deployment

The Docker image is the contract. It exposes one HTTP port, requires no database or persistent filesystem, and contains the host plus discovered plugin directories. Interactive Server uses SignalR and WebSockets, so deployment begins with one replica. Azure Static Web Apps is intentionally unsupported because it cannot host this startup and circuit model.
