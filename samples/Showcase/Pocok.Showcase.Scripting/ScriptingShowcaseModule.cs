// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pocok.Modularity.Contracts;
using Pocok.Scripting.CSharp;
using Pocok.Scripting.Execution;
using Pocok.Scripting.JavaScript;
using Pocok.Scripting.Python;
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

        var showcaseOptions =
            ScriptingShowcaseOptions.FromConfiguration(context.Configuration);
        services.AddSingleton(showcaseOptions);

        IScriptEngineAdapter[] adapters =
        [
            new JavaScriptScriptEngineAdapter(),
            showcaseOptions.TrustedEnginesEnabled
                ? new CSharpScriptEngineAdapter()
                : new UnavailableScriptEngineAdapter(
                    ScriptEngineId.CSharp,
                    "C#",
                    "scripting.engine.trusted_only",
                    "C# is available only in explicitly trusted deployments."),
            showcaseOptions.TrustedEnginesEnabled
                ? new PythonScriptEngineAdapter()
                : new UnavailableScriptEngineAdapter(
                    ScriptEngineId.Python,
                    "Python",
                    "scripting.engine.trusted_only",
                    "Python is available only in explicitly trusted deployments.")
        ];

        services.AddSingleton(new ScriptEngineRegistry(adapters));
        services.AddSingleton(static provider =>
            new ScriptRunner(provider.GetRequiredService<ScriptEngineRegistry>()));
        services.AddHostedService<ScriptingRuntimeWarmupService>();
        services.AddSingleton<ScriptingShowcaseSlice>();
        services.AddSingleton<IShowcaseSlice>(static provider =>
            provider.GetRequiredService<ScriptingShowcaseSlice>());
    }
}

public sealed class ScriptingRuntimeWarmupService(
    ScriptRunner runner,
    ScriptEngineRegistry registry,
    ScriptingShowcaseOptions showcaseOptions) : IHostedService
{
    private static readonly (ScriptEngineId EngineId, string Source)[] WarmupTargets =
    [
        (ScriptEngineId.JavaScript, "21 * 2;"),
        (ScriptEngineId.CSharp, "21 * 2"),
        (ScriptEngineId.Python, "21 * 2")
    ];

    private readonly ScriptEngineRegistry _registry =
        registry ?? throw new ArgumentNullException(nameof(registry));

    private readonly ScriptRunner _runner =
        runner ?? throw new ArgumentNullException(nameof(runner));

    private readonly ScriptingShowcaseOptions _showcaseOptions =
        showcaseOptions ?? throw new ArgumentNullException(nameof(showcaseOptions));

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach ((ScriptEngineId engineId, var source) in WarmupTargets)
        {
            if (engineId != ScriptEngineId.JavaScript && !_showcaseOptions.TrustedEnginesEnabled)
                continue;

            ScriptEngineDescriptor? descriptor = _registry.Descriptors
                .FirstOrDefault(item => item.Id == engineId);
            if (descriptor is not { IsAvailable: true })
            {
                var code = descriptor?.UnavailableCode ?? "scripting.engine.not_registered";
                throw new InvalidOperationException(
                    $"Configured scripting engine '{engineId.Value}' is unavailable ({code}).");
            }

            ScriptResult<object?> result = await _runner.ExecuteAsync(
                new ScriptExecutionRequest(
                    engineId,
                    $"showcase-warmup.{engineId.Value}",
                    source)
                {
                    ExpectResult = true
                },
                CreateWarmupOptions(engineId),
                cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
                throw new InvalidOperationException(
                    $"Scripting runtime warm-up failed for '{engineId.Value}' " +
                    $"({result.Failure!.Code}).");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private ScriptExecutionOptions CreateWarmupOptions(ScriptEngineId engineId)
    {
        return engineId == ScriptEngineId.JavaScript
            ? new ScriptExecutionOptions
            {
                Timeout = TimeSpan.FromMilliseconds(_showcaseOptions.WarmupTimeoutMilliseconds),
                MaxSourceCharacters = 64,
                MaxOutputBytes = 1_024,
                MaxStatements = 100,
                MaxRecursionDepth = 16,
                MaxMemoryBytes = 4 * 1_024 * 1_024
            }
            : new ScriptExecutionOptions
            {
                Timeout = TimeSpan.FromMilliseconds(_showcaseOptions.WarmupTimeoutMilliseconds),
                MaxSourceCharacters = 64,
                MaxOutputBytes = 1_024
            };
    }
}
