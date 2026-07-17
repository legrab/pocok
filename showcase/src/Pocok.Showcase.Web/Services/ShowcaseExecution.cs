// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Pocok.Showcase.Contracts;

namespace Pocok.Showcase.Web.Services;

internal sealed record ShowcaseRunEnvelope(
    IShowcaseSlice Slice,
    object Input,
    CultureInfo Culture,
    CultureInfo UiCulture,
    ChannelWriter<ShowcaseProgressEvent> Progress,
    TaskCompletionSource<ShowcaseRunResult> Completion,
    CancellationToken CallerCancellation);

public sealed class ShowcaseRunnerState
{
    private int _accepting;

    public bool IsAccepting => Volatile.Read(ref _accepting) == 1;

    internal void StartAccepting() => Volatile.Write(ref _accepting, 1);
    internal void StopAccepting() => Volatile.Write(ref _accepting, 0);
}

public sealed class ShowcaseRunBuffer
{
    private readonly Channel<ShowcaseRunEnvelope> _channel;
    private int _depth;

    public ShowcaseRunBuffer(IOptions<ShowcaseOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Capacity = options.Value.QueueCapacity;
        _channel = Channel.CreateBounded<ShowcaseRunEnvelope>(new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
    }

    public int Capacity { get; }
    public int Depth => Math.Max(0, Volatile.Read(ref _depth));

    internal bool TryWrite(ShowcaseRunEnvelope envelope)
    {
        if (!_channel.Writer.TryWrite(envelope))
            return false;

        Interlocked.Increment(ref _depth);
        return true;
    }

    internal async ValueTask<ShowcaseRunEnvelope?> ReadAsync(CancellationToken cancellationToken)
    {
        while (await _channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!_channel.Reader.TryRead(out ShowcaseRunEnvelope? envelope))
                continue;

            Interlocked.Decrement(ref _depth);
            return envelope;
        }

        return null;
    }

    internal void Complete() => _channel.Writer.TryComplete();
}

public sealed class ShowcaseRunClient : IShowcaseRunClient
{
    private readonly ShowcaseRunBuffer _queue;
    private readonly ShowcaseRunnerState _state;
    private readonly ShowcaseOptions _options;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private int _disposed;

    public ShowcaseRunClient(
        ShowcaseRunBuffer queue,
        ShowcaseRunnerState state,
        IOptions<ShowcaseOptions> options)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(options);
        _queue = queue;
        _state = state;
        _options = options.Value;
    }

    public ValueTask<ShowcaseRunHandle> SubmitAsync(
        IShowcaseSlice slice,
        object input,
        CultureInfo culture,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, this);
        ArgumentNullException.ThrowIfNull(slice);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(culture);

        if (!_state.IsAccepting)
            return ValueTask.FromResult(CompletedHandle(ShowcaseRunResult.Rejected(
                "Run rejected",
                "showcase.runner-unavailable",
                "The showcase runner is not accepting work.")));

        if (!slice.InputType.IsInstanceOfType(input))
            return ValueTask.FromResult(CompletedHandle(ShowcaseRunResult.Rejected(
                "Input rejected",
                "showcase.input-type",
                $"Expected {slice.InputType.Name}, received {input.GetType().Name}.")));

        if (!IsSupportedCulture(culture))
            return ValueTask.FromResult(CompletedHandle(ShowcaseRunResult.Rejected(
                "Culture rejected",
                "showcase.culture",
                "Only invariant English, English, and Hungarian cultures are supported.")));

        int inputBytes;
        try
        {
            inputBytes = JsonSerializer.SerializeToUtf8Bytes(input, input.GetType()).Length;
        }
        catch (Exception exception) when (exception is NotSupportedException or JsonException)
        {
            return ValueTask.FromResult(CompletedHandle(ShowcaseRunResult.Rejected(
                "Input rejected",
                "showcase.input-serialization",
                "The input model could not be measured safely.")));
        }

        if (inputBytes > _options.MaximumInputBytes)
            return ValueTask.FromResult(CompletedHandle(ShowcaseRunResult.Rejected(
                "Input rejected",
                "showcase.input-too-large",
                $"The input exceeds the {_options.MaximumInputBytes.ToString(CultureInfo.InvariantCulture)} byte limit.")));

        CultureInfo safeCulture = CultureInfo.ReadOnly((CultureInfo)culture.Clone());
        CultureInfo safeUiCulture = CultureInfo.ReadOnly((CultureInfo)culture.Clone());
        var progress = Channel.CreateBounded<ShowcaseProgressEvent>(new BoundedChannelOptions(32)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.DropOldest,
            AllowSynchronousContinuations = false
        });
        var completion = new TaskCompletionSource<ShowcaseRunResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var ownedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _lifetimeCancellation.Token);
        var envelope = new ShowcaseRunEnvelope(
            slice,
            input,
            safeCulture,
            safeUiCulture,
            progress.Writer,
            completion,
            ownedCancellation.Token);

        if (!_queue.TryWrite(envelope))
        {
            ownedCancellation.Dispose();
            progress.Writer.TryComplete();
            return ValueTask.FromResult(CompletedHandle(ShowcaseRunResult.Rejected(
                "Run rejected",
                "showcase.queue-full",
                "The bounded showcase queue is full. Try again after the current run completes.")));
        }

        return ValueTask.FromResult(new ShowcaseRunHandle(completion.Task, progress.Reader, ownedCancellation));
    }

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return ValueTask.CompletedTask;

        _lifetimeCancellation.Cancel();
        _lifetimeCancellation.Dispose();
        return ValueTask.CompletedTask;
    }

    private static bool IsSupportedCulture(CultureInfo culture) =>
        culture.Equals(CultureInfo.InvariantCulture)
        || culture.TwoLetterISOLanguageName is "en" or "hu";

    private static ShowcaseRunHandle CompletedHandle(ShowcaseRunResult result)
    {
        var progress = Channel.CreateUnbounded<ShowcaseProgressEvent>();
        progress.Writer.TryComplete();
        return new ShowcaseRunHandle(
            Task.FromResult(result),
            progress.Reader,
            new CancellationTokenSource());
    }
}

