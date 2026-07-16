// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Signals;

/// <summary>Describes whether a signal sample is usable and current.</summary>
public enum SignalQuality
{
    /// <summary>No source state has been observed yet.</summary>
    Unknown,
    /// <summary>The sample contains a current usable value.</summary>
    Good,
    /// <summary>The sample retains a usable value that is no longer fresh.</summary>
    Stale,
    /// <summary>The source reported bad quality; diagnostic data may remain.</summary>
    Bad,
    /// <summary>The source is disconnected and exposes no current value.</summary>
    Disconnected,
    /// <summary>Reading, conversion, or subscription processing failed.</summary>
    Failed
}
