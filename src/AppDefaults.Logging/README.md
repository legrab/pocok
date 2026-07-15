# Pocok.AppDefaults.Logging

`Pocok.AppDefaults.Logging` applies a small, repeatable baseline to `Microsoft.Extensions.Logging` without replacing the
framework or owning any sink.

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddPocokLoggingDefaults(options =>
{
    options.CategoryMinimumLevels["Microsoft"] = LogLevel.Warning;
});
```

Configuration is read first from `Pocok:Logging`, then the optional delegate is applied. The resolved startup policy is
available through `IOptions<LoggingDefaultsOptions>`. Defaults add trace and span correlation and no global filtering.
Existing providers are preserved unless `ClearProviders` is explicitly enabled.

Simple-console registration is disabled by default because the standard application host already registers logging
providers. Enable `AddSimpleConsole` only when the composition root deliberately owns that provider, commonly together
with `ClearProviders`. The package never chooses a file, network endpoint, or third-party sink.

Applying the configurator more than once to the same builder throws `InvalidOperationException`. This makes conflicting
composition visible instead of silently ignoring the later options. Application registrations made after these defaults
remain authoritative where the standard logging builder uses last-registration or ordered-filter semantics.

Serilog-specific integration belongs to `Pocok.AppDefaults.Logging.Serilog`.

Example configuration:

```json
{
  "Pocok": {
    "Logging": {
      "AddSimpleConsole": false,
      "MinimumLevel": "Information",
      "CategoryMinimumLevels": {
        "Microsoft": "Warning"
      }
    }
  }
}
```
