// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Pocok.Showcase.Contracts;

namespace Pocok.Showcase.Web.Services;

public sealed class ShowcaseClientIdentity
{
    public ShowcaseClientIdentity(IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);

        HttpContext? context = httpContextAccessor.HttpContext;
        string? address = NormalizeAddress(context?.Connection.RemoteIpAddress);
        string source = string.IsNullOrWhiteSpace(address)
            ? $"circuit:{Guid.NewGuid():N}"
            : $"address:{address}";

        Key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }

    public string Key { get; }

    private static string? NormalizeAddress(IPAddress? address)
    {
        if (address is null)
            return null;
        if (address.IsIPv4MappedToIPv6)
            return address.MapToIPv4().ToString();
        if (address.AddressFamily != AddressFamily.InterNetworkV6)
            return address.ToString();

        byte[] bytes = address.GetAddressBytes();
        Array.Clear(bytes, 8, 8);
        return $"{new IPAddress(bytes)}/64";
    }
}

public sealed class ShowcaseScriptingClientLimiter(
    IOptions<ShowcaseOptions> options,
    TimeProvider timeProvider)
{
    private const string OverflowClientKey = "<overflow>";
    private readonly ShowcaseOptions _options =
        options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly TimeProvider _timeProvider =
        timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly ConcurrentDictionary<string, ClientWindow> _windows =
        new(StringComparer.Ordinal);
    private readonly object _admissionGate = new();
    private int _acquisitionCount;

    public bool TryAcquire(string clientKey, out TimeSpan retryAfter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientKey);

        if (_options.ScriptingClientExecutionLimit == 0)
        {
            retryAfter = TimeSpan.Zero;
            return true;
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        if ((Interlocked.Increment(ref _acquisitionCount) & 63) == 0)
            PruneExpired(now);

        ClientWindow window = GetOrAddWindow(clientKey, now);
        long nowTicks = now.UtcTicks;
        long windowTicks = _options.ScriptingClientExecutionWindow.Ticks;

        lock (window)
        {
            long elapsedTicks = nowTicks - window.StartedUtcTicks;
            if (elapsedTicks < 0 || elapsedTicks >= windowTicks)
            {
                window.StartedUtcTicks = nowTicks;
                window.Count = 0;
                elapsedTicks = 0;
            }

            Volatile.Write(ref window.LastSeenUtcTicks, nowTicks);
            if (window.Count >= _options.ScriptingClientExecutionLimit)
            {
                retryAfter = TimeSpan.FromTicks(Math.Max(0, windowTicks - elapsedTicks));
                return false;
            }

            window.Count++;
            retryAfter = TimeSpan.Zero;
            return true;
        }
    }

    private ClientWindow GetOrAddWindow(string clientKey, DateTimeOffset now)
    {
        if (_windows.TryGetValue(clientKey, out ClientWindow? existing))
            return existing;

        lock (_admissionGate)
        {
            if (_windows.TryGetValue(clientKey, out existing))
                return existing;

            int regularClientCapacity = _options.ScriptingRateLimitMaximumTrackedClients - 1;
            if (_windows.Count >= regularClientCapacity)
                PruneExpired(now);

            string boundedKey = _windows.Count < regularClientCapacity
                ? clientKey
                : OverflowClientKey;
            return _windows.GetOrAdd(
                boundedKey,
                static (_, ticks) => new ClientWindow(ticks),
                now.UtcTicks);
        }
    }

    private void PruneExpired(DateTimeOffset now)
    {
        long expirationTicks = now.UtcTicks - checked(_options.ScriptingClientExecutionWindow.Ticks * 2);
        foreach ((string key, ClientWindow window) in _windows)
        {
            if (Volatile.Read(ref window.LastSeenUtcTicks) > expirationTicks)
                continue;

            _windows.TryRemove(key, out _);
        }
    }

    private sealed class ClientWindow(long startedUtcTicks)
    {
        public long StartedUtcTicks = startedUtcTicks;
        public long LastSeenUtcTicks = startedUtcTicks;
        public int Count;
    }
}

public sealed class RateLimitedShowcaseRunClient(
    ShowcaseRunClient inner,
    ShowcaseScriptingClientLimiter limiter,
    ShowcaseClientIdentity clientIdentity) : IShowcaseRunClient
{
    private const string ScriptingPackageId = "Pocok.Scripting";
    private readonly ShowcaseRunClient _inner =
        inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly ShowcaseScriptingClientLimiter _limiter =
        limiter ?? throw new ArgumentNullException(nameof(limiter));
    private readonly ShowcaseClientIdentity _clientIdentity =
        clientIdentity ?? throw new ArgumentNullException(nameof(clientIdentity));

    public ValueTask<ShowcaseRunHandle> SubmitAsync(
        IShowcaseSlice slice,
        object input,
        CultureInfo culture,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slice);

        if (string.Equals(slice.Descriptor.PackageId, ScriptingPackageId, StringComparison.Ordinal)
            && !_limiter.TryAcquire(_clientIdentity.Key, out TimeSpan retryAfter))
        {
            int retrySeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
            return ValueTask.FromResult(RejectedHandle(ShowcaseRunResult.Rejected(
                "Run rejected",
                "showcase.scripting-client-limit",
                $"This client has reached the scripting execution limit. Try again in " +
                $"{retrySeconds.ToString(CultureInfo.InvariantCulture)} seconds.")));
        }

        return _inner.SubmitAsync(slice, input, culture, cancellationToken);
    }

    public ValueTask DisposeAsync() => _inner.DisposeAsync();

    private static ShowcaseRunHandle RejectedHandle(ShowcaseRunResult result)
    {
        var progress = System.Threading.Channels.Channel.CreateUnbounded<ShowcaseProgressEvent>();
        progress.Writer.TryComplete();
        return new ShowcaseRunHandle(
            Task.FromResult(result),
            progress.Reader,
            new CancellationTokenSource());
    }
}
