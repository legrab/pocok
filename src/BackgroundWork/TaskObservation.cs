// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.BackgroundWork;

/// <summary>Owns completion of an intentionally non-awaited task observation.</summary>
public sealed class TaskObservation
{
    internal TaskObservation(Task<TaskObservationResult> completion)
    {
        Completion = completion;
    }

    /// <summary>Gets a task that completes after the source outcome and callbacks have been processed.</summary>
    /// <remarks>This task never faults. Failures are reported through <see cref="TaskObservationResult"/>.</remarks>
    public Task<TaskObservationResult> Completion { get; }
}
