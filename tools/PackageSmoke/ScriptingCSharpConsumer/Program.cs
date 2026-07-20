// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Scripting.CSharp;
using Pocok.Scripting.Execution;

var adapter = new CSharpScriptEngineAdapter(new CSharpScriptEngineOptions
{
    WorkerDirectory = Path.Combine(AppContext.BaseDirectory, "Pocok.Scripting", "CSharpWorker")
});
if (!adapter.Descriptor.IsAvailable)
{
    Console.WriteLine(adapter.Descriptor.UnavailableCode);
    return string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH")) ? 0 : 1;
}

var runner = new ScriptRunner(new ScriptEngineRegistry([adapter]));
ScriptResult<int> result = await runner.ExecuteAsync<int>(
    new ScriptExecutionRequest(ScriptEngineId.CSharp, "smoke", "40 + 2")
    {
        ExpectResult = true
    },
    new ScriptExecutionOptions { Timeout = TimeSpan.FromSeconds(5) });
return result.IsSuccess && result.Value == 42 ? 0 : 1;
