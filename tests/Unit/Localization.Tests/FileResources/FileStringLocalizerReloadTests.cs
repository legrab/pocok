// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Localization.FileResources;
using Pocok.Localization.Tests.TestSupport;

namespace Pocok.Localization.Tests.FileResources;

public sealed class FileStringLocalizerReloadTests
{
    [Test]
    public async Task ManualReloadAtomicallyReplacesSnapshot()
    {
        using var directory = TemporaryDirectory.Create();
        var path = Path.Combine(directory.Path, "Messages.json");
        await File.WriteAllTextAsync(path, """{"Value":"old","OldOnly":"old"}""");
        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(directory.Path);

        await File.WriteAllTextAsync(path, """{"Value":"new","NewOnly":"new"}""");
        await localizer.ReloadAsync();

        localizer["Value"].Value.ShouldBe("new");
        localizer["NewOnly"].Value.ShouldBe("new");
        localizer["OldOnly"].ResourceNotFound.ShouldBeTrue();
        localizer.Status.LastError.ShouldBeNull();
        localizer.Status.HasValidSnapshot.ShouldBeTrue();
    }

    [Test]
    public async Task FailedReloadRetainsLastKnownGoodAndUpdatesStatus()
    {
        using var directory = TemporaryDirectory.Create();
        var path = Path.Combine(directory.Path, "Messages.json");
        await File.WriteAllTextAsync(path, """{"Value":"old"}""");
        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(directory.Path);

        await File.WriteAllTextAsync(path, "{\"Value\":");
        await Should.ThrowAsync<FormatException>(async () => await localizer.ReloadAsync());

        localizer["Value"].Value.ShouldBe("old");
        localizer.Status.LastError.ShouldBeOfType<FormatException>();
        localizer.Status.HasValidSnapshot.ShouldBeTrue();
    }

    [Test]
    public async Task ReloadRetriesPartialJsonWriteAndPublishesOnlyCompleteSnapshot()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        using var directory = TemporaryDirectory.Create();
        var path = Path.Combine(directory.Path, "Messages.json");
        await File.WriteAllTextAsync(path, """{"Value":"old"}""");
        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(
            directory.Path,
            options =>
            {
                options.ReloadRetryCount = 2;
                options.ReloadRetryDelay = TimeSpan.FromSeconds(1);
                options.TimeProvider = time;
            });

        await File.WriteAllTextAsync(path, "{\"Value\":");
        Task reload = localizer.ReloadAsync();
        localizer["Value"].Value.ShouldBe("old");

        await FileLocalizationTestAsync.UntilAsync(() => time.ScheduledTimerCount == 1);
        await File.WriteAllTextAsync(path, """{"Value":"new"}""");
        time.Advance(TimeSpan.FromSeconds(1));
        await reload;

