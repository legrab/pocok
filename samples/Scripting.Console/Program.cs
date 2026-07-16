// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Scripting;

var modules = new InMemoryScriptModuleSource([
    new ScriptModule("Arithmetic", "Samples", "function add(left, right) { return left + right; }")
]);
var injector = new ScriptImportInjector(new ScriptModuleResolver(modules));
InjectedScript injected = await injector.InjectAsync("// #import Arithmetic from Samples\nadd(20, 22);");
if (injected.Diagnostics.Count != 0) return 1;

ScriptResult<int> result = await new ScriptRunner().ExecuteAsync<int>(
    new ScriptExecutionRequest("sample.sum", injected.Content) { ExpectResult = true });
if (!result.IsSuccess || result.Value != 42) return 1;

Console.WriteLine($"script-result={result.Value}");
return 0;