public sealed class ShowcaseRunnerService : BackgroundService
{
    private static readonly Action<ILogger, string, Exception?> LogRunFailure = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(1, nameof(LogRunFailure)),
        "Showcase run {CorrelationId} failed.");
    private readonly ShowcaseRunBuffer _queue;
    private readonly ShowcaseRunnerState _state;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<ShowcaseOptions> _options;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ShowcaseRunnerService> _logger;
    private readonly ShowcasePublicLog _publicLog;

    public ShowcaseRunnerService(
        ShowcaseRunBuffer queue,
        ShowcaseRunnerState state,
        IServiceScopeFactory scopeFactory,
        IOptions<ShowcaseOptions> options,
        IHostApplicationLifetime lifetime,
        TimeProvider timeProvider,
        ILogger<ShowcaseRunnerService> logger,
        ShowcasePublicLog publicLog)
    {
        _queue = queue;
        _state = state;
        _scopeFactory = scopeFactory;
        _options = options;
        _lifetime = lifetime;
        _timeProvider = timeProvider;
        _logger = logger;
        _publicLog = publicLog;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _state.StartAccepting();
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (true)
            {
                ShowcaseRunEnvelope? envelope = await _queue.ReadAsync(stoppingToken).ConfigureAwait(false);
                if (envelope is null)
                    break;

                await ExecuteEnvelopeAsync(envelope, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _state.StopAccepting();
        _queue.Complete();
        return base.StopAsync(cancellationToken);
    }

    private async Task ExecuteEnvelopeAsync(ShowcaseRunEnvelope envelope, CancellationToken stoppingToken)
    {
        string correlationId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        _publicLog.RunStarted(envelope.Slice.Descriptor.PackageId);
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        using var timeout = new CancellationTokenSource(_options.Value.RunTimeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            stoppingToken,
            _lifetime.ApplicationStopping,
            envelope.CallerCancellation,
            timeout.Token);

        var output = new BoundedOutputWriter(_options.Value.MaximumOutputCharacters);
        var progress = new ShowcaseProgressWriter(envelope.Progress, _timeProvider);
        var temporaryDirectories = new SafeTemporaryDirectoryFactory(_options.Value.MaximumTemporaryFiles);
        var context = new ShowcaseExecutionContext(
            _timeProvider,
            output,
            progress,
            envelope.Culture,
            envelope.UiCulture,
            temporaryDirectories,
            correlationId,
            scope.ServiceProvider);

        try
        {
            await progress.ReportAsync("queued", "Run accepted.", linked.Token).ConfigureAwait(false);
            ShowcaseRunResult result = await envelope.Slice.ExecuteUntypedAsync(
                envelope.Input,
                context,
                linked.Token).ConfigureAwait(false);
            envelope.Completion.TrySetResult(Sanitize(result, output));
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            envelope.Completion.TrySetResult(new ShowcaseRunResult(
                ShowcaseRunStatus.TimedOut,
                "Run timed out",
                diagnostics:
                [
                    new ShowcaseDiagnostic(
                        "showcase.timeout",
                        "The bounded run exceeded its time limit.",
                        "warning")
                ]));
        }
        catch (OperationCanceledException)
        {
            envelope.Completion.TrySetResult(new ShowcaseRunResult(
                ShowcaseRunStatus.Cancelled,
                "Run cancelled",
                diagnostics:
                [
                    new ShowcaseDiagnostic(
                        "showcase.cancelled",
                        "The run was cancelled.",
                        "info")
                ]));
        }
        catch (Exception exception)
        {
            LogRunFailure(_logger, correlationId, exception);
            envelope.Completion.TrySetResult(new ShowcaseRunResult(
                ShowcaseRunStatus.InternalFailure,
                "Run failed safely",
                diagnostics:
                [
                    new ShowcaseDiagnostic(
                        "showcase.internal",
                        $"The run failed safely. Correlation id: {correlationId}.",
                        "error")
                ]));
        }
        finally
        {
            envelope.Progress.TryComplete();
        }
    }

    private static ShowcaseRunResult Sanitize(ShowcaseRunResult result, BoundedOutputWriter output)
    {
        IReadOnlyList<ShowcaseResultField> fields = result.Fields
            .Take(200)
            .Select(static field => field with { Value = SanitizeText(field.Value, 4_096) })
            .ToArray();
        IReadOnlyList<ShowcaseTimelineEvent> timeline = result.Timeline
            .Take(100)
            .Select(static item => item with { Message = SanitizeText(item.Message, 1_000) ?? string.Empty })
            .ToArray();
        IReadOnlyList<ShowcaseDiagnostic> diagnostics = result.Diagnostics
            .Take(32)
            .Select(static diagnostic => diagnostic with
            {
                Message = SanitizeText(diagnostic.Message, 1_000) ?? string.Empty
            })
            .ToArray();
        string? code = SanitizeText(result.CodePreview, 16_384);

        return new ShowcaseRunResult(
            result.Status,
            SanitizeText(result.Headline, 512) ?? "Run completed",
            fields,
            timeline,
            diagnostics,
            code,
            result.Elapsed,
            result.IsTruncated || output.IsTruncated || result.Fields.Count > 200 || result.Timeline.Count > 100,
            result.TipKeys.Take(16).ToArray());
    }

    private static string? SanitizeText(string? value, int maximum)
    {
        if (value is null)
            return null;

        string safe = value.Replace(Path.GetTempPath(), "<temporary-directory>/", StringComparison.OrdinalIgnoreCase);
        safe = Regex.Replace(
            safe,
            @"(?i)\b(private[_ -]?key|password|secret|token)\s*[:=]\s*\S+",
            "$1=<redacted>",
            RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(50));
        return safe.Length <= maximum ? safe : string.Concat(safe.AsSpan(0, maximum), "…");
    }
}

public sealed class BoundedOutputWriter : IBoundedOutputWriter
{
    private readonly int _maximumCharacters;
    private readonly StringBuilder _builder = new();

    public BoundedOutputWriter(int maximumCharacters)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumCharacters, 1);
        _maximumCharacters = maximumCharacters;
    }

    public bool IsTruncated { get; private set; }

    public void Write(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (IsTruncated)
            return;

        int remaining = _maximumCharacters - _builder.Length;
        if (remaining <= 0)
        {
            IsTruncated = true;
            return;
        }

        if (value.Length > remaining)
        {
            _builder.Append(value.AsSpan(0, remaining));
            IsTruncated = true;
            return;
        }

        _builder.Append(value);
    }

    public void WriteLine(string value)
    {
        Write(value);
        Write(Environment.NewLine);
    }

    public string GetContent() => _builder.ToString();
}

