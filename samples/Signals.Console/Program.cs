// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Signals.Runtime;
using Pocok.Signals.Sources;

SourceId source = new("demo");
SignalAddress address = new(source, "temperature/outlet");
SignalSample<double> sample = new(
    21.5,
    true,
    DateTimeOffset.UtcNow,
    DateTimeOffset.UtcNow,
    SignalQuality.Good,
    1);

Console.WriteLine($"signal={address} quality={sample.Quality} value={sample.Value}");
