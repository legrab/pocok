// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors


using Pocok.Signals.Operations;

namespace Pocok.Signals.Sources;

/// <summary>
///     Resolves a caller-owned source for a source identifier.
/// </summary>
public delegate ValueTask<SignalResult<ISignalSource>> SignalSourceFactory(
    SourceId sourceId,
    CancellationToken cancellationToken);
