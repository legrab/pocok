# Pocok.AppDefaults

`Pocok.AppDefaults` is a deliberately small composition contract for cross-application defaults. It is useful when
several applications should register logging, observability, modularity, or similar concerns in the same reviewed way
while the underlying implementation remains owned by standard .NET or a focused provider package.

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.ConfigureWith(new LoggingDefaultsConfigurator());
```

The package does not scan assemblies, discover configurators, construct service providers, own configuration files, or
impose a dependency graph. Order is the order written at the composition root. Concern packages must document duplicate
application behavior and override order. The Pocok logging configurators reject duplicate application because silently
ignoring a second call with different options would hide a composition error.

Use the package for policy composition, not as a new foundation dependency. Capability libraries such as Conversion and
Readiness do not reference it.
