# Repository-internal shared source

This directory is intentionally not a project and is never packaged as an assembly.

A source file belongs here only when all of the following are true:

- the same implementation is already needed by at least four repository projects;
- the behavior is small, deterministic, and unlikely to deserve an independent public contract;
- the BCL or an accepted dependency does not already provide it;
- the file can remain `internal` after compilation into each consuming assembly;
- the file has no package-specific domain language and no third-party dependency.

Consumers must link individual files explicitly with `Compile Include` and `Link`. Wildcards are forbidden. Packable projects must never reference a non-packaged shared assembly. A helper that develops public semantics, independent tests, configuration, or dependencies must be promoted into a reviewed package or moved back into its owning package.
