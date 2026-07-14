// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Hosting;

var readiness = new ReadinessSource();
var cycle = readiness.BeginStartup();
var waiter = readiness.WaitUntilReadyAsync();

readiness.MarkReady(cycle);
await waiter;

return readiness.State == ReadinessState.Ready ? 0 : 1;
