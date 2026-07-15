# ADR 0008: Trusted startup modularity with isolated BCL load contexts

- Status: Accepted for implementation, release gated
- Date: 2026-07-15

## Context

Several applications need independently deployed implementations of application-owned contracts such as codecs, supplier device interfaces, or external communicators. Some implementations can be operating-system specific. The host must remain free of implementation references and should consume ordinary `IEnumerable<TContract>` services after startup registration.

The origin repository demonstrates the need but not a reusable solution. It discovers only assemblies already loaded into the default context, relies on naming conventions, and in one registration path builds an intermediate service provider. Those practices make optional deployments unreliable and can create duplicate singleton graphs.

## Decision

`Pocok.Modularity` provides trusted, startup-only plugin discovery. A plugin is one directory containing a manifest, one entry assembly, and its private dependencies. A module is an `IServiceModule` implementation inside that plugin.

Version 1 uses the standard `AssemblyLoadContext` and `AssemblyDependencyResolver` APIs directly. The implementation is intentionally smaller than a general plugin framework:

- one non-collectible load context per plugin;
- explicit manifest discovery rather than scanning every DLL;
- explicit shared-assembly names for `Pocok.Modularity.Contracts` and application contract assemblies;
- private dependency resolution from the plugin output directory;
- deterministic discovery and module ordering;
- platform and process-architecture filtering before assembly loading;
- ordinary root `IServiceCollection` registration before the host is built;
- structured catalog and diagnostics retained in DI.

McMaster.NETCore.Plugins remains the first replacement candidate if the BCL implementation grows beyond this boundary. It is not added initially because the required behavior is narrow, the release remains gated by fixture tests, and owning one small load-context class avoids exposing another abstraction in the public API.

## Explicit exclusions

Version 1 does not provide:

- hot reload or runtime installation;
- unload guarantees or collectible contexts;
- child containers or per-plugin service providers;
- module-to-module dependencies;
- arbitrary assembly scanning;
- a security sandbox;
- remote package acquisition;
- shadow copying;
- automatic native-library compatibility guarantees.

Plugins are fully trusted in-process code. Untrusted extensions require a separate process or stronger operating-system boundary.

## Shared contract rule

The host and plugin must resolve shared contract assemblies from the default load context. The mandatory shared set includes `Pocok.Modularity.Contracts` plus the Microsoft configuration and DI abstraction assemblies exposed by its public signatures. A plugin must not deploy private copies of application behavior contracts with incompatible versions. Manifest-declared shared assembly names extend that mandatory set. The loader reports contract resolution failures before module registration where possible.

## Release gate

All Modularity package catalog entries remain non-releasable until clean-room fixtures pass on Linux and Windows for:

- private managed dependency loading;
- shared contract identity;
- optional and required failures;
- duplicate IDs;
- malformed manifests;
- incompatible platform and architecture filtering;
- multiple implementations consumed through `IEnumerable<TContract>`;
- actionable diagnostics without leaking secrets;
- package smoke restore from the complete local closure.

## Consequences

The host can add supplier implementations without rebuilding its own project, while application code remains coupled only to behavior contracts. The project owns a small amount of reflection and load-context code, so its tests and release gate are stricter than those of the simple configurator packages.
