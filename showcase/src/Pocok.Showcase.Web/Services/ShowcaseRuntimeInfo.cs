// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Reflection;
using System.Runtime.InteropServices;

namespace Pocok.Showcase.Web.Services;

public sealed class ShowcaseRuntimeInfo
{
    private readonly TimeProvider _timeProvider;
    private readonly DateTimeOffset _startedAt;

    public ShowcaseRuntimeInfo(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
        _startedAt = timeProvider.GetUtcNow();
        Assembly assembly = typeof(ShowcaseRuntimeInfo).Assembly;
        ApplicationVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
    }

    public string ApplicationVersion { get; }
    public string FrameworkDescription => RuntimeInformation.FrameworkDescription;
    public string OperatingSystem => RuntimeInformation.OSDescription;
    public string Architecture => RuntimeInformation.ProcessArchitecture.ToString();
    public TimeSpan Uptime => _timeProvider.GetUtcNow() - _startedAt;
}
