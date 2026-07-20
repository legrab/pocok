// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Scripting.CSharp;
using Pocok.Scripting.Execution;
using Pocok.Scripting.JavaScript;
using Pocok.Scripting.Python;

IScriptEngineAdapter[] adapters =
[
    new JavaScriptScriptEngineAdapter(),
    new CSharpScriptEngineAdapter(),
    new PythonScriptEngineAdapter()
];
var registry = new ScriptEngineRegistry(adapters);
var runner = new ScriptRunner(registry);
var sources = new Dictionary<ScriptEngineId, string>
{
    [ScriptEngineId.JavaScript] = "21 * 2;",
    [ScriptEngineId.CSharp] = "21 * 2",
    [ScriptEngineId.Python] = "21 * 2"
};

foreach (ScriptEngineDescriptor descriptor in registry.Descriptors)
{
    if (!descriptor.IsAvailable)
    {
        Console.WriteLine($"{descriptor.Language}: {descriptor.UnavailableCode}");
        continue;
    }

    var options = new ScriptExecutionOptions { Timeout = TimeSpan.FromSeconds(5) };
    if (descriptor.Id == ScriptEngineId.JavaScript)
    {
        options = options with
        {
            MaxStatements = 1_000,
            MaxRecursionDepth = 32,
            MaxMemoryBytes = 8 * 1024 * 1024
        };
    }

    ScriptResult<int> result = await runner.ExecuteAsync<int>(
        new ScriptExecutionRequest(descriptor.Id, "console", sources[descriptor.Id])
        {
            ExpectResult = true
        },
        options);
    Console.WriteLine(result.IsSuccess
        ? $"{descriptor.Language}: {result.Value}"
        : $"{descriptor.Language}: {result.Failure!.Code}");
    if (!result.IsSuccess || result.Value != 42)
        return 1;
}

return 0;
