// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.BackgroundWork;

/// <summary>Describes the source outcome and any failure raised while dispatching observation callbacks.</summary>
/// <param name="Outcome">The source task outcome.</param>
/// <param name="SourceException">The source exception for faulted or canceled tasks.</param>
/// <param name="ObserverException">A predicate or callback failure, including observation cancellation.</param>
/// <param name="CallbackInvoked">Whether an outcome callback began execution.</param>
public sealed record TaskObservationResult(
    TaskObservationOutcome Outcome,
    Exception? SourceException,
    Exception? ObserverException,
    bool CallbackInvoked);
