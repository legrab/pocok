// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Pocok.Showcase.Scripting;

/// <summary>
/// Runs scripting probes after the web host has started so worker warm-up cannot
/// delay Kestrel binding or application readiness.
/// </summary>
public sealed class ScriptingRuntimeWarmupBackgroundService(
    ScriptingRuntimeWarmupService warmup,
    IHostApplicationLifetime applicationLifetime,
    ILogger<ScriptingRuntimeWarmupBackgroundService>? logger = null) : BackgroundService
{
    private readonly IHostApplicationLifetime _applicationLifetime =
        applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));

    private readonly ILogger<ScriptingRuntimeWarmupBackgroundService>? _logger = logger;

    private readonly ScriptingRuntimeWarmupService _warmup =
        warmup ?? throw new ArgumentNullException(nameof(warmup));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await WaitForApplicationStartedAsync(stoppingToken).ConfigureAwait(false);
            await _warmup.StartAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal host shutdown while waiting for or running background warm-up.
        }
        catch (Exception exception)
        {
            _logger?.LogCritical(
                exception,
                "Background scripting runtime warm-up failed.");
            throw;
        }
    }

    private async Task WaitForApplicationStartedAsync(CancellationToken cancellationToken)
    {
        // Ensure BackgroundService.StartAsync always returns before any probe can run,
        // including test hosts where ApplicationStarted may already be signaled.
        await Task.Yield();

        CancellationToken startedToken = _applicationLifetime.ApplicationStarted;
        if (startedToken.IsCancellationRequested)
            return;

        var started = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenRegistration registration = startedToken.Register(
            static state => ((TaskCompletionSource<bool>)state!).TrySetResult(true),
            started);

        // Close the race between the initial check and callback registration.
        if (startedToken.IsCancellationRequested)
            started.TrySetResult(true);

        await started.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }
}
