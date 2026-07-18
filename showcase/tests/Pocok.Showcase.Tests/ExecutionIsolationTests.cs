// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Pocok.Showcase.Components;
using Pocok.Showcase.Contracts;
using Pocok.Showcase.Web.Services;
using RunStatus = Pocok.Showcase.Contracts.ShowcaseRunStatus;

namespace Pocok.Showcase.Tests;

[TestFixture]
public sealed class ExecutionIsolationTests
{
    [Test]
    public void BoundedOutputStopsAtLimit()
    {
        var output = new BoundedOutputWriter(5);
        output.Write("1234");
        output.Write("5678");
        output.GetContent().ShouldBe("12345");
        output.IsTruncated.ShouldBeTrue();
    }

    [Test]
    public async Task ScopedClientsReceiveOnlyTheirOwnResults()
    {
        await using RunnerFixture fixture = await RunnerFixture.StartAsync();
        var slice = new DelayedSlice();
        await using var firstClient = new ShowcaseRunClient(fixture.Queue, fixture.State, fixture.Options);
        await using var secondClient = new ShowcaseRunClient(fixture.Queue, fixture.State, fixture.Options);
        await using ShowcaseRunHandle first = await firstClient.SubmitAsync(slice, new DelayInput("first", 20), CultureInfo.GetCultureInfo("en"));
        await using ShowcaseRunHandle second = await secondClient.SubmitAsync(slice, new DelayInput("second", 20), CultureInfo.GetCultureInfo("hu"));
        ShowcaseRunResult[] results = await Task.WhenAll(first.Completion, second.Completion);
        results[0].Headline.ShouldBe("first:en");
        results[1].Headline.ShouldBe("second:hu");
    }

    [Test]
    public async Task CancellingOneRunDoesNotCancelAnother()
    {
        await using RunnerFixture fixture = await RunnerFixture.StartAsync();
        var slice = new DelayedSlice();
        await using var firstClient = new ShowcaseRunClient(fixture.Queue, fixture.State, fixture.Options);
        await using var secondClient = new ShowcaseRunClient(fixture.Queue, fixture.State, fixture.Options);
        await using ShowcaseRunHandle first = await firstClient.SubmitAsync(slice, new DelayInput("cancel", 500), CultureInfo.InvariantCulture);
        await using ShowcaseRunHandle second = await secondClient.SubmitAsync(slice, new DelayInput("keep", 1), CultureInfo.InvariantCulture);
        first.Cancel();
        ShowcaseRunResult cancelled = await first.Completion;
        ShowcaseRunResult completed = await second.Completion;
        cancelled.Status.ShouldBe(RunStatus.Cancelled);
        completed.Headline.ShouldBe("keep:");
    }


    [Test]
    public async Task DisposingClientCancelsItsOwnedRun()
    {
        await using RunnerFixture fixture = await RunnerFixture.StartAsync();
        var slice = new DelayedSlice();
        var client = new ShowcaseRunClient(fixture.Queue, fixture.State, fixture.Options);
        await using ShowcaseRunHandle handle = await client.SubmitAsync(
            slice,
            new DelayInput("disposed", 500),
            CultureInfo.InvariantCulture);

        await client.DisposeAsync();

        ShowcaseRunResult result = await handle.Completion;
        result.Status.ShouldBe(RunStatus.Cancelled);
    }

    [Test]
    public async Task RunTimeoutIsBounded()
    {
        await using RunnerFixture fixture = await RunnerFixture.StartAsync(TimeSpan.FromMilliseconds(20));
        var slice = new DelayedSlice();
        await using var client = new ShowcaseRunClient(fixture.Queue, fixture.State, fixture.Options);
        await using ShowcaseRunHandle handle = await client.SubmitAsync(slice, new DelayInput("slow", 500), CultureInfo.InvariantCulture);
        ShowcaseRunResult result = await handle.Completion;
        result.Status.ShouldBe(RunStatus.TimedOut);
    }