        localizer["Value"].Value.ShouldBe("new");
        localizer.Status.LastError.ShouldBeNull();
    }

    [Test]
    public async Task ReloadRetriesPartialResxWrite()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        using var directory = TemporaryDirectory.Create();
        var path = Path.Combine(directory.Path, "Messages.resx");
        await File.WriteAllTextAsync(path, FileLocalizationTestData.CreateResx(("Value", "old")));
        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(
            directory.Path,
            options =>
            {
                options.ReloadRetryCount = 1;
                options.ReloadRetryDelay = TimeSpan.FromSeconds(1);
                options.TimeProvider = time;
            });

        await File.WriteAllTextAsync(path, "<root><data name=\"Value\">");
        Task reload = localizer.ReloadAsync();
        await FileLocalizationTestAsync.UntilAsync(() => time.ScheduledTimerCount == 1);
        await File.WriteAllTextAsync(path, FileLocalizationTestData.CreateResx(("Value", "new")));
        time.Advance(TimeSpan.FromSeconds(1));
        await reload;

        localizer["Value"].Value.ShouldBe("new");
    }

    [Test]
    public async Task CancellationBetweenRetriesCancelsReload()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        using var cancellation = new CancellationTokenSource();
        using var directory = TemporaryDirectory.Create();
        var path = Path.Combine(directory.Path, "Messages.json");
        File.WriteAllText(path, """{"Value":"old"}""");
        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(
            directory.Path,
            options =>
            {
                options.ReloadRetryCount = 3;
                options.ReloadRetryDelay = TimeSpan.FromSeconds(1);
                options.TimeProvider = time;
            });

        File.WriteAllText(path, "{\"Value\":");
        Task reload = localizer.ReloadAsync(cancellation.Token);
        await FileLocalizationTestAsync.UntilAsync(() => time.ScheduledTimerCount == 1);
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(async () => await reload);
        localizer["Value"].Value.ShouldBe("old");
    }

    [Test]
    public async Task DefaultMissingFileBehaviorRetainsSnapshot()
    {
        using var directory = TemporaryDirectory.Create();
        var path = Path.Combine(directory.Path, "Messages.json");
        await File.WriteAllTextAsync(path, """{"Value":"old"}""");
        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(directory.Path);
        File.Delete(path);

        await Should.ThrowAsync<IOException>(async () => await localizer.ReloadAsync());

        localizer["Value"].Value.ShouldBe("old");
    }

    [Test]
    public async Task RemoveMissingResourcesPublishesEmptySnapshot()
    {
        using var directory = TemporaryDirectory.Create();
        var path = Path.Combine(directory.Path, "Messages.json");
        await File.WriteAllTextAsync(path, """{"Value":"old"}""");
        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(
            directory.Path,
            options => options.MissingFileBehavior = MissingLocalizationFileBehavior.RemoveMissingResources);
        File.Delete(path);

        await localizer.ReloadAsync();

        localizer["Value"].ResourceNotFound.ShouldBeTrue();
        localizer.Status.LastError.ShouldBeNull();
    }

    [Test]
    public async Task StatusCallbackSeesStoredStatusAndCannotBreakPublication()
    {
        using var directory = TemporaryDirectory.Create();
        var path = Path.Combine(directory.Path, "Messages.json");
        await File.WriteAllTextAsync(path, """{"Value":"old"}""");
        FileStringLocalizer? observedLocalizer = null;
        var callbacks = 0;
        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(
            directory.Path,
            options => options.StatusChanged = status =>
            {
                callbacks++;
                if (observedLocalizer is not null) observedLocalizer.Status.ShouldBeSameAs(status);

                throw new InvalidOperationException("consumer callback");
            });
        observedLocalizer = localizer;

        await File.WriteAllTextAsync(path, """{"Value":"new"}""");
        await localizer.ReloadAsync();

        localizer["Value"].Value.ShouldBe("new");
        callbacks.ShouldBeGreaterThanOrEqualTo(2);
        localizer.Status.LastError.ShouldBeNull();
    }

    [Test]
    public async Task ConcurrentReloadsSerializeAndReadersSeeCompleteSnapshots()
    {
        using var directory = TemporaryDirectory.Create();
        var path = Path.Combine(directory.Path, "Messages.json");
        await File.WriteAllTextAsync(path, """{"First":"old","Second":"old"}""");
        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(directory.Path);
        await File.WriteAllTextAsync(path, """{"First":"new","Second":"new"}""");

        Task first = localizer.ReloadAsync();
        Task second = localizer.ReloadAsync();
        await Task.WhenAll(first, second);

        string[] values = [localizer["First"].Value, localizer["Second"].Value];
        values.ShouldBe(["new", "new"]);
    }

    [Test]
    public async Task ReadsRemainAvailableButReloadIsRejectedAfterDisposal()
    {
        using var directory = TemporaryDirectory.Create();
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "Messages.json"), """{"Value":"loaded"}""");
        FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(directory.Path);
        await localizer.DisposeAsync();

        localizer["Value"].Value.ShouldBe("loaded");
        Should.Throw<ObjectDisposedException>(() => localizer.ReloadAsync());
    }

    [Test]
    [Category("FileWatcher")]
    public async Task FileWatcherEventuallyReloadsAtomicReplacement()
    {
        using var directory = TemporaryDirectory.Create();
        var path = Path.Combine(directory.Path, "Messages.json");
        await File.WriteAllTextAsync(path, """{"Value":"old"}""");
        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(
            directory.Path,
            options =>
            {
                options.WatchForChanges = true;
                options.ReloadDebounce = TimeSpan.FromMilliseconds(50);
                options.ReloadRetryCount = 3;
                options.ReloadRetryDelay = TimeSpan.FromMilliseconds(25);
            });

        var replacement = Path.Combine(directory.Path, "replacement.tmp");
        await File.WriteAllTextAsync(replacement, """{"Value":"new"}""");
        File.Move(replacement, path, true);

        await FileLocalizationTestAsync.UntilAsync(
            () => localizer["Value"].Value == "new",
            () => $"Path: {path}; value: {localizer["Value"].Value}; status: {localizer.Status}");

        localizer.Status.LastError.ShouldBeNull();
    }
}
