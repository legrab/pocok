// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.BackgroundWork.FailureHandling;

namespace Pocok.BackgroundWork.Coalescing;

/// <summary>Configures a <see cref="CoalescingTaskRunner" />.</summary>
public sealed record CoalescingTaskRunnerOptions
{
    /// <summary>Gets the minimum time from one operation completion to the next rerun start.</summary>
    public TimeSpan MinimumInterval { get; init; } = TimeSpan.Zero;

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
