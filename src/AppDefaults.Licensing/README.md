# Pocok.AppDefaults.Licensing

Configuration-driven host policy for `Pocok.Licensing`.

```csharp
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.AddPocokLicensingDefaults(options =>
{
    options.RequiredModules = ["Core"];
    options.FailureBehavior = LicenseFailureBehavior.Block;
});
```

Configuration is read from `Pocok:Licensing`, then the optional delegate is applied. Relative license and public-key paths
are resolved against the host content root. Applying the defaults twice throws.

At startup, the package asynchronously reloads and verifies the license. `Block` assigns the configured exit code and throws
`LicenseException`, preventing later hosted services from starting. `Warn` logs and continues. A background service then
reloads and validates at `RevalidationInterval`; blocking failures request host shutdown. Blocking exit codes are limited
to the portable range from 1 through 255.

The options object is resolved once and available through `IOptions<LicenseOptions>`. File reads honor host cancellation.
Runtime validation is provided by the thread-safe singleton `ILicenseService`. This package does not add interception,
reflection scanning, endpoint middleware, process termination, or service-provider corruption. Applications retain
operation-level enforcement through `Demand` and `DemandFor`.

See [`docs/licensing.md`](https://github.com/legrab/pocok/blob/main/docs/licensing.md) for configuration, security guidance, and the release gate.
