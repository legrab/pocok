// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.BackgroundWork.FailureHandling;

namespace Pocok.BackgroundWork.Debouncing;

/// <summary>Configures a <see cref="DebouncedTaskRunner" />.</summary>
public sealed record DebouncedTaskRunnerOptions
{
    /// <summary>Gets the quiet period required before work begins.</summary>
    public TimeSpan QuietPeriod { get; init; }

    /// <summary>Gets the time provider used for delays.</summary>
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    /// <summary>Gets the operation failure policy.</summary>
    public BackgroundWorkFailurePolicy FailurePolicy { get; init; } = BackgroundWorkFailurePolicy.Stop;

    /// <summary>
    ///     Gets the handler invoked for failures when <see cref="FailurePolicy" /> is
    ///     <see cref="BackgroundWorkFailurePolicy.Continue" />.
    /// </summary>
    public Func<Exception, CancellationToken, ValueTask>? OnFailure { get; init; }
}
