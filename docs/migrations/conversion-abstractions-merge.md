# Migrating from Pocok.Conversion.Abstractions

The separate abstractions package was merged into `Pocok.Conversion`. The public namespace remains `Pocok.Conversion`.

Replace the package reference:

```xml
<PackageReference Include="Pocok.Conversion" Version="..." />
```

No independent implementation ecosystem justified a second package and release lifecycle. The converter interface, policies, context, failure types, and implementation now version together.
