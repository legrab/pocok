// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

        var runtimeGates = new List<RuntimeGatedScriptEngineAdapter>();
        IScriptEngineAdapter[] adapters =
        [
            new JavaScriptScriptEngineAdapter(),
            CreateTrustedAdapter(
                showcaseOptions.TrustedEnginesEnabled,
                static () => new CSharpScriptEngineAdapter(),
                ScriptEngineId.CSharp,
                "C#",
                runtimeGates),
            CreateTrustedAdapter(
                showcaseOptions.TrustedEnginesEnabled,
                static () => new PythonScriptEngineAdapter(),
                ScriptEngineId.Python,
                "Python",
                runtimeGates)
        ];

        services.AddSingleton(new ScriptingRuntimeAvailability(runtimeGates));
        services.AddSingleton(new ScriptEngineRegistry(adapters));
        services.AddSingleton(static provider =>
            new ScriptRunner(provider.GetRequiredService<ScriptEngineRegistry>()));
        services.AddSingleton<ScriptingRuntimeWarmupService>();
        services.AddHostedService<ScriptingRuntimeWarmupBackgroundService>();
        services.AddSingleton<ScriptingShowcaseSlice>();
        services.AddSingleton<IShowcaseSlice>(static provider =>
            provider.GetRequiredService<ScriptingShowcaseSlice>());
    }

    private static IScriptEngineAdapter CreateTrustedAdapter(
        bool enabled,
        Func<IScriptEngineAdapter> factory,
        ScriptEngineId engineId,
        string language,
        List<RuntimeGatedScriptEngineAdapter> runtimeGates)
    {
        if (!enabled)
        {
            return new UnavailableScriptEngineAdapter(
                engineId,
                language,
                "scripting.engine.trusted_only",
                $"{language} is available only in explicitly trusted deployments.");
        }

        var gate = new RuntimeGatedScriptEngineAdapter(factory());
        runtimeGates.Add(gate);
        return gate;
    }
}

public sealed class ScriptingRuntimeWarmupService(
    ScriptRunner runner,
    ScriptEngineRegistry registry,
    ScriptingShowcaseOptions showcaseOptions,
    ScriptingRuntimeAvailability? runtimeAvailability = null,
    ILogger<ScriptingRuntimeWarmupService>? logger = null) : IHostedService
{
    private static readonly (ScriptEngineId EngineId, string Source)[] WarmupTargets =
    [
        (ScriptEngineId.JavaScript, "21 * 2;"),
        (ScriptEngineId.CSharp, "21 * 2"),
        (ScriptEngineId.Python, "21 * 2")
    ];

    private readonly ILogger<ScriptingRuntimeWarmupService>? _logger = logger;

    private readonly ScriptEngineRegistry _registry =
        registry ?? throw new ArgumentNullException(nameof(registry));

    private readonly ScriptRunner _runner =
        runner ?? throw new ArgumentNullException(nameof(runner));

    private readonly ScriptingRuntimeAvailability? _runtimeAvailability = runtimeAvailability;

    private readonly ScriptingShowcaseOptions _showcaseOptions =
        showcaseOptions ?? throw new ArgumentNullException(nameof(showcaseOptions));

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach ((ScriptEngineId engineId, var source) in WarmupTargets)
        {
            bool trustedEngine = engineId != ScriptEngineId.JavaScript;
            if (trustedEngine && !_showcaseOptions.TrustedEnginesEnabled)
                continue;

            ScriptEngineDescriptor? descriptor = _registry.Descriptors
                .FirstOrDefault(item => item.Id == engineId);
            if (descriptor is not { IsAvailable: true })
            {
                var code = descriptor?.UnavailableCode ?? "scripting.engine.not_registered";
                var message = descriptor?.UnavailableMessage ??
                              $"Configured scripting engine '{engineId.Value}' is unavailable.";

                if (TryDegradeTrustedEngine(engineId, code, message))
                    continue;

                throw new InvalidOperationException(
                    $"Configured scripting engine '{engineId.Value}' is unavailable ({code}).");
            }

            ScriptResult<object?> result;
            try
            {
                result = await _runner.ExecuteAsync(
                    new ScriptExecutionRequest(
                        engineId,
                        $"showcase-warmup.{engineId.Value}",
                        source)
                    {
                        ExpectResult = true
                    },
                    CreateWarmupOptions(engineId),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                const string code = "scripting.warmup.failed";
                if (TryDegradeTrustedEngine(
                        engineId,
                        code,
                        $"The {descriptor.Language} engine was disabled because its startup probe failed.",
                        exception))
                {
                    continue;
                }

                throw new InvalidOperationException(
                    $"Scripting runtime warm-up failed for '{engineId.Value}' ({code}).",
                    exception);
            }

            if (result.IsSuccess)
                continue;

            string failureCode = result.Failure?.Code ?? "scripting.warmup.failed";
            if (TryDegradeTrustedEngine(
                    engineId,
                    failureCode,
                    $"The {descriptor.Language} engine was disabled because its startup probe failed " +
                    $"({failureCode})."))
            {
                continue;
            }

            throw new InvalidOperationException(
                $"Scripting runtime warm-up failed for '{engineId.Value}' ({failureCode}).");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private bool TryDegradeTrustedEngine(
        ScriptEngineId engineId,
        string code,
        string message,
        Exception? exception = null)
    {
        if (engineId == ScriptEngineId.JavaScript ||
            _showcaseOptions.RequireTrustedEnginesAvailable)
        {
            return false;
        }

        if (_runtimeAvailability is null ||
            !_runtimeAvailability.TryDisable(engineId, code, message))
        {
            throw new InvalidOperationException(
                $"The optional scripting engine '{engineId.Value}' failed startup, " +
                "but no runtime availability gate was registered.",
                exception);
        }

        if (exception is null)
        {
            _logger?.LogWarning(
                "Disabled optional scripting engine {EngineId} after startup probe failure {FailureCode}.",
                engineId.Value,
                code);
        }
        else
        {
            _logger?.LogWarning(
                exception,
                "Disabled optional scripting engine {EngineId} after an unexpected startup probe failure {FailureCode}.",
                engineId.Value,
                code);
        }

        return true;
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
