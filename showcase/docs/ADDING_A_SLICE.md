# Add a showcase slice

A normal slice is added entirely under `samples/Showcase`. Do not add a package switch to the Web host and do not reference the Web project.

## Recommended structure

```text
samples/Showcase/Pocok.Showcase.Example/
  Pocok.Showcase.Example.csproj
  _Imports.razor
  ExampleShowcaseModule.cs
  ExampleShowcaseSlice.cs
  ExamplePage.razor
  ExamplePage.razor.cs
  ExampleEditor.razor
  ExampleEditor.razor.cs
  Models/
  Content/Locales/Example.json
  Content/Locales/Example.hu.json
  pocok.module.json
  README.md
```

The publisher discovers plugins from `pocok.module.json`, not from a hard-coded project list or project-name pattern. A directory segment beginning with `_` is intentionally ignored.

## Implementation steps

1. Create a non-packable Blazor component library under `samples/Showcase`.
2. Add `<ShowcasePluginId>pocok.showcase.example</ShowcasePluginId>` and import `showcase/Showcase.Plugin.targets`.
3. Reference:
   - `showcase/src/Pocok.Showcase.Contracts`;
   - `showcase/src/Pocok.Showcase.Components`;
   - `src/Modularity.Contracts`;
   - the real Pocok package being demonstrated.
4. Add the project to `showcase/Pocok.Showcase.Samples.slnx`. Do not add it to `showcase/Pocok.Showcase.slnx`, `Pocok.slnx`, or `Pocok.Core.slnx`.
5. Do not reference `Pocok.Showcase.Web`.
6. Define package-owned typed input and output records. Keep circuit-local editor state owned by the page and pass it to focused editor components through `Value` and `ValueChanged`; never store it in a singleton.
7. Derive the slice from `ShowcaseSlice<TInput, TOutput>`.
8. Provide an immutable `ShowcaseSliceDescriptor` whose package id and slug match the deployment package catalog.
9. Provide immutable sample descriptors backed by factories that return fresh input instances.
10. Mark exactly one sample as the default.
11. Implement the module-owned Blazor page component with:
   ```csharp
   [Parameter, EditorRequired]
   public ShowcasePageContext Context { get; set; } = default!;
   ```
12. Keep the page component as the orchestrator: sample selection, current input, result, and run status. Use `ShowcasePackageWorkbench`, focused editor components, shared controls, and stable global CSS classes. Use `ShowcaseExecutionControls` for ordinary run, progress, cancellation, and disposal behavior rather than reimplementing it. Follow the buffered-input convention below. Do not add runtime-dependent `.razor.css`.
13. Add invariant English and Hungarian JSON resources. Localize prose, labels, sample descriptions, tips, and result labels. Keep package ids, public API names, error codes, code, and manifest values untranslated.
14. Add ordered guide sections and compile-valid snippets based on the real current API.
15. Add slice-owned code-assist metadata only when an editor benefits from it.
16. Implement one public parameterless `IServiceModule`.
17. Register resources using `ModuleContext.BaseDirectory`, then register the typed slice and its `IShowcaseSlice` bridge.
18. Add and validate `pocok.module.json`.
19. Add default-sample, expected-failure, localization, boundary, and isolation tests to `Pocok.Showcase.Samples.Tests`.
20. Publish and smoke-test the repository. A valid slice becomes Available automatically.

A minimal registration module follows the existing package-owned slice pattern:

```csharp
public sealed class ExampleShowcaseModule : IServiceModule
{
    public void ConfigureServices(IServiceCollection services, ModuleContext context)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(context);

        services.AddSingleton(new ShowcaseResourceRegistration(
            "example",
            context.BaseDirectory,
            "Content/Locales/Example"));

        services.AddSingleton<ExampleShowcaseSlice>();
        services.AddSingleton<IShowcaseSlice>(
            static provider => provider.GetRequiredService<ExampleShowcaseSlice>());
    }
}
```

The slice may be singleton only when descriptors, guides, samples, and completion metadata are immutable and every sample creates a fresh input.

The project file should include the shared local-staging target:

```xml
<PropertyGroup>
  <ShowcasePluginId>pocok.showcase.example</ShowcasePluginId>
</PropertyGroup>
<Import Project="../../../showcase/Showcase.Plugin.targets" />
```

## Buffered text input

Keep the browser-owned editing value separate from the page's committed input model whenever text must be observed on every keystroke. Publishing a new immutable page model during `oninput` causes Blazor to render the value back into the control and can move the caret or selection while the user types.

- Use `ShowcaseBufferedTextArea` for ordinary multiline text and `ShowcaseCodeAssistEditor` for source code with completion suggestions.
- Let the shared control debounce parent updates. It keeps the live DOM value local, commits the latest value after 500 ms, and flushes immediately on blur or an explicit editor action.
- Use normal Blazor `onchange` binding for inputs that need updates only after editing is committed. Do not add debounce to selects, checkboxes, buttons, or other discrete controls.
- Do not use `@bind:event="oninput"` or a raw `@oninput` callback in a sample plugin. If a new single-line or specialized control genuinely needs per-keystroke behavior, add a shared control under `Pocok.Showcase.Components` that reuses `BufferedEditorValue` and `DebouncedValueCommitter<T>`.
- The page remains the owner of committed typed input. The temporary editing buffer is circuit-local UI state and must not be stored in a singleton.

`Pocok.Showcase.Samples.Tests` enforces the absence of direct `oninput` handlers in sample plugins so new slices inherit this behavior by default.

## Shared Monaco scripting editor

