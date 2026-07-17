// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Collections.ObjectModel;
using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace Pocok.Showcase.Web.Services;

public static class ShowcaseInAppLogging
{
    public static bool Add(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        bool enabled = configuration.GetValue<bool?>(
            $"{ShowcaseOptions.SectionName}:{nameof(ShowcaseOptions.InAppLogConsoleEnabled)}") ?? true;
        if (!enabled)
            return false;

        services.AddSingleton<ShowcaseLogBuffer>();
        services.AddSingleton<ShowcaseLogProvider>();
        services.AddSingleton<ILoggerProvider>(static provider => provider.GetRequiredService<ShowcaseLogProvider>());
        return true;
    }
}

public sealed record ShowcaseLogRecord(
    long Sequence,
    DateTimeOffset Timestamp,
    LogLevel Level,
    string Category,
    EventId EventId,
    string Message,
    IReadOnlyDictionary<string, string> Properties);

public sealed class ShowcaseLogSubscription : IAsyncDisposable
{
    private readonly Action _dispose;
    private int _disposed;

    internal ShowcaseLogSubscription(ChannelReader<long> updates, Action dispose)
    {
        Updates = updates;
        _dispose = dispose;
    }

    public ChannelReader<long> Updates { get; }

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
            _dispose();
        return ValueTask.CompletedTask;
    }
}

public sealed class ShowcaseLogBuffer
{
    private readonly object _gate = new();
    private readonly int _capacity;
    private readonly LinkedList<ShowcaseLogRecord> _records = [];
    private readonly Dictionary<long, ChannelWriter<long>> _subscribers = [];
    private long _nextSequence;
    private long _nextSubscription;

    public ShowcaseLogBuffer(IOptions<ShowcaseOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _capacity = options.Value.InAppLogCapacity;
    }

    public int Capacity => _capacity;

    public ShowcaseLogRecord Append(
        DateTimeOffset timestamp,
        LogLevel level,
        string category,
        EventId eventId,
        string message,
        IReadOnlyDictionary<string, string> properties)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(properties);

        ChannelWriter<long>[] subscribers;
        ShowcaseLogRecord record;
        lock (_gate)
        {
            long sequence = checked(++_nextSequence);
            record = new ShowcaseLogRecord(sequence, timestamp, level, category, eventId, message, properties);
            _records.AddFirst(record);
            if (_records.Count > _capacity)
                _records.RemoveLast();
            subscribers = [.. _subscribers.Values];
        }

        foreach (ChannelWriter<long> subscriber in subscribers)
            subscriber.TryWrite(record.Sequence);
        return record;
    }

    public IReadOnlyList<ShowcaseLogRecord> Snapshot(LogLevel minimumLevel = LogLevel.Information)
    {
        lock (_gate)
        {
            return _records
                .Where(record => record.Level >= minimumLevel)
                .ToArray();
        }
    }

    public ShowcaseLogSubscription Subscribe()
    {
        var channel = Channel.CreateBounded<long>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        long id;
        lock (_gate)
        {
            id = checked(++_nextSubscription);
            _subscribers.Add(id, channel.Writer);
        }

        return new ShowcaseLogSubscription(channel.Reader, () => RemoveSubscription(id, channel.Writer));
    }

    private void RemoveSubscription(long id, ChannelWriter<long> writer)
    {
        lock (_gate)
            _subscribers.Remove(id);
        writer.TryComplete();
    }
}

public sealed class ShowcaseLogProvider : ILoggerProvider
{
    public const string AllowedCategoryPrefix = "Pocok.Showcase.Public.";
    private static readonly HashSet<string> AllowedProperties = new(StringComparer.Ordinal)
    {
        "Culture",
        "InstalledCount",
        "Package",
        "Page",
        "Theme",
        "Visible"
    };
    private readonly ShowcaseLogBuffer _buffer;
    private readonly TimeProvider _timeProvider;
    private readonly ShowcaseOptions _options;