    [Test]
    public async Task ProgressChannelIsPrivateAndCompletes()
    {
        await using RunnerFixture fixture = await RunnerFixture.StartAsync();
        var slice = new DelayedSlice();
        await using var client = new ShowcaseRunClient(fixture.Queue, fixture.State, fixture.Options);
        await using ShowcaseRunHandle handle = await client.SubmitAsync(slice, new DelayInput("progress", 1), CultureInfo.InvariantCulture);
        var events = new List<ShowcaseProgressEvent>();
        await foreach (ShowcaseProgressEvent item in handle.Progress.ReadAllAsync()) events.Add(item);
        await handle.Completion;
        events.Count.ShouldBeGreaterThan(0);
    }


    [Test]
    public async Task QueueFullIsRejectedWithoutCrossSessionLookup()
    {
        await using RunnerFixture fixture = await RunnerFixture.StartAsync(capacity: 1);
        var slice = new GatedSlice();
        await using var client = new ShowcaseRunClient(fixture.Queue, fixture.State, fixture.Options);
        await using ShowcaseRunHandle first = await client.SubmitAsync(
            slice,
            new DelayInput("first", 0),
            CultureInfo.InvariantCulture);
        await slice.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await using ShowcaseRunHandle second = await client.SubmitAsync(
            slice,
            new DelayInput("second", 0),
            CultureInfo.InvariantCulture);
        await using ShowcaseRunHandle rejected = await client.SubmitAsync(
            slice,
            new DelayInput("third", 0),
            CultureInfo.InvariantCulture);

        ShowcaseRunResult rejectedResult = await rejected.Completion;
        rejectedResult.Status.ShouldBe(RunStatus.Rejected);
        rejectedResult.Diagnostics.Single().Code.ShouldBe("showcase.queue-full");

        slice.Release.TrySetResult();
        await first.Completion;
        await second.Completion;
    }

    [Test]
    public async Task OversizedInputIsRejectedBeforeQueueing()
    {
        await using RunnerFixture fixture = await RunnerFixture.StartAsync(maximumInputBytes: 32);
        var slice = new DelayedSlice();
        await using var client = new ShowcaseRunClient(fixture.Queue, fixture.State, fixture.Options);
        await using ShowcaseRunHandle handle = await client.SubmitAsync(
            slice,
            new DelayInput(new string('x', 256), 0),
            CultureInfo.InvariantCulture);

        ShowcaseRunResult result = await handle.Completion;
        result.Status.ShouldBe(RunStatus.Rejected);
        result.Diagnostics.Single().Code.ShouldBe("showcase.input-too-large");
    }

    [Test]
    public async Task InternalFailureIsClassifiedWithoutExceptionDetails()
    {
        await using RunnerFixture fixture = await RunnerFixture.StartAsync();
        var slice = new ThrowingSlice();
        await using var client = new ShowcaseRunClient(fixture.Queue, fixture.State, fixture.Options);
        await using ShowcaseRunHandle handle = await client.SubmitAsync(
            slice,
            new DelayInput("failure", 0),
            CultureInfo.InvariantCulture);

        ShowcaseRunResult result = await handle.Completion;
        result.Status.ShouldBe(RunStatus.InternalFailure);
        result.Diagnostics.Single().Message.ShouldNotContain("deliberate test exception");
    }

    [Test]
    public async Task DiagnosticsRedactSecretsAndTemporaryPaths()
    {
        await using RunnerFixture fixture = await RunnerFixture.StartAsync();
        var slice = new UnsafeDiagnosticSlice();
        await using var client = new ShowcaseRunClient(fixture.Queue, fixture.State, fixture.Options);
        await using ShowcaseRunHandle handle = await client.SubmitAsync(
            slice,
            new DelayInput("safe", 0),
            CultureInfo.InvariantCulture);

        ShowcaseRunResult result = await handle.Completion;
        string message = result.Diagnostics.Single().Message;
        message.ShouldContain("<redacted>");
        message.ShouldContain("<temporary-directory>");
        message.ShouldNotContain("top-secret");
        message.ShouldNotContain(Path.GetTempPath());
    }

    private sealed record DelayInput(string Value, int DelayMilliseconds);

