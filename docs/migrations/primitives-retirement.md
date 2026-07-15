# Migrating from Pocok.Primitives

`Pocok.Primitives` is retired. It was too small and generic to justify forcing a shared package dependency across unrelated capabilities.

There is no direct replacement package.

- Conversion callers should use `ConversionResult<T>` and `ConversionFailure` from `Pocok.Conversion`.
- Readiness callers should use `ReadinessFailure` and `ReadinessSnapshot` from `Pocok.Readiness`.
- Application code needing a general result type should use an application-local type or an established result library selected for that application's requirements.

The retired package remains available at its published version for existing consumers, but receives no new features.

## Maintainer action on nuget.org

The owner should mark the existing package version as deprecated and select no alternate package. The deprecation message should point here and explain that the package boundary was intentionally removed.
