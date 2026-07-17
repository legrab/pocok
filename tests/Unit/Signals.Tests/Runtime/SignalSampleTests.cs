// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Signals.Operations;
using Pocok.Signals.Runtime;

namespace Pocok.Signals.Tests.Runtime;

public sealed class SignalSampleTests
{
    private static readonly DateTimeOffset ObservedAt = new(2026, 7, 16, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void GoodSampleMayContainLegitimateNull()
    {
        SignalSample<string?> sample = new(null, true, null, ObservedAt, SignalQuality.Good, 1);

        sample.HasValue.ShouldBeTrue();
        sample.Value.ShouldBeNull();
    }

    [Test]
    public void FailedSampleRequiresStructuredFailure()
    {
        SignalFailure failure = new("signals.source.failed", "The source failed.");
        SignalSample<object?> sample = new(null, false, null, ObservedAt, SignalQuality.Failed, 1, failure);

        sample.Failure.ShouldBe(failure);
        Should.Throw<ArgumentException>(() =>
            new SignalSample<object?>(null, false, null, ObservedAt, SignalQuality.Failed, 2));
    }

    [Test]
    public void InvalidQualityAndPresenceCombinationsAreRejected()
    {
        Should.Throw<ArgumentException>(() =>
            new SignalSample<int>(42, true, null, ObservedAt, SignalQuality.Unknown, 1));
        Should.Throw<ArgumentException>(() =>
            new SignalSample<int>(default, false, null, ObservedAt, SignalQuality.Good, 1));
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new SignalSample<int>(42, true, null, ObservedAt, SignalQuality.Good, 0));
    }
}
