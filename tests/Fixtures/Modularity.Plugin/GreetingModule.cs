// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.DependencyInjection;
using Pocok.Modularity.FixtureDependency;
using Pocok.Modularity.Fixtures;

namespace Pocok.Modularity.FixturePlugin;

public sealed class GreetingModule : IServiceModule
{
    public void ConfigureServices(IServiceCollection services, ModuleContext context)
    {
        if (string.Equals(context.Configuration["ThrowDuringRegistration"], "true", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Fixture registration failure.");

        var prefix = context.Configuration["Prefix"] ?? "Hello";
        services.AddSingleton<GreetingSuffix>();
        services.AddSingleton<IGreetingProvider>(provider =>
            new GreetingProvider(prefix, provider.GetRequiredService<GreetingSuffix>()));
    }

    private sealed class GreetingProvider(string prefix, GreetingSuffix suffix) : IGreetingProvider
    {
        public string Greet(string name)
        {
            return $"{prefix} {name}{suffix}";
        }
    }
}
