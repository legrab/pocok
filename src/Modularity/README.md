# Pocok.Modularity

`Pocok.Modularity` loads trusted, independently deployed startup plugins without adding implementation references to the
host.

```csharp
builder.Services.AddPocokModules(builder.Configuration, options =>
{
    options.AddDirectory(Path.Combine(builder.Environment.ContentRootPath, "plugins"));
    options.ShareAssemblyContaining<IDeviceCommunicator>();
});
```

Each plugin directory contains a versioned `pocok.module.json`, its entry assembly, and private dependencies:

```json
{
  "manifestVersion": 1,
  "id": "supplier.acme.device",
  "version": "1.0.0",
  "entryAssembly": "Acme.DevicePlugin.dll",
  "required": false,
  "supportedOperatingSystems": ["windows"],
  "sharedAssemblies": ["MyApplication.DeviceContracts"]
}
```

The entry assembly exposes one or more public parameterless `IServiceModule` implementations. Their registrations are
staged and copied into the host only after the complete plugin succeeds. The loader never builds an intermediate service
provider.

One non-collectible `AssemblyLoadContext` and `AssemblyDependencyResolver` pair is used per plugin. The module
contracts, the standard configuration and DI abstractions exposed by those contracts, and explicitly shared application
contract assemblies resolve from the default context; private dependencies resolve from the plugin directory. Discovery
is manifest-led and deterministic.

This is not a sandbox. Plugins execute trusted code in-process. Version 1 is startup-only and deliberately excludes hot
reload, installation, unload guarantees, child containers, and module dependency graphs.

The package remains non-releasable in `eng/packages.json` until its Linux and Windows fixture matrix passes in CI.
