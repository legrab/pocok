# Modularity design spike — resolved implementation record

**Status:** Resolved and implemented; no MVP design decision remains  
**Release Readiness owner:** `docs/plans/release-readiness.md` RR4  
**Later catalog migration:** `docs/plans/mvp-closure.md` M1

## Verified repository fit

The current implementation already follows the selected design:

- public contracts: `src/Modularity.Contracts` (`IServiceModule`, `ModuleContext`, `ModuleIdentity`);
- manifest/options/loading: `src/Modularity/Loading` (`ModuleManifest`, `ModuleLoadOptions`, `ModuleLoader`);
- observable catalog: `src/Modularity/Catalog`;
- DI entry point: `ModuleServiceCollectionExtensions.AddPocokModules(...)`;
- maintainer defaults: `src/AppDefaults.Modularity`;
- clean-room fixtures: `tests/Fixtures/Modularity.*` and `tests/Integration/Modularity.Tests`;
- real consumers: `samples/ModularCommunicator.*` and the manifest-loaded Showcase plugins.

No project reference crosses the repository boundary. The Showcase provides substantial real startup/deployment use, while package tests remain the technical release gate.

## Chosen design

Keep the BCL implementation. Each trusted plugin is one directory containing a versioned manifest, entry assembly, and private dependencies. `ModuleLoader` uses one non-collectible `AssemblyLoadContext` plus `AssemblyDependencyResolver` per plugin. Discovery is explicit, deterministic, startup-only, and completes service registration before the root provider is built.

The host supplies `Pocok.Modularity.Contracts`, Microsoft configuration/DI abstractions exposed by it, and manifest-declared application contract assemblies from the default load context. Plugin-private dependencies resolve from the plugin directory. Platform/architecture filters run before assembly loading. Required failures stop startup; optional failures remain structured catalog diagnostics according to `ModuleLoadOptions`.

This is trusted in-process extension loading, not a sandbox. MVP does not add arbitrary DLL scanning, intermediate service providers, unload/hot reload, remote acquisition, child containers, shadow copy, or module-to-module dependency orchestration.

McMaster.NETCore.Plugins is not an MVP dependency. Reconsider it only through a new post-MVP design if concrete requirements exceed the current internal load-context implementation. Implementation agents must not research or substitute it during RR4.

## Existing functionality to reuse

- `AddPocokModules(...)` and `ModuleLoadOptions` for host registration/configuration;
- `IModuleCatalog`, `ModuleDescriptor`, and `ModuleDiagnostic` for status and safe diagnostics;
- `ModularityDefaultsConfigurator` for conventional AppDefaults integration;
- fixture projects for shared identity, private dependencies, platform filtering, and failure behavior;
- `samples/ModularCommunicator/Stage-Plugin.ps1` for a deployable directory;
- Showcase `pocok.module.json` manifests and publication staging as the real consumer.

Do not create a second plugin catalog, resolver, manifest, or logging pipeline.

## Release Readiness validation

Run on Linux and Windows at the exact candidate commit:

```powershell
dotnet test tests/Unit/Modularity.Contracts.Tests/Pocok.Modularity.Contracts.Tests.csproj --configuration Release
dotnet test tests/Unit/AppDefaults.Modularity.Tests/Pocok.AppDefaults.Modularity.Tests.csproj --configuration Release
dotnet test tests/Integration/Modularity.Tests/Pocok.Modularity.Integration.Tests.csproj --configuration Release

pwsh -File samples/ModularCommunicator/Stage-Plugin.ps1
dotnet run --project samples/ModularCommunicator.Host/Pocok.ModularCommunicator.Host.csproj --configuration Release

pwsh -File tools/PackageSmoke/Invoke-PackageSmoke.ps1 -Mode LocalClosure -PackageIds Pocok.Modularity.Contracts,Pocok.Modularity,Pocok.AppDefaults.Modularity
pwsh -File tools/PublicReleaseAudit/Invoke-PublicReleaseAudit.ps1 -PackageIds Pocok.Modularity.Contracts,Pocok.Modularity,Pocok.AppDefaults.Modularity
```

Also publish the Showcase to a clean directory and require every plugin to register through the generic host without host project references:

```powershell
pwsh -File showcase/scripts/publish-showcase.ps1 -OutputPath artifacts/showcase/modularity-proof
python showcase/scripts/smoke-showcase.py artifacts/showcase/modularity-proof
```

## Acceptance and failure behavior

Pass only when fixtures prove:

- private managed dependency loading;
- shared contract identity;
- optional and required failures;
- duplicate IDs;
- malformed manifests;
- platform and architecture filtering;
- deterministic order;
- multiple `IEnumerable<TContract>` implementations;
- diagnostics without path or secret leakage;
- matching package and Showcase staging behavior.

On a loader failure, preserve the failing fixture and diagnostic. Fix the current BCL implementation while retaining the chosen public boundary.

Do not:

- fall back to default-context scanning;
- load every DLL;
- swallow `ReflectionTypeLoadException`;
- build an intermediate provider;
- change publication eligibility before all commands pass.

After the exact RR4 gate passes under the current schema-v1 catalog:

- keep API maturity `state: Experimental`;
- set `releasable: true`.

MVP Closure later migrates this to schema-v2 `publicationPolicy: PrereleaseOnly`; stable publication remains a separate compatibility decision.
