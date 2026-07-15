// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pocok.Modularity;

namespace Pocok.AppDefaults.Modularity.Tests;

public sealed class ModularityDefaultsConfiguratorTests
{
    [Test]
    public void MissingConventionalDirectoryProducesCatalogWithoutFailingStartup()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.AddPocokModularityDefaults(defaults =>
            defaults.PluginDirectory = $"missing-{Guid.NewGuid():N}");

        using IHost host = builder.Build();
        IModuleCatalog catalog = host.Services.GetRequiredService<IModuleCatalog>();

        catalog.Modules.ShouldBeEmpty();
        catalog.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "modularity.directory-missing");
    }

    [Test]
    public void ConfigurationIsBoundBeforeDelegateOverrides()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Pocok:Modularity:PluginDirectory"] = "configured-plugins",
            ["Pocok:Modularity:SearchRecursively"] = "false"
        });

        builder.AddPocokModularityDefaults(defaults => defaults.PluginDirectory = "delegate-plugins");
        using IHost host = builder.Build();
        ModularityDefaultsOptions defaults = host.Services.GetRequiredService<ModularityDefaultsOptions>();

        defaults.PluginDirectory.ShouldBe("delegate-plugins");
        defaults.SearchRecursively.ShouldBeFalse();
    }

    [Test]
    public void DuplicateApplicationKeepsOneCatalog()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.AddPocokModularityDefaults();
        builder.AddPocokModularityDefaults();

        using IHost host = builder.Build();
        host.Services.GetServices<IModuleCatalog>().ShouldHaveSingleItem();
        host.Services.GetServices<ModularityDefaultsOptions>().ShouldHaveSingleItem();
    }
}
