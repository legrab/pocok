// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.DependencyInjection;
using Pocok.Modularity.Contracts;
using Pocok.Showcase.Contracts;

namespace Pocok.Showcase.Subscriptions;

public sealed class SubscriptionsShowcaseModule : IServiceModule
{
    public void ConfigureServices(IServiceCollection services, ModuleContext context)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(context);
        services.AddSingleton(new ShowcaseResourceRegistration("subscriptions", context.BaseDirectory, "Content/Locales/Subscriptions"));
        services.AddSingleton<SubscriptionsShowcaseSlice>();
        services.AddSingleton<IShowcaseSlice>(static provider => provider.GetRequiredService<SubscriptionsShowcaseSlice>());
    }
}
