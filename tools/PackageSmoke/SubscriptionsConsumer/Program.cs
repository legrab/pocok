// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Subscriptions;

using KeyedSubscriptionHub<string> hub = new();
int observed = 0;
using IDisposable registration = hub.Subscribe<int>("sample", (_, value) => observed = value);

return hub.Publish("sample", 7) == 1 && observed == 7 ? 0 : 1;
