// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Pocok.Showcase.Contracts;
using Pocok.Showcase.Web.Services;

namespace Pocok.Showcase.Samples.Tests;

internal static class TestSupport
{
    public static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "global.json"))) directory = directory.Parent;
            return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
        }
    }

    public static ShowcaseExecutionContext CreateContext(
        CultureInfo? culture = null,
        IShowcaseProgressWriter? progress = null,
        IBoundedOutputWriter? output = null)
    {
        CultureInfo selected = culture ?? CultureInfo.InvariantCulture;
        return new ShowcaseExecutionContext(
            TimeProvider.System,
            output ?? new BoundedOutputWriter(4_096),
            progress ?? new RecordingProgressWriter(),
            selected,
            selected,
            new NoopTemporaryDirectoryFactory(),
            Guid.NewGuid().ToString("N"),
            EmptyServiceProvider.Instance);
    }

    public static async Task<ShowcaseRunResult> ExecuteAsync(IShowcaseSlice slice, object input, CultureInfo? culture = null,
        CancellationToken cancellationToken = default) =>
        await slice.ExecuteUntypedAsync(input, CreateContext(culture), cancellationToken);

    public static FakeWebHostEnvironment WebEnvironment() => new()
    {
        ApplicationName = "Pocok.Showcase.Tests",
        EnvironmentName = Environments.Production,
        ContentRootPath = Path.Combine(RepositoryRoot, "showcase", "src", "Pocok.Showcase.Web"),
        ContentRootFileProvider = new NullFileProvider(),
        WebRootPath = Path.Combine(RepositoryRoot, "showcase", "src", "Pocok.Showcase.Web", "wwwroot"),
        WebRootFileProvider = new NullFileProvider()
    };

    internal sealed class RecordingProgressWriter : IShowcaseProgressWriter
    {
        public List<ShowcaseProgressEvent> Events { get; } = [];
        public ValueTask ReportAsync(string stage, string message, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Events.Add(new ShowcaseProgressEvent(DateTimeOffset.UtcNow, stage, message));
            return ValueTask.CompletedTask;
        }
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new();
        public object? GetService(Type serviceType) => null;
    }

    private sealed class NoopTemporaryDirectoryFactory : ISafeTemporaryDirectoryFactory
    {
        public ValueTask<IAsyncDisposable> CreateAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IAsyncDisposable>(new NoopAsyncDisposable());
    }

    private sealed class NoopAsyncDisposable : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

internal sealed class FakeWebHostEnvironment : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = string.Empty;
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = string.Empty;
    public string EnvironmentName { get; set; } = Environments.Production;
    public string ContentRootPath { get; set; } = string.Empty;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

internal sealed class FakeLifetime : IHostApplicationLifetime, IDisposable
{
    private readonly CancellationTokenSource _started = new();
    private readonly CancellationTokenSource _stopping = new();
    private readonly CancellationTokenSource _stopped = new();
    public CancellationToken ApplicationStarted => _started.Token;
    public CancellationToken ApplicationStopping => _stopping.Token;
    public CancellationToken ApplicationStopped => _stopped.Token;
    public void StopApplication() => _stopping.Cancel();
    public void Dispose() { _started.Dispose(); _stopping.Dispose(); _stopped.Dispose(); }
}
