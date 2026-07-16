// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Licensing;

/// <summary>Uses a <see cref="TimeProvider" /> to measure UTC time and process runtime.</summary>
public sealed class SystemLicenseClock : ILicenseClock
{
    private readonly long _started;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a clock from the supplied time provider.</summary>
    /// <param name="timeProvider">The time provider.</param>
    public SystemLicenseClock(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _started = _timeProvider.GetTimestamp();
    }

    /// <summary>Gets the current UTC time.</summary>
    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    /// <summary>Gets elapsed process runtime.</summary>
    public TimeSpan ProcessRuntime => _timeProvider.GetElapsedTime(_started);
}
