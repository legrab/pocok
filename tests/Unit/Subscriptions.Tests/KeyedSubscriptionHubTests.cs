// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Subscriptions;

namespace Pocok.Subscriptions.Tests;

public sealed class KeyedSubscriptionHubTests
{
    [Test]
    public void PublishDeliversOnlyToMatchingKeyAndReturnsDeliveryCount()
    {
        using KeyedSubscriptionHub<string> hub = new();
        List<int> values = [];
        using IDisposable registration = hub.Subscribe<int>("temperature", (_, value) => values.Add(value));
        using IDisposable otherRegistration = hub.Subscribe<int>("pressure", (_, value) => values.Add(value));

        hub.Publish("temperature", 21).ShouldBe(1);
        hub.Publish("pressure", 5).ShouldBe(1);
        hub.Publish("unknown", 0).ShouldBe(0);

        values.ShouldBe([21, 5]);
        hub.Keys.Count.ShouldBe(2);
        hub.Keys.ShouldContain("temperature");
        hub.Keys.ShouldContain("pressure");
    }

    [Test]
    public void PublishAppliesObjectFilterMapperAndValueFilter()
    {
        using KeyedSubscriptionHub<string> hub = new();
        List<string> values = [];
        using IDisposable registration = hub.Subscribe<string>("messages", (_, value) => values.Add(value!), options =>
        {
            options
                .WithObjectFilter(value => value is int)
                .WithValueMapper(value => $"value:{value}")
                .WithValueFilter(value => value?.EndsWith('2') is true);
        });

        hub.Publish("messages", "ignored").ShouldBe(0);
        hub.Publish("messages", 1).ShouldBe(0);
        hub.Publish("messages", 2).ShouldBe(1);

        values.ShouldBe(["value:2"]);
    }

    [Test]
    public void PublishSkipsIncompatibleValuesWithTheDefaultMapper()
    {
        using KeyedSubscriptionHub<string> hub = new();
        List<int> values = [];
        using IDisposable registration = hub.Subscribe<int>("numbers", (_, value) => values.Add(value));

        hub.Publish("numbers", "not-a-number").ShouldBe(0);
        hub.Publish("numbers", 7).ShouldBe(1);

        values.ShouldBe([7]);
    }

    [Test]
    public void RegistrationDisposeRemovesListenerAndLastKey()
    {
        using KeyedSubscriptionHub<string> hub = new();
        IDisposable registration = hub.Subscribe<string>("events", (_, _) => { });

        hub.Keys.ShouldBe(["events"]);
        registration.Dispose();
        registration.Dispose();

        hub.Keys.ShouldBeEmpty();
        hub.Publish("events", "value").ShouldBe(0);
    }

    [Test]
    public void DisposePreventsNewOperations()
    {
        KeyedSubscriptionHub<string> hub = new();
        hub.Dispose();

        Should.Throw<ObjectDisposedException>(() => hub.Subscribe<string>("events", (_, _) => { }));
        Should.Throw<ObjectDisposedException>(() => hub.Publish("events", "value"));
        Should.Throw<ObjectDisposedException>(() => _ = hub.Keys);
    }
}
