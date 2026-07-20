// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Scripting.Execution;
using Pocok.Scripting.Python;

var adapter = new PythonScriptEngineAdapter(new PythonScriptEngineOptions
{
    WorkerPath = Path.Combine(AppContext.BaseDirectory, "Pocok.Scripting", "PythonWorker", "pocok_worker.py")
});
if (!adapter.Descriptor.IsAvailable)
{
    Console.WriteLine(adapter.Descriptor.UnavailableCode);
    return string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("POCOK_PYTHON_EXECUTABLE")) ? 0 : 1;
}

var runner = new ScriptRunner(new ScriptEngineRegistry([adapter]));
ScriptResult<int> result = await runner.ExecuteAsync<int>(
    new ScriptExecutionRequest(ScriptEngineId.Python, "smoke", "sum([40, 2])")
    {
        ExpectResult = true
    },
    new ScriptExecutionOptions { Timeout = TimeSpan.FromSeconds(5) });
return result.IsSuccess && result.Value == 42 ? 0 : 1;
