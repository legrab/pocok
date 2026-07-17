// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.BackgroundWork.FailureHandling;

namespace Pocok.BackgroundWork.Repetition;

/// <summary>Configures repeated non-overlapping task execution.</summary>
public sealed record TaskRepeaterOptions
{
    /// <summary>Gets the delay applied after one iteration completes and before the next begins.</summary>
    public TimeSpan Interval { get; init; }

    /// <summary>Gets the delay before the first iteration.</summary>
    public TimeSpan InitialDelay { get; init; } = TimeSpan.Zero;

    /// <summary>Gets the maximum number of operation attempts, or <see langword="null" /> for no limit.</summary>
    public int? MaximumIterations { get; init; }

    /// <summary>Gets the time provider used for delays.</summary>
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    /// <summary>Gets an optional predicate evaluated before each iteration.</summary>
    public Func<bool>? ShouldContinue { get; init; }

    /// <summary>Gets the operation failure policy.</summary>
    public BackgroundWorkFailurePolicy FailurePolicy { get; init; } = BackgroundWorkFailurePolicy.Stop;

    /// <summary>
    ///     Gets the handler invoked for failures when <see cref="FailurePolicy" /> is
    ///     <see cref="BackgroundWorkFailurePolicy.Continue" />.
    /// </summary>
    public Func<Exception, CancellationToken, ValueTask>? OnFailure { get; init; }
}
