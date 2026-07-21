// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.DependencyInjection;
using Pocok.Modularity.Contracts;
using Pocok.Showcase.Contracts;

namespace Pocok.Showcase.Readiness;

public sealed class ReadinessShowcaseModule : IServiceModule
{
    public void ConfigureServices(IServiceCollection services, ModuleContext context)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(context);
        services.AddSingleton(new ShowcaseResourceRegistration("readiness", context.BaseDirectory,
            "Content/Locales/Readiness"));
        services.AddSingleton<ReadinessShowcaseSlice>();
        services.AddSingleton<IShowcaseSlice>(static provider => provider.GetRequiredService<ReadinessShowcaseSlice>());
    }
}
