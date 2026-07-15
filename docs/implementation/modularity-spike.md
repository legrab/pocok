# Modularity implementation spike

## Inputs inspected

- The origin seeding registrar searches `AppDomain.CurrentDomain.GetAssemblies()` and requires hard references or side effects to make implementations visible.
- The origin generic type helper scans all loaded assemblies and partially handles `ReflectionTypeLoadException`, but has no dependency-resolution or deployment model.
- The origin seeding registrar builds an intermediate `ServiceProvider` to discover registration contributors, which risks duplicate singleton instances and hidden disposal ownership.
- Current .NET exposes `AssemblyLoadContext` and `AssemblyDependencyResolver` for isolated managed and unmanaged dependency resolution.
- McMaster.NETCore.Plugins offers a broader wrapper around the same problem and remains a viable substitution if the required surface expands.

No proprietary implementation is copied. The origin is used only as a requirements and failure-pattern inventory.

## Chosen first slice

Implement one manifest-led, non-collectible context per plugin with explicit shared assemblies and startup-only DI registration. Keep the load context internal. Keep manifests, diagnostics, options, and the module catalog public because applications need to configure and inspect them.

## Stop conditions

Do not publish the package if implementation requires any of the following to pass realistic fixtures:

- default-context assembly scanning;
- an intermediate service provider;
- swallowing loader exceptions without diagnostics;
- loading every DLL in a directory;
- treating untrusted code as isolated merely because it uses an `AssemblyLoadContext`;
- exposing `Assembly`, `Type`, or `AssemblyLoadContext` as the primary application API.
