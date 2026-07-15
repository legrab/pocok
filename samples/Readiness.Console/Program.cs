// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Readiness;

var readiness = new ReadinessSource();
Task consumer = ConsumeWhenReadyAsync(readiness);
ReadinessCycle cycle = readiness.BeginStartup();

await Task.Yield();
readiness.MarkReady(cycle);
await consumer;

readiness.BeginShutdown();
readiness.MarkStopped();

static async Task ConsumeWhenReadyAsync(IReadinessSignal readiness)
{
    await readiness.WaitUntilReadyAsync();
    Console.WriteLine($"Integration state: {readiness.State}");
}
