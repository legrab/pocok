// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.DependencyInjection;
using Pocok.Modularity.Contracts;
using Pocok.Scripting.Execution;
using Pocok.Showcase.Contracts;

namespace Pocok.Showcase.Scripting;

public sealed class ScriptingShowcaseModule : IServiceModule
{
    public void ConfigureServices(IServiceCollection services, ModuleContext context)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(context);

        services.AddSingleton(new ShowcaseResourceRegistration(
            "scripting",
            context.BaseDirectory,
            "Content/Locales/Scripting"));
        services.AddSingleton(new ScriptRunner());
        services.AddSingleton<ScriptingShowcaseSlice>();
        services.AddSingleton<IShowcaseSlice>(static provider => provider.GetRequiredService<ScriptingShowcaseSlice>());
    }
}
