// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pocok.Licensing;

namespace Pocok.AppDefaults.Licensing;

internal sealed class LicenseEnforcementHostedService : BackgroundService
{
    private static readonly Action<ILogger, LicenseValidationCode, string, Exception?> LogStartupRejection =
        LoggerMessage.Define<LicenseValidationCode, string>(
            LogLevel.Critical,
            new EventId(1, nameof(LogStartupRejection)),
            "License enforcement rejected application startup with {Code}: {Message}");

    private static readonly Action<ILogger, LicenseValidationCode, string, Exception?> LogRuntimeRejection =
        LoggerMessage.Define<LicenseValidationCode, string>(
            LogLevel.Critical,
            new EventId(2, nameof(LogRuntimeRejection)),
            "License enforcement stopped the running application with {Code}: {Message}");

    private static readonly Action<ILogger, LicenseValidationCode, string, Exception?> LogEnforcementWarning =
        LoggerMessage.Define<LicenseValidationCode, string>(
            LogLevel.Warning,
            new EventId(3, nameof(LogEnforcementWarning)),
            "License enforcement warning {Code}: {Message}");

    private readonly ILicenseService _licenses;
    private readonly LicenseOptions _settings;
    private readonly TimeProvider _timeProvider;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<LicenseEnforcementHostedService> _logger;

    public LicenseEnforcementHostedService(
        ILicenseService licenses,
        IOptions<LicenseOptions> options,
        TimeProvider timeProvider,
        IHostApplicationLifetime lifetime,
        ILogger<LicenseEnforcementHostedService> logger)
    {
        _licenses = licenses ?? throw new ArgumentNullException(nameof(licenses));
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        LicenseValidationResult? failure = await FindFailureAsync(cancellationToken).ConfigureAwait(false);
        if (failure is null)
        {
            await base.StartAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if (_settings.FailureBehavior == LicenseFailureBehavior.Warn)
        {
            LogWarning(failure);
            await base.StartAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        Environment.ExitCode = _settings.BlockingExitCode;
        LogStartupRejection(_logger, failure.Code, failure.Message, null);
        throw new LicenseException(failure);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_settings.RevalidationInterval, _timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                LicenseValidationResult? failure = await FindFailureAsync(stoppingToken).ConfigureAwait(false);
                if (failure is null) continue;
                if (_settings.FailureBehavior == LicenseFailureBehavior.Warn)
                {
                    LogWarning(failure);
                    continue;
                }

                Environment.ExitCode = _settings.BlockingExitCode;
                LogRuntimeRejection(_logger, failure.Code, failure.Message, null);
                _lifetime.StopApplication();
                break;
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async ValueTask<LicenseValidationResult?> FindFailureAsync(CancellationToken cancellationToken)
    {
        LicenseValidationResult result = await _licenses.RefreshAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!result.IsValid) return result;

        foreach (var module in _settings.RequiredModules)
        {
            result = _licenses.Validate(module);
            if (!result.IsValid) return result;
        }

        return null;
    }

    private void LogWarning(LicenseValidationResult failure) =>
        LogEnforcementWarning(_logger, failure.Code, failure.Message, null);
}
