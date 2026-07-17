// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.BackgroundWork.Observation;

/// <summary>Identifies how an observed source task completed.</summary>
public enum TaskObservationOutcome
{
    /// <summary>The source task completed successfully.</summary>
    Succeeded,

    /// <summary>The source task faulted.</summary>
    Faulted,

    /// <summary>The source task was canceled.</summary>
    Canceled
}
