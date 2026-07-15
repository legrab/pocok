// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pocok.Modularity;

var services = new ServiceCollection();
var module = new SmokeModule();
module.ConfigureServices(
    services,
    new ModuleContext(
        new ModuleIdentity("smoke", new Version(1, 0)),
        AppContext.BaseDirectory,
        new ConfigurationBuilder().Build()));
return services.Count == 1 ? 0 : 1;

file sealed class SmokeModule : IServiceModule
{
    public void ConfigureServices(IServiceCollection services, ModuleContext context) =>
        services.AddSingleton(context.Identity);
}
