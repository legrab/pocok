# Migrating from Pocok.Hosting to Pocok.Readiness

The package and namespace were renamed because the capability coordinates readiness lifecycle state rather than general hosting.

```diff
- <PackageReference Include="Pocok.Hosting" Version="..." />
+ <PackageReference Include="Pocok.Readiness" Version="..." />
```

```diff
- using Pocok.Hosting;
+ using Pocok.Readiness;
```

`Error` is replaced by `ReadinessFailure`. Consumers that need a consistent state observation should read `IReadinessSignal.Snapshot` rather than reading state and failure separately.
