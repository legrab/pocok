# Security model

Showcase plugins are trusted in-process assemblies built from this repository. `Pocok.Modularity` provides discovery and type identity, not a hostile-code sandbox. Do not load user-supplied assemblies.

The Conversion editor does not compile C#. It removes line comments and parses a small allowlisted grammar with an 8 KiB input limit. Target types, cultures, policy enums, literals, depth, and collection size are fixed or bounded. It never invokes arbitrary members, evaluates expressions, scans arbitrary types, or installs packages.

The Scripting editor executes user-provided JavaScript through `Pocok.Scripting`, not through browser `eval`. Every run creates a fresh Jint engine with CLR interop and dynamic string compilation disabled. The public playground exposes no bindings, modules, filesystem, network, browser APIs, service provider, or secrets. Source size, timeout, statements, recursion, memory, outer queue, and output are bounded. These are in-process safety boundaries, not operating-system isolation.

The Licensing editor builds `LicenseDocument` and `LicenseValidationContext` values in memory and calls the public `LicenseValidator` API. It does not accept license files, generate signing keys, expose private keys, persist pre-shared keys, or perform filesystem access. Password fields are masked in the browser and secrets are omitted from result previews and diagnostics.

Runtime work uses a bounded queue, one per-run scope, private result and progress channels, linked cancellation, a timeout, bounded output, and safe diagnostics. No result is retrievable through a public run identifier. Temporary content is placed under a unique system-temporary directory and deleted after the run. The application stores no durable user data.

Forwarded headers are limited to one hop. Deploy behind a platform-controlled ingress and do not expose a second untrusted proxy hop. The culture endpoint accepts only `en` and `hu`, writes an HTTP-only same-site culture cookie, and redirects only to a validated local URL.

Do not add secrets to manifests, package catalogs, examples, logs, client-visible diagnostics, or deployment files.
