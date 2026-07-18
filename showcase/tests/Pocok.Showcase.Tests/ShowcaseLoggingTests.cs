// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pocok.Showcase.Web.Services;

namespace Pocok.Showcase.Tests;

[TestFixture]
public sealed class ShowcaseLoggingTests
{
    [Test]
    public void BufferEvictsOldestAndReturnsNewestFirst()
    {
        ShowcaseLogBuffer buffer = CreateBuffer(capacity: 4);

        for (int index = 0; index < 6; index++)
        {
            buffer.Append(
                DateTimeOffset.UnixEpoch.AddSeconds(index),
                LogLevel.Information,
                "…Public.Test",
                new EventId(index),
                $"message-{index}",
                EmptyProperties());
        }

        IReadOnlyList<ShowcaseLogRecord> records = buffer.Snapshot();
        records.Select(record => record.Message).ShouldBe(["message-5", "message-4", "message-3", "message-2"]);
        records.Select(record => record.Sequence).ShouldBeInOrder(SortDirection.Descending);
    }

    [Test]
    public void ProviderAcceptsOnlyAllowlistedCategoriesAndLevels()
    {
        ShowcaseLogBuffer buffer = CreateBuffer();
        using var provider = new ShowcaseLogProvider(
            buffer,
            new FixedTimeProvider(),
            Options.Create(new ShowcaseOptions
            {
                InAppLogMinimumLevel = LogLevel.Information,
                InAppLogMaximumTextLength = 80
            }));
        ILogger publicLogger = provider.CreateLogger("Pocok.Showcase.Public.Test");
        ILogger ordinaryLogger = provider.CreateLogger("Microsoft.AspNetCore.Requests");

        var tokenSet = new Dictionary<string, object?> { ["Token"] = "secret" };
        var tokenPrivateException = new InvalidOperationException("private");

        var themeSet = new Dictionary<string, object?> { ["Theme"] = "dark" };

        publicLogger.Log(LogLevel.Debug, new EventId(1), "hidden", null, static (state, _) => state);
        ordinaryLogger.Log(
            LogLevel.Critical,
            new EventId(2),
            tokenSet,
            tokenPrivateException,
            static (_, _) => "request secret");
        publicLogger.Log(
            LogLevel.Information,
            new EventId(3, "safe"),
            themeSet,
            null,
            static (_, _) => "visible dark\nvalue");

        ShowcaseLogRecord record = buffer.Snapshot().ShouldHaveSingleItem();
        record.Message.ShouldBe("visible dark value");
        record.Category.ShouldBe("…Public.Test");
        record.Properties.ShouldContainKey("Theme");
        record.Properties.ShouldNotContainKey("Token");
    }

    [Test]
    public async Task SubscriptionCoalescesAndCompletesWhenDisposed()
    {
        ShowcaseLogBuffer buffer = CreateBuffer();
        ShowcaseLogSubscription subscription = buffer.Subscribe();
        buffer.Append(DateTimeOffset.UnixEpoch, LogLevel.Information, "test", default, "one", EmptyProperties());
        buffer.Append(DateTimeOffset.UnixEpoch, LogLevel.Information, "test", default, "two", EmptyProperties());

        long sequence = await subscription.Updates.ReadAsync();
        sequence.ShouldBe(2);
        await subscription.DisposeAsync();
        (await subscription.Updates.WaitToReadAsync()).ShouldBeFalse();
    }

    [Test]
    public void ConcurrentWritersRemainBoundedAndOrdered()
    {
        ShowcaseLogBuffer buffer = CreateBuffer(capacity: 32);

        Parallel.For(0, 1_000, index => buffer.Append(
            DateTimeOffset.UnixEpoch,
            LogLevel.Information,
            "test",
            default,
            index.ToString(System.Globalization.CultureInfo.InvariantCulture),
            EmptyProperties()));

        IReadOnlyList<ShowcaseLogRecord> records = buffer.Snapshot();
        records.Count.ShouldBe(32);
        records.Select(record => record.Sequence).ShouldBeInOrder(SortDirection.Descending);
        records.Select(record => record.Sequence).Distinct().Count().ShouldBe(records.Count);
    }

    [Test]
    public void SnapshotFiltersLowerLevels()
    {
        ShowcaseLogBuffer buffer = CreateBuffer();
        buffer.Append(DateTimeOffset.UnixEpoch, LogLevel.Debug, "test", default, "debug", EmptyProperties());
        buffer.Append(DateTimeOffset.UnixEpoch, LogLevel.Warning, "test", default, "warning", EmptyProperties());

        buffer.Snapshot(LogLevel.Information).ShouldHaveSingleItem().Message.ShouldBe("warning");
    }

    [Test]
    public void OperatorSwitchPreventsProviderAndBufferRegistration()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Showcase:InAppLogConsoleEnabled"] = "false"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        var services = new ServiceCollection();

        ShowcaseInAppLogging.Add(services, configuration).ShouldBeFalse();
        services.ShouldNotContain(descriptor => descriptor.ServiceType == typeof(ShowcaseLogBuffer));
        services.ShouldNotContain(descriptor => descriptor.ServiceType == typeof(ShowcaseLogProvider));
    }

    [Test]
    public void PublicPageAndRunEventsPopulateTheAllowlistedStream()
    {
        ShowcaseLogBuffer buffer = CreateBuffer();
        using var provider = new ShowcaseLogProvider(
            buffer,
            new FixedTimeProvider(),
            Options.Create(new ShowcaseOptions()));
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        var publicLog = new ShowcasePublicLog(loggerFactory);

        publicLog.PageOpened("Pocok.Conversion");
        publicLog.RunStarted("Pocok.Scripting");

        IReadOnlyList<ShowcaseLogRecord> records = buffer.Snapshot();
        records.Select(record => record.Message).ShouldBe([
            "Running the Pocok.Scripting sample.",
            "Opened Pocok.Conversion."
        ]);
        records[0].Properties["Package"].ShouldBe("Pocok.Scripting");
        records[1].Properties["Page"].ShouldBe("Pocok.Conversion");
    }

    private static ShowcaseLogBuffer CreateBuffer(int capacity = 16) =>
        new(Options.Create(new ShowcaseOptions { InAppLogCapacity = capacity }));

    private static Dictionary<string, string> EmptyProperties() =>
        new Dictionary<string, string>(StringComparer.Ordinal);

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch;
    }
}
