// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pocok.AppDefaults;
using Pocok.AppDefaults.Logging;
using Pocok.Conversion;
using Pocok.Readiness;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.ConfigureWith(new LoggingDefaultsConfigurator(options =>
{
    options.ClearProviders = true;
    options.AddSimpleConsole = true;
    options.MinimumLevel = LogLevel.Information;
    options.CategoryMinimumLevels["Microsoft.Hosting.Lifetime"] = LogLevel.Warning;
}));

builder.Services.AddSingleton<ValueConverter>();
builder.Services.AddSingleton<ReadinessSource>();
builder.Services.AddSingleton<IReadinessSignal>(provider => provider.GetRequiredService<ReadinessSource>());
builder.Services.AddHostedService<ImportWorker>();

using IHost host = builder.Build();
await host.StartAsync();
await host.Services.GetRequiredService<IReadinessSignal>().WaitUntilReadyAsync().WaitAsync(TimeSpan.FromSeconds(5));
await host.StopAsync();

internal sealed class ImportWorker(
    ValueConverter converter,
    ReadinessSource readiness,
    ILogger<ImportWorker> logger,
    IHostApplicationLifetime lifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ReadinessCycle cycle = readiness.BeginStartup();
        try
        {
            string[] rows = ["sensor-a;21,5", "sensor-b;19,75", "broken;not-a-number"];
            var context = new ConversionContext(CultureInfo.GetCultureInfo("de-DE"));
            var accepted = new List<Measurement>();

            foreach (var row in rows)
            {
                var cells = row.Split(';');
                ConversionResult<decimal> value = converter.Convert<decimal>(cells[1], context);
                if (value.IsFailure)
                {
                    logger.LogWarning("Rejected {Sensor}: {Code} at {Path}", cells[0], value.Error!.Code, value.Error.Path);
                    continue;
                }

                accepted.Add(new Measurement(cells[0], value.Value));
            }

            readiness.MarkReady(cycle);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Imported {Count} measurements: {Values}", accepted.Count, string.Join(", ", accepted));
            }

            await Task.Delay(50, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            if (readiness.State is ReadinessState.Starting)
            {
                readiness.CancelStartup(cycle, stoppingToken);
            }
        }
        catch (Exception exception)
        {
            readiness.MarkFailed(cycle, ReadinessFailure.FromException(
                "sample.import.failed", "The sample import failed.", exception));
            throw;
        }
        finally
        {
            lifetime.StopApplication();
        }
    }
}

internal sealed record Measurement(string Sensor, decimal Value);
