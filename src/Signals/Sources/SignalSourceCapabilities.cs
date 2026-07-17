// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Signals.Sources;

/// <summary>Declares operations supported by a signal source.</summary>
[Flags]
public enum SignalSourceCapabilities
{
    /// <summary>No signal operation is supported.</summary>
    None = 0,

    /// <summary>Point-in-time reads are supported.</summary>
    Read = 1,

    /// <summary>Writes are supported.</summary>
    Write = 2,

    /// <summary>Streaming subscriptions are supported.</summary>
    Subscribe = 4
}
