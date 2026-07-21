// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Pocok.Showcase.Web.Services;
using Shouldly;

namespace Pocok.Showcase.Tests;

[TestFixture]
public sealed class ScriptingRateLimitTests
{
    [Test]
    public void LimitRejectsAfterConfiguredNumberAndReportsRetry()
    {
        var time = new ManualTimeProvider(new DateTimeOffset(2026, 7, 21, 8, 0, 0, TimeSpan.Zero));
        var options = Options.Create(new ShowcaseOptions
        {
            ScriptingClientExecutionLimit = 2,
            ScriptingClientExecutionWindow = TimeSpan.FromMinutes(10),
            ScriptingRateLimitMaximumTrackedClients = 16
        });
        var limiter = new ShowcaseScriptingClientLimiter(options, time);

        limiter.TryAcquire("client", out _).ShouldBeTrue();
        limiter.TryAcquire("client", out _).ShouldBeTrue();
        limiter.TryAcquire("client", out TimeSpan retryAfter).ShouldBeFalse();
        retryAfter.ShouldBe(TimeSpan.FromMinutes(10));
    }

    [Test]
    public void LimitResetsAfterConfiguredWindow()
    {
        var time = new ManualTimeProvider(new DateTimeOffset(2026, 7, 21, 8, 0, 0, TimeSpan.Zero));
        var options = Options.Create(new ShowcaseOptions
        {
            ScriptingClientExecutionLimit = 1,
            ScriptingClientExecutionWindow = TimeSpan.FromMinutes(10),
            ScriptingRateLimitMaximumTrackedClients = 16
        });
        var limiter = new ShowcaseScriptingClientLimiter(options, time);

        limiter.TryAcquire("client", out _).ShouldBeTrue();
        limiter.TryAcquire("client", out _).ShouldBeFalse();

        time.Advance(TimeSpan.FromMinutes(10));

        limiter.TryAcquire("client", out _).ShouldBeTrue();
    }

    [Test]
    public void ZeroLimitDisablesClientRateLimiting()
    {
        var options = Options.Create(new ShowcaseOptions
        {
            ScriptingClientExecutionLimit = 0
        });
        var limiter = new ShowcaseScriptingClientLimiter(options, TimeProvider.System);

        for (var index = 0; index < 100; index++)
            limiter.TryAcquire("client", out _).ShouldBeTrue();
    }

    [Test]
    public void TrackedClientCapUsesABoundedOverflowBucket()
    {
        var options = Options.Create(new ShowcaseOptions
        {
            ScriptingClientExecutionLimit = 1,
            ScriptingClientExecutionWindow = TimeSpan.FromMinutes(10),
            ScriptingRateLimitMaximumTrackedClients = 16
        });
        var limiter = new ShowcaseScriptingClientLimiter(options, TimeProvider.System);

        for (var index = 0; index < 15; index++)
            limiter.TryAcquire($"client-{index}", out _).ShouldBeTrue();

        limiter.TryAcquire("overflow-first", out _).ShouldBeTrue();
        limiter.TryAcquire("overflow-second", out _).ShouldBeFalse();
    }

    [Test]
    public void ClientIdentityIsStableForSameAddress()
    {
        var first = CreateIdentity("203.0.113.10");
        var second = CreateIdentity("203.0.113.10");
        var different = CreateIdentity("203.0.113.11");

        first.Key.ShouldBe(second.Key);
        first.Key.ShouldNotBe(different.Key);
        first.Key.ShouldNotContain("203.0.113.10");
    }

    [Test]
    public void ClientIdentityGroupsIpv6PrivacyAddressesByPrefix()
    {
        var first = CreateIdentity("2001:db8:1234:5678::1");
        var second = CreateIdentity("2001:db8:1234:5678::abcd");
        var different = CreateIdentity("2001:db8:1234:5679::1");

        first.Key.ShouldBe(second.Key);
        first.Key.ShouldNotBe(different.Key);
    }

    private static ShowcaseClientIdentity CreateIdentity(string address)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(address);
        return new ShowcaseClientIdentity(new HttpContextAccessor { HttpContext = context });
    }

    private sealed class ManualTimeProvider(DateTimeOffset initial) : TimeProvider
    {
        private DateTimeOffset _utcNow = initial;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan amount) => _utcNow = _utcNow.Add(amount);
    }
}
