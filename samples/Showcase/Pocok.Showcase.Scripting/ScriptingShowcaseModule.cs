// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        services.AddHostedService<ScriptingRuntimeWarmupService>();
        services.AddSingleton<ScriptingShowcaseSlice>();
        services.AddSingleton<IShowcaseSlice>(static provider => provider.GetRequiredService<ScriptingShowcaseSlice>());
    }
}

public sealed class ScriptingRuntimeWarmupService(ScriptRunner runner) : IHostedService
{
    private readonly ScriptRunner _runner = runner ?? throw new ArgumentNullException(nameof(runner));

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ScriptResult<object?> result = await _runner.ExecuteAsync(
            new ScriptExecutionRequest("showcase-warmup", "0;") { ExpectResult = true },
            new ScriptExecutionOptions
            {
                Timeout = TimeSpan.FromSeconds(10),
                MaxStatements = 10,
                MaxRecursionDepth = 8,
                MaxScriptLength = 16,
                MaxMemoryBytes = 4 * 1024 * 1024
            },
            cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
            throw new InvalidOperationException($"The scripting runtime warm-up failed: {result.Failure!.Code}.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
