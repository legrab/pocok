// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Subscriptions;

using KeyedSubscriptionHub<string> hub = new();
int observed = 0;
using IDisposable registration = hub.Subscribe<int>("temperature", (_, value) => observed = value);

int delivered = hub.Publish("temperature", 21);
Console.WriteLine($"delivered={delivered} observed={observed}");

return delivered == 1 && observed == 21 ? 0 : 1;
