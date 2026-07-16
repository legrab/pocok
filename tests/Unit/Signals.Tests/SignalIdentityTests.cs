// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Signals;

namespace Pocok.Signals.Tests;

public sealed class SignalIdentityTests
{
    [Test]
    public void SourceAndAddressUseExplicitOrdinalIdentity()
    {
        SourceId source = new("line-a");
        SignalAddress address = new(source, "Temperature/Outlet");

        address.ToString().ShouldBe("line-a:Temperature/Outlet");
        new SourceId("line-a").ShouldBe(source);
        new SignalAddress(new SourceId("line-a"), "Temperature/Outlet").ShouldBe(address);
        new SignalAddress(new SourceId("line-a"), "temperature/outlet").ShouldNotBe(address);
    }

    [Test]
    public void IdentityRejectsInvalidBoundaries()
    {
        Should.Throw<ArgumentException>(() => new SourceId(" line-a"));
        Should.Throw<ArgumentException>(() => new SourceId("line\0a"));
        Should.Throw<ArgumentException>(() => new SignalAddress(default, "path"));
        Should.Throw<ArgumentException>(() => new SignalAddress(new SourceId("line-a"), " path"));
        Should.Throw<ArgumentException>(() => new SignalAddress(new SourceId("line-a"), "path\0value"));
    }
}
