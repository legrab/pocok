// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Pocok.AppDefaults;
using Pocok.AppDefaults.Logging;
using Pocok.AppDefaults.Logging.Serilog;
using Pocok.Showcase.Contracts;

namespace Pocok.Showcase.AppDefaults.Logging;

public sealed record LoggingInput
{
    public string SampleId { get; init; } = "structured";
    public LogLevel Level { get; init; } = LogLevel.Information;
    public int EventCount { get; init; } = 3;
    public bool IncludeException { get; init; }
    public bool IncludeProperty { get; init; } = true;
}

public sealed record LoggingEventOutput(
    string Timestamp,
    string Level,
    int EventId,
    string Category,
    string Template,
    string Message,
    IReadOnlyDictionary<string, string> Properties);

public sealed record LoggingOutput(
    IReadOnlyList<LoggingEventOutput> Events,
    string ProbeSummary);

public sealed class LoggingShowcaseSlice : ShowcaseSlice<LoggingInput, LoggingOutput>, IShowcasePackageCoverage
{
    private static readonly IReadOnlyList<ShowcaseSample<LoggingInput>> SampleDefinitions =
    [
        new(
            "structured",
            "Samples.structured.Name",
            "Samples.structured.Description",
            () => new LoggingInput(),
            true,
            "3 safe event(s)",
            "purpose",
            "logging"),
        new(
            "levels",
            "Samples.levels.Name",
            "Samples.levels.Description",
            () => new LoggingInput
            {
                SampleId = "levels",
                Level = LogLevel.Warning,
                EventCount = 4
            },
            false,
            "4 safe event(s)",
            "purpose",
            "logging"),
        new(
            "exception",
            "Samples.exception.Name",
            "Samples.exception.Description",
            () => new LoggingInput
            {
                SampleId = "exception",
                Level = LogLevel.Error,
                EventCount = 1,
                IncludeException = true
            },
            false,
            "1 safe event(s)",
            "purpose",
            "logging")
    ];

    public override ShowcaseSliceDescriptor Descriptor { get; } = new(
        "pocok.showcase.app-defaults-logging",
        "Pocok.AppDefaults.Logging",
        "app-defaults-logging",
        "MaintainerDefaults",
        "Active",
        "Package.Name",
        "Package.Summary",
        4,
        "src/AppDefaults.Logging/README.md",
        true,
        ShowcaseImplementationStatus.Available,
        "app-defaults-logging",
        "1.0.0");

    public override Type PageComponentType => typeof(LoggingPage);
    public override IReadOnlyList<ShowcaseSample<LoggingInput>> TypedSamples => SampleDefinitions;

    public override ShowcaseGuide Guide { get; } = new(
        [new ShowcaseGuideSection("purpose", "Guide.Purpose.Title", ["Guide.Purpose.Body"], ["logging"])],
        [
            new ShowcaseCodeSnippet(
                "logging",
                "Guide.Snippet.Title",
                "csharp",
                "logger.LogInformation(\"Processed item {ItemId}\", 42);")
        ]);

    public IReadOnlyList<string> CoveredPackageIds { get; } =
    [
        "Pocok.AppDefaults",
        "Pocok.AppDefaults.Logging",
        "Pocok.AppDefaults.Logging.Serilog"
    ];

    public override async ValueTask<LoggingOutput> ExecuteAsync(
        LoggingInput input,
        ShowcaseExecutionContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var count = Math.Clamp(input.EventCount, 1, 20);
        ILoggerFactory factory = context.Services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
        ILogger logger = factory.CreateLogger("Pocok.Showcase.Logging.Demo.Component");
        var events = new List<LoggingEventOutput>(count);

        for (var index = 0; index < count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var eventId = 100 + index;
            const string template = "Processed item {ItemId} for {Package}";
            var message = $"Processed item {index + 1} for Pocok";
            Exception? exception = input.IncludeException
                ? new InvalidOperationException("Synthetic failure without sensitive details.")
                : null;
            Action<ILogger, int, string, Exception?> emit = LoggerMessage.Define<int, string>(
                input.Level,
                new EventId(eventId, "ShowcaseEvent"),
                template);
            emit(logger, index + 1, "Pocok", exception);

            IReadOnlyDictionary<string, string> properties = input.IncludeProperty
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["ItemId"] = (index + 1).ToString(CultureInfo.InvariantCulture),
                    ["Package"] = "Pocok"
                }
                : new Dictionary<string, string>(StringComparer.Ordinal);

            events.Add(new LoggingEventOutput(
                context.TimeProvider.GetUtcNow().ToString("O", CultureInfo.InvariantCulture),
                input.Level.ToString(),
                eventId,
                "…Logging.Demo.Component",
                template,
                exception is null ? message : message + " — Synthetic failure",
                properties));
        }

        var probe = ProbeConfigurators();
        await context.Progress.ReportAsync(
            "complete",
            "Structured logging and AppDefaults probes completed.",
            cancellationToken).ConfigureAwait(false);
        return new LoggingOutput(
            events.OrderByDescending(static item => item.EventId).ToArray(),
            probe);
    }

    protected override ShowcaseRunResult CreateRunResult(LoggingOutput output, TimeSpan elapsed)
    {
        IReadOnlyList<ShowcaseResultField> fields = output.Events
            .Select(item => new ShowcaseResultField(
                $"{item.Level} #{item.EventId}",
                FormatRecord(item),
                true))
            .ToArray();

        return new ShowcaseRunResult(
            ShowcaseRunStatus.Success,
            $"{output.Events.Count} safe event(s)",
            fields,
            codePreview: output.ProbeSummary,
            elapsed: elapsed,
            tipKeys: ["Tips.Structured", "Tips.Safe"]);
    }

    private static string FormatRecord(LoggingEventOutput item)
    {
        var properties = string.Join(
            ", ",
            item.Properties.Select(static pair => $"{pair.Key}={pair.Value}"));
        return $"{item.Timestamp} | {item.Category} | {item.Template} | {item.Message} | {properties}";
    }

    private static string ProbeConfigurators()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.ConfigureWith(
            new LoggingDefaultsConfigurator(options =>
            {
                options.ClearProviders = true;
                options.AddSimpleConsole = false;
                options.MinimumLevel = LogLevel.Information;
                options.CategoryMinimumLevels["Pocok"] = LogLevel.Debug;
            }),
            new SerilogDefaultsConfigurator(options =>
            {
                options.PreserveStaticLogger = true;
                options.WriteToProviders = true;
            }));
        using IHost host = builder.Build();
        return "builder.ConfigureWith(new LoggingDefaultsConfigurator(...), " +
               "new SerilogDefaultsConfigurator(...));";
    }
}
