// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.DependencyInjection;
using Pocok.Modularity.Contracts;
using Pocok.Showcase.Contracts;

namespace Pocok.Showcase.AppDefaults.Logging;

public sealed class LoggingShowcaseModule : IServiceModule
{
    public void ConfigureServices(IServiceCollection services, ModuleContext context)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(context);
        services.AddSingleton(new ShowcaseResourceRegistration("app-defaults-logging", context.BaseDirectory, "Content/Locales/Logging"));
        services.AddSingleton<LoggingShowcaseSlice>();
        services.AddSingleton<IShowcaseSlice>(static provider => provider.GetRequiredService<LoggingShowcaseSlice>());
    }
}
