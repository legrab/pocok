// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Signals;

/// <summary>Describes a source and the operations it exposes.</summary>
public interface ISignalSource
{
    /// <summary>Gets the stable source identifier.</summary>
    public SourceId Id { get; }

    /// <summary>Gets the source-wide operation capabilities.</summary>
    public SignalSourceCapabilities Capabilities { get; }
}