    private class DelayedSlice : ShowcaseSlice<DelayInput, string>
    {
        public override ShowcaseSliceDescriptor Descriptor { get; } = new("test.delay", "Test.Delay", "delay", "Test", "Test",
            "Name", "Summary", 1, "README.md", true, ShowcaseImplementationStatus.Available, "test", "1.0.0");
        public override Type PageComponentType => typeof(ShowcasePackageHeader);
        public override IReadOnlyList<ShowcaseSample<DelayInput>> TypedSamples { get; } =
            [new("default", "Name", "Description", () => new DelayInput("default", 1), true, "default")];
        public override ShowcaseGuide Guide => ShowcaseGuide.Empty;

        public override async ValueTask<string> ExecuteAsync(DelayInput input, ShowcaseExecutionContext context,
            CancellationToken cancellationToken)
        {
            await Task.Delay(input.DelayMilliseconds, cancellationToken);
            await context.Progress.ReportAsync("done", input.Value, cancellationToken);
            return $"{input.Value}:{context.Culture.Name}";
        }

        protected override ShowcaseRunResult CreateRunResult(string output, TimeSpan elapsed) =>
            new(RunStatus.Success, output, elapsed: elapsed);
    }


    private sealed class GatedSlice : DelayedSlice
    {
        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<string> ExecuteAsync(
            DelayInput input,
            ShowcaseExecutionContext context,
            CancellationToken cancellationToken)
        {
            Started.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return await base.ExecuteAsync(input, context, cancellationToken);
        }
    }

    private sealed class ThrowingSlice : DelayedSlice
    {
        public override ValueTask<string> ExecuteAsync(
            DelayInput input,
            ShowcaseExecutionContext context,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("deliberate test exception");
    }

    private sealed class UnsafeDiagnosticSlice : DelayedSlice
    {
        protected override ShowcaseRunResult CreateRunResult(string output, TimeSpan elapsed) =>
            new(
                RunStatus.Success,
                output,
                diagnostics:
                [
                    new ShowcaseDiagnostic(
                        "test.unsafe",
                        $"token=top-secret path={Path.Combine(Path.GetTempPath(), "pocok-showcase", "run")}")
                ],
                elapsed: elapsed);
    }

    private sealed class RunnerFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _services;
        private readonly FakeLifetime _lifetime;
        private readonly ShowcaseRunnerService _worker;
        private RunnerFixture(ServiceProvider services, FakeLifetime lifetime, ShowcaseRunBuffer queue,
            ShowcaseRunnerState state, IOptions<ShowcaseOptions> options, ShowcaseRunnerService worker)
        {
            _services = services;
            _lifetime = lifetime;
            Queue = queue;
            State = state;
            Options = options;
            _worker = worker;
        }

        public ShowcaseRunBuffer Queue { get; }
        public ShowcaseRunnerState State { get; }
        public IOptions<ShowcaseOptions> Options { get; }

        public static async Task<RunnerFixture> StartAsync(
            TimeSpan? timeout = null,
            int capacity = 4,
            int maximumInputBytes = 65_536)
        {
            var services = new ServiceCollection().BuildServiceProvider();
            var options = Microsoft.Extensions.Options.Options.Create(new ShowcaseOptions
            {
                QueueCapacity = capacity,
                RunTimeout = timeout ?? TimeSpan.FromSeconds(2),
                MaximumInputBytes = maximumInputBytes
            });
            var queue = new ShowcaseRunBuffer(options);
            var state = new ShowcaseRunnerState();
            var lifetime = new FakeLifetime();
            var worker = new ShowcaseRunnerService(queue, state, services.GetRequiredService<IServiceScopeFactory>(),
                options, lifetime, TimeProvider.System, NullLogger<ShowcaseRunnerService>.Instance,
                new ShowcasePublicLog(NullLoggerFactory.Instance));
            await worker.StartAsync(CancellationToken.None);
            return new RunnerFixture(services, lifetime, queue, state, options, worker);
        }

        public async ValueTask DisposeAsync()
        {
            await _worker.StopAsync(CancellationToken.None);
            _worker.Dispose();
            _lifetime.Dispose();
            await _services.DisposeAsync();
        }
    }
}
