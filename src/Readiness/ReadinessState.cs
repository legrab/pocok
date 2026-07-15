// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Readiness;

/// <summary>
///     Describes the current lifecycle state of a readiness source.
/// </summary>
public enum ReadinessState
{
    /// <summary>
    ///     No startup attempt is active and a future startup may begin.
    /// </summary>
    Stopped,

    /// <summary>
    ///     Startup is active but readiness has not been reached.
    /// </summary>
    Starting,

    /// <summary>
    ///     The current startup attempt reached readiness.
    /// </summary>
    Ready,

    /// <summary>
    ///     Shutdown is active and the source is no longer ready.
    /// </summary>
    Stopping,

    /// <summary>
    ///     The current lifecycle attempt failed.
    /// </summary>
    Failed
}
