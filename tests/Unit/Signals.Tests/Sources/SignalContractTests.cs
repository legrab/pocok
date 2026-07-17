// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Signals.Operations;
using Pocok.Signals.Sources;
using Pocok.Signals.Writing;

namespace Pocok.Signals.Tests.Sources;

public sealed class SignalContractTests
{
    [Test]
    public void FailuresRequireStableCodeAndMessage()
    {
        Should.Throw<ArgumentException>(() => new SignalFailure("", "message"));
        Should.Throw<ArgumentException>(() => new SignalFailure("code", ""));
    }

    [Test]
    public void CapabilitiesComposeWithoutImplicitOperations()
    {
        SignalSourceCapabilities capabilities = SignalSourceCapabilities.Read | SignalSourceCapabilities.Subscribe;

        capabilities.HasFlag(SignalSourceCapabilities.Read).ShouldBeTrue();
        capabilities.HasFlag(SignalSourceCapabilities.Write).ShouldBeFalse();
    }

    [Test]
    public void AcknowledgementMayOmitSampleButConfirmedEvidenceMayNot()
    {
        SignalWriteResult acknowledged = new(SignalWriteConsistency.Acknowledged, null);

        acknowledged.Sample.ShouldBeNull();
        Should.Throw<ArgumentException>(() =>
            new SignalWriteResult(SignalWriteConsistency.Confirmed, null));
    }
}
