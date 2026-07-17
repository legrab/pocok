// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.BackgroundWork;

/// <summary>Defines how coordinated background work responds to an operation failure.</summary>
public enum BackgroundWorkFailurePolicy
{
    /// <summary>Stop the current lifecycle and propagate the failure.</summary>
    Stop,

    /// <summary>Invoke the configured failure handler and continue when possible.</summary>
    Continue
}
