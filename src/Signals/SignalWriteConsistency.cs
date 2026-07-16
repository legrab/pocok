// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Signals;

/// <summary>Defines the evidence a successful write must provide.</summary>
public enum SignalWriteConsistency
{
    /// <summary>The source acknowledged the request without confirming the resulting value.</summary>
    Acknowledged,
    /// <summary>The source directly confirmed the resulting sample.</summary>
    Confirmed,
    /// <summary>The source must read the signal after writing.</summary>
    ReadAfterWrite
}
