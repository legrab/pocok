// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.DependencyInjection;
using Pocok.Modularity.Contracts;
using Pocok.Showcase.Contracts;

namespace Pocok.Showcase.Localization;

public sealed class LocalizationShowcaseModule : IServiceModule
{
    public void ConfigureServices(IServiceCollection services, ModuleContext context)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(context);
        services.AddSingleton(new ShowcaseResourceRegistration("localization", context.BaseDirectory,
            "Content/Locales/Localization"));
        services.AddSingleton<LocalizationShowcaseSlice>();
        services.AddSingleton<IShowcaseSlice>(static provider =>
            provider.GetRequiredService<LocalizationShowcaseSlice>());
    }
}
