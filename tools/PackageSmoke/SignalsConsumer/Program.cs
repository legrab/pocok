// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Signals.Operations;
using Pocok.Signals.Runtime;
using Pocok.Signals.Sources;

SignalAddress address = new(new SourceId("consumer"), "sample");
await using var runtime = new SignalRuntime(
    (_, _) => ValueTask.FromResult(
        SignalResult.Failed<ISignalSource>(new SignalFailure("consumer.expected", "Synthetic source failure."))));
var connection = await runtime.ConnectAsync<object?>(address);

return address.Source.Value == "consumer" &&
       address.Path == "sample" &&
       connection.Failure?.Code == "consumer.expected" ? 0 : 1;
