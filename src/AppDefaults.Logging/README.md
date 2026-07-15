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

Configuration is read first from `Pocok:Logging`, then the optional delegate is applied. Defaults add trace and span
correlation, scope-friendly single-line console output, and no global filtering. Existing providers are preserved unless
`ClearProviders` is explicitly enabled. Applying the configurator more than once is a no-op after the first application.

Applications remain in control. Registrations and logging configuration added after this configurator run later and can
override its settings. Serilog-specific integration belongs to `Pocok.AppDefaults.Logging.Serilog`.

Example configuration:

```json
{
  "Pocok": {
    "Logging": {
      "AddSimpleConsole": true,
      "MinimumLevel": "Information",
      "CategoryMinimumLevels": {
        "Microsoft": "Warning"
      }
    }
  }
}
```
