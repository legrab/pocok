// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.DependencyInjection;
using Pocok.Modularity.Contracts;
using Pocok.Showcase.Contracts;

namespace Pocok.Showcase.Signals;

public sealed class SignalsShowcaseModule : IServiceModule
{
    public void ConfigureServices(IServiceCollection services, ModuleContext context)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(context);
        services.AddSingleton(new ShowcaseResourceRegistration("signals", context.BaseDirectory, "Content/Locales/Signals"));
        services.AddSingleton<SignalsShowcaseSlice>();
        services.AddSingleton<IShowcaseSlice>(static provider => provider.GetRequiredService<SignalsShowcaseSlice>());
    }
}
