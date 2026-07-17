// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Licensing.Runtime;

/// <summary>Provides deterministic wall-clock and process-runtime values for license validation.</summary>
public interface ILicenseClock
{
    /// <summary>Gets the current UTC time.</summary>
    public DateTimeOffset UtcNow { get; }

    /// <summary>Gets the elapsed runtime of the current application process.</summary>
    public TimeSpan ProcessRuntime { get; }
}
