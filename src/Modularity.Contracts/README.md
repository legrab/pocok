# Pocok.Modularity.Contracts

`Pocok.Modularity.Contracts` is the only assembly that both a host and independently deployed plugin must share.

```csharp
public sealed class AcmeCodecModule : IServiceModule
{
    public void ConfigureServices(IServiceCollection services, ModuleContext context)
    {
        services.AddSingleton<ICodec, AcmeCodec>();
    }
}
```

Application-owned behavior contracts such as `ICodec` or `IDeviceCommunicator` should live in their own neutral contract
assemblies. The module project references those contracts plus this package, while the host references the same contract
assemblies and `Pocok.Modularity`.

The package deliberately excludes assembly discovery, manifests, load contexts, logging, filesystem policy, hot reload,
and service location. Modules register ordinary services during startup. They are trusted in-process code, not sandboxed
extensions.
