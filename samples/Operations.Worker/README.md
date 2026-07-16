# Operations worker sample

A small but realistic host that combines the released package families without inventing a Pocok application framework:

- AppDefaults applies a conservative logging baseline;
- Conversion parses culture-sensitive external values and reports rejected rows;
- Readiness separates successful startup from merely starting the hosted service;
- standard Generic Host and dependency injection remain visible and replaceable.

Run from the repository root:

```pwsh
dotnet run --project samples/Operations.Worker/Pocok.Operations.Worker.csproj
```
