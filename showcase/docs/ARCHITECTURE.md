# Showcase architecture

## Goal

The Showcase is the repository-level sandbox and visualization framework for Pocok libraries. A slice should make a package understandable and directly testable through its real public API without turning the Web host into a package-specific application. Documentation-only slices are valid when runtime interaction would be artificial, but executable libraries should normally provide bounded samples and editable inputs.

## Boundaries

```text
showcase/Pocok.Showcase.slnx
  Pocok.Showcase.Contracts
          ^
          |
  Pocok.Showcase.Components
          ^
          |
  Pocok.Showcase.Web

showcase/Pocok.Showcase.Samples.slnx
  Contracts + Components + Pocok.Modularity.Contracts + real package <- each slice
```

The framework solution contains no concrete slice reference. The sample solution contains only plugin projects and plugin tests. Both remain outside `Pocok.slnx` and `Pocok.Core.slnx`, so package builds and package ownership stay independent from the Showcase.

The executable host has no reference to a concrete slice, including package-id checks. Before `builder.Build()`, it points `Pocok.Modularity` at `SHOWCASE_PLUGIN_DIR` or `<content-root>/plugins` and shares the Contracts and Components assemblies. Each trusted module registers one immutable `IShowcaseSlice`, its resource root, and any stateless helpers through `IServiceModule`. The generic `/packages/{slug}` route renders the module-owned page through `DynamicComponent`.

`ShowcaseExecutionControls` owns the common run lifecycle for slice pages: submission, private progress consumption, cancellation, result delivery, and handle disposal. Package pages own only their editor state and package-specific visualization. `ShowcaseResultPanel` accepts a preview language so C#, JavaScript, and later formats do not leak assumptions into shared UI.

The shell derives its compact sandbox navigation from the installed slice catalog, keeping Home and System as stable routes. Theme colors live in a dedicated semantic token file. When no explicit preference cookie exists, the browser selects its initial dark or warm-light theme from `prefers-color-scheme`; explicit theme choices and feature-toggle settings are persisted in first-party, one-year `SameSite=Lax` cookies.

The optional in-app console captures only allowlisted synthetic `Pocok.Showcase.Public.*` events into a bounded newest-first buffer. It does not expose framework, request, exception, script, license, or user-input logs. Operator configuration controls registration, capacity, level, and text bounds; when disabled, neither capture nor console controls are registered. Circuit-local state mirrors the browser-persisted preference that controls whether an available console is visible in the rail.


## Local plugin staging

Each sample project imports `showcase/Showcase.Plugin.targets` and declares a stable `ShowcasePluginId`. A normal project build copies its complete output to `showcase/src/Pocok.Showcase.Web/plugins/<module-id>`. This gives Rider, Visual Studio, and `dotnet run` the same manifest-based layout used by publication without adding project references from the host to the samples. Cleaning a plugin project removes its staged directory. Publication disables this local staging target and writes directly to the requested publish root.

## Catalog modes

Publication generates `Content/package-catalog.json` from the current non-retired entries in `eng/packages.json` and `Content/showcase-slices.json` from discovered plugin manifests. Partial mode is the normal incremental default and marks absent slices as planned. `Showcase:RequireCompleteCatalog=true` makes startup and publication reject missing current slices.

## Execution isolation

A singleton bounded channel feeds one hosted worker. Every Blazor circuit receives a scoped `IShowcaseRunClient`. Every submission owns a private completion source, private progress channel, linked cancellation token, cloned read-only cultures, per-run dependency-injection scope, bounded output, timeout, and temporary-directory factory. No mutable editor state or results are stored globally. Correlation IDs exist only for logs.

## Editor input state

Slice pages own committed typed input, while shared buffered editor controls own the live browser value during active typing. `ShowcaseBufferedTextArea` and `ShowcaseCodeAssistEditor` debounce parent-model updates by 500 ms, flush on blur or explicit editor actions, and accept external sample resets through a revision-keyed render. This prevents a parent render from rewriting the textarea and moving its caret after each keystroke.

Sample plugins use ordinary `onchange` binding when per-keystroke updates are unnecessary. Any control that truly needs `oninput` semantics belongs in `Pocok.Showcase.Components` and reuses the shared buffer and debouncer; plugin code does not implement independent timers or raw `oninput` callbacks.

## Localization

The shell and each module register a `ShowcaseResourceRegistration`. `FileStringLocalizer` reads invariant and culture-specific JSON without file watching. Language changes set the standard ASP.NET Core culture cookie and perform a full local redirect. No process-global default culture is changed.

## Deployment

The Docker image is the contract. It exposes one HTTP port, requires no database or persistent filesystem, and contains the host plus discovered plugin directories. Interactive Server uses SignalR and WebSockets, so deployment begins with one replica. Azure Static Web Apps is intentionally unsupported because it cannot host this startup and circuit model.