internal sealed class ShowcaseProgressWriter : IShowcaseProgressWriter
{
    private readonly ChannelWriter<ShowcaseProgressEvent> _writer;
    private readonly TimeProvider _timeProvider;

    public ShowcaseProgressWriter(ChannelWriter<ShowcaseProgressEvent> writer, TimeProvider timeProvider)
    {
        _writer = writer;
        _timeProvider = timeProvider;
    }

    public ValueTask ReportAsync(string stage, string message, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return _writer.WriteAsync(
            new ShowcaseProgressEvent(_timeProvider.GetUtcNow(), stage, message),
            cancellationToken);
    }
}

internal sealed class SafeTemporaryDirectoryFactory : ISafeTemporaryDirectoryFactory
{
    private readonly int _maximumFiles;

    public SafeTemporaryDirectoryFactory(int maximumFiles)
    {
        _maximumFiles = maximumFiles;
    }

    public ValueTask<IAsyncDisposable> CreateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string root = Path.Combine(
            Path.GetTempPath(),
            "pocok-showcase",
            Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(root);
        return ValueTask.FromResult<IAsyncDisposable>(new TemporaryDirectory(root, _maximumFiles));
    }

    private sealed class TemporaryDirectory : IAsyncDisposable
    {
        private readonly string _path;
        private readonly int _maximumFiles;

        public TemporaryDirectory(string path, int maximumFiles)
        {
            _path = path;
            _maximumFiles = maximumFiles;
        }

        public ValueTask DisposeAsync()
        {
            try
            {
                if (Directory.Exists(_path))
                {
                    int count = Directory.EnumerateFiles(_path, "*", SearchOption.AllDirectories)
                        .Take(_maximumFiles + 1)
                        .Count();
                    if (count <= _maximumFiles)
                        Directory.Delete(_path, recursive: true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return ValueTask.CompletedTask;
        }
    }
}
