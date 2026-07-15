# Pocok.AppDefaults.Modularity

`Pocok.AppDefaults.Modularity` is the opinionated application-policy layer over `Pocok.Modularity`.

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddPocokModularityDefaults(
    configureDefaults: defaults =>
    {
        defaults.SharedAssemblyNames.Add(typeof(IDeviceCommunicator).Assembly.GetName().Name!);
    });
```

By default it searches `<content-root>/plugins` recursively, treats the directory as optional, and allows optional
plugin failures to remain diagnostic rather than fatal. The policy can be configured under `Pocok:Modularity` or through
the delegate. The resolved startup policy is available through `IOptions<ModularityDefaultsOptions>`.

The second delegate exposes the underlying `ModuleLoadOptions` for application contract sharing and exceptional cases.
The package does not alter the loader's startup-only and trusted-code boundaries. Applying the configurator more than
once to the same builder throws `InvalidOperationException` so conflicting plugin policies cannot be silently ignored.

This package remains non-releasable until the underlying Modularity integration matrix passes on Linux and Windows.
