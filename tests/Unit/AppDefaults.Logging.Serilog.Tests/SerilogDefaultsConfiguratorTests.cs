// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Pocok.AppDefaults.Logging.Serilog.Tests;

public sealed class SerilogDefaultsConfiguratorTests
{
    [Test]
    public void PolicyConfigurationIsBoundBeforeDelegateOverrides()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Pocok:Logging:Serilog:PreserveStaticLogger"] = "true",
            ["Pocok:Logging:Serilog:WriteToProviders"] = "false"
        });

        builder.AddPocokSerilogDefaults(options => options.WriteToProviders = true);
        using IHost host = builder.Build();
        SerilogDefaultsOptions options = host.Services.GetRequiredService<SerilogDefaultsOptions>();

        options.PreserveStaticLogger.ShouldBeTrue();
        options.WriteToProviders.ShouldBeTrue();
        host.Services.GetRequiredService<ILogger>().ShouldNotBeNull();
    }

    [Test]
    public void DuplicateApplicationKeepsFirstPolicy()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.AddPocokSerilogDefaults(options => options.EnrichFromLogContext = false);
        builder.AddPocokSerilogDefaults(options => options.EnrichFromLogContext = true);

        using IHost host = builder.Build();
        host.Services.GetServices<SerilogDefaultsOptions>().ShouldHaveSingleItem()
            .EnrichFromLogContext.ShouldBeFalse();
    }

    [Test]
    public void CodeConfigurationRunsAfterConfigurationBinding()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        var called = false;

        builder.AddPocokSerilogDefaults(configureLogger: configuration =>
        {
            called = true;
            configuration.MinimumLevel.Verbose();
        });

        using IHost host = builder.Build();
        host.Services.GetRequiredService<ILogger>().ShouldNotBeNull();
        called.ShouldBeTrue();
    }
}
