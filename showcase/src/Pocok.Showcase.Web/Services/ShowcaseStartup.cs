// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Microsoft.Extensions.Options;
using Pocok.Modularity.Catalog;
using Pocok.Modularity.Loading;
using Pocok.Readiness;
using Pocok.Showcase.Contracts;

namespace Pocok.Showcase.Web.Services;

public sealed class ShowcaseStartupService : IHostedService
{
    private static readonly Action<ILogger, int, Exception?> LogReady = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(1, nameof(LogReady)),
        "Pocok Showcase is ready with {SliceCount} installed slice(s).");
    private static readonly Action<ILogger, Exception?> LogStartupFailure = LoggerMessage.Define(
        LogLevel.Critical,
        new EventId(2, nameof(LogStartupFailure)),
        "Pocok Showcase startup validation failed.");
    private readonly ReadinessSource _readiness;
    private readonly ShowcaseRunnerState _runner;
    private readonly ShowcaseSliceCatalog _slices;
    private readonly ShowcasePackageCatalog _packages;
    private readonly ShowcaseTextCatalog _text;
    private readonly IModuleCatalog _modules;
    private readonly IOptions<ShowcaseOptions> _options;
    private readonly ILogger<ShowcaseStartupService> _logger;
    private ReadinessCycle? _cycle;

    public ShowcaseStartupService(
        ReadinessSource readiness,
        ShowcaseRunnerState runner,
        ShowcaseSliceCatalog slices,
        ShowcasePackageCatalog packages,
        ShowcaseTextCatalog text,
        IModuleCatalog modules,
        IOptions<ShowcaseOptions> options,
        ILogger<ShowcaseStartupService> logger)
    {
        _readiness = readiness;
        _runner = runner;
        _slices = slices;
        _packages = packages;
        _text = text;
        _modules = modules;
        _options = options;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cycle = _readiness.BeginStartup();
        try
        {
            Validate();
            _readiness.MarkReady(_cycle);
            LogReady(_logger, _slices.All.Count, null);
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            var failure = new ReadinessFailure("showcase.startup", "Showcase startup validation failed.", exception);
            _readiness.MarkFailed(_cycle, failure);
            LogStartupFailure(_logger, exception);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_readiness.State is ReadinessState.Ready or ReadinessState.Failed or ReadinessState.Starting)
        {
            _readiness.BeginShutdown();
            _readiness.MarkStopped();
        }
        return Task.CompletedTask;
    }

    private void Validate()
    {
        if (!_runner.IsAccepting)
            throw new InvalidOperationException("The bounded runner must start before startup validation.");
        if (_packages.Current.Count == 0)
            throw new InvalidOperationException("The generated package catalog is empty.");
        if (_slices.All.Count == 0)
            throw new InvalidOperationException("No showcase slices were registered.");
        ModuleDescriptor[] requiredFailures = _modules.Modules
            .Where(static module => module.Required && module.Status != ModuleStatus.Registered)
            .ToArray();
        if (requiredFailures.Length > 0)
            throw new InvalidOperationException("One or more required Pocok modules failed to load.");

        foreach (IShowcaseSlice slice in _slices.All)
        {
            if (!_text.Contains(slice.Descriptor.ResourceNamespace, slice.Descriptor.DisplayNameKey, CultureInfo.InvariantCulture))
                throw new InvalidOperationException($"Missing invariant display name for {slice.Descriptor.PackageId}.");
            if (!_text.Contains(slice.Descriptor.ResourceNamespace, slice.Descriptor.SummaryKey, CultureInfo.InvariantCulture))
                throw new InvalidOperationException($"Missing invariant summary for {slice.Descriptor.PackageId}.");
        }

        if (_options.Value.RequireCompleteCatalog)
        {
            string[] missing = _packages.Current
                .Where(package => _slices.FindByPackageId(package.Id) is null)
                .Select(static package => package.Id)
                .ToArray();
            if (missing.Length > 0)
                throw new InvalidOperationException($"Strict catalog coverage is missing: {string.Join(", ", missing)}.");
        }
    }
}
