// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.DependencyInjection;
using Pocok.Modularity.Contracts;
using Pocok.Showcase.Contracts;

namespace Pocok.Showcase.BackgroundWork;

public sealed class BackgroundWorkShowcaseModule : IServiceModule
{
    public void ConfigureServices(IServiceCollection services, ModuleContext context)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(context);
        services.AddSingleton(new ShowcaseResourceRegistration("background-work", context.BaseDirectory,
            "Content/Locales/BackgroundWork"));
        services.AddSingleton<BackgroundWorkShowcaseSlice>();
        services.AddSingleton<IShowcaseSlice>(static provider =>
            provider.GetRequiredService<BackgroundWorkShowcaseSlice>());
    }
}