Use `ShowcaseMonacoEditor` only for package-owned source editors that benefit from a language mode. It is Showcase-internal and preserves the same 500 ms buffered commit contract as `ShowcaseBufferedTextArea`.

- Provide a stable language ID and increment `ResetRevision` only for an external sample or engine reset.
- Keep live editor state circuit-local and flush it before Run, engine changes, sample resets, fallback changes, and disposal.
- When a run must observe the flushed value, use `ShowcaseExecutionControls.InputResolver` rather than submitting a stale parent model.
- Supply only small package-owned completion records; do not claim semantic Roslyn or Python language-service behavior.
- Monaco assets are local. Initialization or interop failure must leave the latest committed value usable through the buffered-textarea fallback.
- Do not publish the wrapper as a reusable package during Release Readiness.
## Manifest checklist

```json
{
  "manifestVersion": 1,
  "id": "pocok.showcase.example",
  "version": "1.0.0",
  "entryAssembly": "Pocok.Showcase.Example.dll",
  "required": true,
  "sharedAssemblies": [
    "Pocok.Showcase.Contracts",
    "Pocok.Showcase.Components",
    "Microsoft.AspNetCore.Components",
    "Microsoft.AspNetCore.Components.Web"
  ],
  "metadata": {
    "packageId": "Pocok.Example",
    "slug": "example",
    "kind": "showcase-slice"
  }
}
```

Confirm:

- module id, package id, and slug are unique;
- the slug contains only ASCII letters, digits, and hyphens;
- entry assembly name matches the project output;
- Contracts and Components are shared in both the manifest and host load options;
- no secret or machine-specific path appears in the manifest.

## Verify

Restore both Showcase solutions once, then run:

```bash
/usr/bin/env -u PLATFORM dotnet build showcase/Pocok.Showcase.Samples.slnx \
  --configuration Release --no-restore

bash showcase/scripts/publish-showcase.sh /tmp/pocok-showcase --no-restore
python3 showcase/scripts/smoke-showcase.py /tmp/pocok-showcase
```

PowerShell:

```powershell
Remove-Item Env:PLATFORM -ErrorAction SilentlyContinue
dotnet build showcase/Pocok.Showcase.Samples.slnx `
  --configuration Release --no-restore

./showcase/scripts/publish-showcase.ps1 `
  -OutputPath ([System.IO.Path]::GetFullPath("./artifacts/showcase")) `
  -NoRestore
python showcase/scripts/smoke-showcase.py ./artifacts/showcase
```

Publication must produce one deterministic directory under `plugins/<module-id>/` containing the manifest, entry assembly, `.deps.json`, private dependency closure, and localized content. Inspect `Content/showcase-slices.json` in the publish root.

Enable `--require-complete` or `-RequireComplete` only after every current non-retired package has a committed slice.

## Troubleshooting

### Contract assembly loaded twice

Symptoms include a registered slice that cannot be resolved as `IShowcaseSlice`, or a page parameter type mismatch. Ensure the host calls `ShareAssemblyContaining<IShowcaseSlice>()` and shares a stable Components type. Keep both common assemblies in `sharedAssemblies`.

### Missing shared assembly declaration

If a plugin loads private copies of Contracts or Components, type identity is broken even when names match. Add the declarations to the manifest and keep the host sharing configuration.

### Duplicate id, package id, or slug

The publisher and startup catalog reject duplicates. Search every `samples/Showcase/**/pocok.module.json`; do not work around the validation.

### Missing resources

Verify the resource registration base directory is `ModuleContext.BaseDirectory`, the base name omits `.json`, invariant English exists, files are copied to publish output, and keys use valid nested JSON string values.

### Page type is invalid

`PageComponentType` must name a public Blazor component implementing `IComponent`, and the page must accept the shared `ShowcasePageContext`.

### Plugin assets are absent after publish

Keep module-owned deployable assets as ordinary JSON content with `CopyToOutputDirectory` and `CopyToPublishDirectory`. Do not rely on dynamically loaded static-web-assets manifests or scoped CSS.

### Package remains Planned

Check that:

- the project is listed in `showcase/Pocok.Showcase.Samples.slnx`;
- the project imports `showcase/Showcase.Plugin.targets` and declares `ShowcasePluginId`;
- a valid `pocok.module.json` is beside the project;
- no path segment begins with `_`;
- the manifest was copied;
- package id and slug match `Content/package-catalog.json`;
- the module registered exactly one `IShowcaseSlice`;
- the final plugin inventory contains the module.

### Readiness fails

Read `/health/ready`, then inspect server logs and `/system`. Common causes are a required module load failure, duplicate metadata, missing invariant resource keys, no default sample, or strict catalog mode enabled before all slices exist.

## Final contributor checklist

- [ ] Non-packable project under `samples/Showcase` and listed only in `Pocok.Showcase.Samples.slnx`
- [ ] Stable `ShowcasePluginId` and shared staging-target import
- [ ] Contracts, Components, Modularity.Contracts, and real package references
- [ ] No Web reference
- [ ] Typed input and output
- [ ] Immutable descriptor, guide, samples, and code-assist metadata
- [ ] Fresh input factory and exactly one default sample
- [ ] Shared-context Blazor page component with circuit-local state and focused editor components
- [ ] Per-keystroke text editing uses a shared buffered control; no direct `oninput` binding in the plugin
- [ ] Invariant English and Hungarian resources
- [ ] Public parameterless module registration
- [ ] Valid manifest and shared assemblies
- [ ] Default sample and failure tests
- [ ] Discovery publish and real-process smoke pass
