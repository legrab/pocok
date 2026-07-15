# Pocok.AppDefaults.Logging.Serilog

`Pocok.AppDefaults.Logging.Serilog` configures the accepted Serilog hosting packages rather than wrapping or replacing
Serilog.

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddPocokSerilogDefaults();
```

Logger configuration is read from the conventional top-level `Serilog` section. The separate `Pocok:Logging:Serilog`
policy section controls only integration behavior such as preserving the static logger, forwarding to other Microsoft
logging providers, and adding `LogContext` enrichment.

The package intentionally ships no sink and hard-codes no path, endpoint, retention rule, or environment-specific level.
Applications choose sinks through their own package references and Serilog configuration. This package is an alternative
provider policy and does not depend on `Pocok.AppDefaults.Logging`. A delegate can add code-based configuration after
the configuration section has been read.

Applying the configurator more than once is a no-op after the first call. Call application-specific Serilog
configuration after these defaults when order matters.