    public ShowcaseLogProvider(
        ShowcaseLogBuffer buffer,
        TimeProvider timeProvider,
        IOptions<ShowcaseOptions> options)
    {
        _buffer = buffer;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public ILogger CreateLogger(string categoryName) => new ShowcaseLogger(this, categoryName);

    public void Dispose()
    {
    }

    private bool IsEnabled(string category, LogLevel level) =>
        level >= _options.InAppLogMinimumLevel
        && level != LogLevel.None
        && category.StartsWith(AllowedCategoryPrefix, StringComparison.Ordinal);

    private void Write<TState>(
        string category,
        LogLevel level,
        EventId eventId,
        TState state,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(category, level))
            return;

        string message = Sanitize(formatter(state, null), _options.InAppLogMaximumTextLength);
        if (message.Length == 0)
            return;

        var properties = new Dictionary<string, string>(StringComparer.Ordinal);
        if (state is IEnumerable<KeyValuePair<string, object?>> values)
        {
            foreach ((string key, object? value) in values)
            {
                if (AllowedProperties.Contains(key) && value is not null)
                    properties[key] = Sanitize(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty, 80);
            }
        }

        _buffer.Append(
            _timeProvider.GetUtcNow(),
            level,
            ShortenCategory(category),
            eventId,
            message,
            new ReadOnlyDictionary<string, string>(properties));
    }

    internal static string ShortenCategory(string category)
    {
        string[] parts = category.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length <= 2 ? category : $"…{parts[^2]}.{parts[^1]}";
    }

    internal static string Sanitize(string value, int maximumLength)
    {
        var result = new System.Text.StringBuilder(Math.Min(value.Length, maximumLength));
        bool previousWasSpace = false;
        foreach (char character in value)
        {
            char safe = char.IsControl(character) ? ' ' : character;
            bool isSpace = char.IsWhiteSpace(safe);
            if (isSpace && previousWasSpace)
                continue;
            if (result.Length == maximumLength)
                break;
            result.Append(safe);
            previousWasSpace = isSpace;
        }
        return result.ToString().Trim();
    }

    private sealed class ShowcaseLogger(ShowcaseLogProvider provider, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => provider.IsEnabled(category, logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            provider.Write(category, logLevel, eventId, state, formatter);
        }
    }
}

public sealed class ShowcasePublicLog
{
    private static readonly Action<ILogger, Exception?> LogInitializing = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(100, nameof(LogInitializing)),
        "Initializing the bounded Showcase host.");
    private static readonly Action<ILogger, int, Exception?> LogCatalog = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(101, nameof(LogCatalog)),
        "Validated {InstalledCount} installed sandbox(es).");
    private static readonly Action<ILogger, Exception?> LogRunner = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(102, nameof(LogRunner)),
        "The bounded sample runner is accepting work.");
    private static readonly Action<ILogger, Exception?> LogReady = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(103, nameof(LogReady)),
        "Showcase startup completed.");
    private static readonly Action<ILogger, string, Exception?> LogTheme = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(104, nameof(LogTheme)),
        "Theme changed to {Theme}.");
    private static readonly Action<ILogger, bool, Exception?> LogConsole = LoggerMessage.Define<bool>(
        LogLevel.Information,
        new EventId(105, nameof(LogConsole)),
        "In-app console visibility changed to {Visible}.");
    private static readonly Action<ILogger, string, Exception?> LogPage = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(106, nameof(LogPage)),
        "Opened {Page}.");
    private static readonly Action<ILogger, string, Exception?> LogRun = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(107, nameof(LogRun)),
        "Running the {Package} sample.");
    private readonly ILogger _shellLogger;
    private readonly ILogger _navigationLogger;
    private readonly ILogger _executionLogger;

    public ShowcasePublicLog(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _shellLogger = loggerFactory.CreateLogger(ShowcaseLogProvider.AllowedCategoryPrefix + "Shell");
        _navigationLogger = loggerFactory.CreateLogger(ShowcaseLogProvider.AllowedCategoryPrefix + "Navigation");
        _executionLogger = loggerFactory.CreateLogger(ShowcaseLogProvider.AllowedCategoryPrefix + "Execution");
    }

    public void Initializing() => LogInitializing(_shellLogger, null);
    public void CatalogValidated(int installedCount) => LogCatalog(_shellLogger, installedCount, null);
    public void RunnerReady() => LogRunner(_shellLogger, null);
    public void Ready() => LogReady(_shellLogger, null);
    public void ThemeChanged(string theme) => LogTheme(_shellLogger, theme, null);
    public void ConsoleVisibilityChanged(bool visible) => LogConsole(_shellLogger, visible, null);
    public void PageOpened(string page) => LogPage(_navigationLogger, page, null);
    public void RunStarted(string package) => LogRun(_executionLogger, package, null);
}

public sealed class ShowcaseUiState
{
    private bool _logConsoleVisible = true;

    public event Action? Changed;

    public bool LogConsoleVisible => _logConsoleVisible;

    public void SetLogConsoleVisible(bool visible)
    {
        if (_logConsoleVisible == visible)
            return;
        _logConsoleVisible = visible;
        Changed?.Invoke();
    }
}
