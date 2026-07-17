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
  Models/
  Content/Locales/Example.json
  Content/Locales/Example.hu.json
  pocok.module.json
  README.md
```

The project name must match `Pocok.Showcase.*.csproj` so the discovery publisher finds it. A directory segment beginning with `_` is intentionally ignored.

## Implementation steps

1. Create a non-packable Razor project under `samples/Showcase`.
2. Reference:
   - `showcase/src/Pocok.Showcase.Contracts`;
   - `showcase/src/Pocok.Showcase.Components`;
   - `src/Modularity.Contracts`;
   - the real Pocok package being demonstrated.
3. Do not reference `Pocok.Showcase.Web`.
4. Define package-owned typed input and output records. Keep mutable editor state in the page component, not in a singleton.
5. Derive the slice from `ShowcaseSlice<TInput, TOutput>`.
6. Provide an immutable `ShowcaseSliceDescriptor` whose package id and slug match the deployment package catalog.
7. Provide immutable sample descriptors backed by factories that return fresh input instances.
8. Mark exactly one sample as the default.
9. Implement the module-owned Razor page with:
   ```csharp
   [Parameter, EditorRequired]
   public ShowcasePageContext Context { get; set; } = default!;
   ```
10. Use shared components and stable global CSS classes. Use `ShowcaseExecutionControls` for ordinary run, progress, cancellation, and disposal behavior rather than reimplementing it. Do not add runtime-dependent `.razor.css`.
11. Add invariant English and Hungarian JSON resources. Localize prose, labels, sample descriptions, tips, and result labels. Keep package ids, public API names, error codes, code, and manifest values untranslated.
12. Add ordered guide sections and compile-valid snippets based on the real current API.
13. Add slice-owned code-assist metadata only when an editor benefits from it.
14. Implement one public parameterless `IServiceModule`.
15. Register resources using `ModuleContext.BaseDirectory`, then register the typed slice and its `IShowcaseSlice` bridge.
16. Add and validate `pocok.module.json`.
17. Add default-sample, expected-failure, localization, boundary, and isolation tests.
18. Publish and smoke-test the repository. A valid slice becomes Available automatically.

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

Restore once, then run:

```bash
/usr/bin/env -u PLATFORM dotnet build samples/Showcase/Pocok.Showcase.Example/Pocok.Showcase.Example.csproj \
  --configuration Release --no-restore

bash showcase/scripts/publish-showcase.sh /tmp/pocok-showcase --no-restore
python3 showcase/scripts/smoke-showcase.py /tmp/pocok-showcase
```

PowerShell:

```powershell
Remove-Item Env:PLATFORM -ErrorAction SilentlyContinue
dotnet build samples/Showcase/Pocok.Showcase.Example/Pocok.Showcase.Example.csproj `
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

`PageComponentType` must name a public Razor component implementing `IComponent`, and the page must accept the shared `ShowcasePageContext`.

### Plugin assets are absent after publish

Keep module-owned deployable assets as ordinary JSON content with `CopyToOutputDirectory` and `CopyToPublishDirectory`. Do not rely on dynamically loaded static-web-assets manifests or scoped CSS.

### Package remains Planned

Check that:

- the project matches `samples/Showcase/**/Pocok.Showcase.*.csproj`;
- no path segment begins with `_`;
- the manifest was copied;
- package id and slug match `Content/package-catalog.json`;
- the module registered exactly one `IShowcaseSlice`;
- the final plugin inventory contains the module.

### Readiness fails

Read `/health/ready`, then inspect server logs and `/system`. Common causes are a required module load failure, duplicate metadata, missing invariant resource keys, no default sample, or strict catalog mode enabled before all slices exist.

## Final contributor checklist

- [ ] Non-packable project under `samples/Showcase`
- [ ] Contracts, Components, Modularity.Contracts, and real package references
- [ ] No Web reference
- [ ] Typed input and output
- [ ] Immutable descriptor, guide, samples, and code-assist metadata
- [ ] Fresh input factory and exactly one default sample
- [ ] Shared-context Razor page
- [ ] Invariant English and Hungarian resources
- [ ] Public parameterless module registration
- [ ] Valid manifest and shared assemblies
- [ ] Default sample and failure tests
- [ ] Discovery publish and real-process smoke pass
