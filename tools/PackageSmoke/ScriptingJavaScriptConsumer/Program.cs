// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Scripting.Execution;
using Pocok.Scripting.JavaScript;

var runner = new ScriptRunner(new ScriptEngineRegistry([new JavaScriptScriptEngineAdapter()]));
ScriptResult<int> result = await runner.ExecuteAsync<int>(
    new ScriptExecutionRequest(ScriptEngineId.JavaScript, "smoke", "40 + 2;")
    {
        ExpectResult = true
    },
    new ScriptExecutionOptions
    {
        MaxStatements = 1_000,
        MaxRecursionDepth = 32,
        MaxMemoryBytes = 8 * 1024 * 1024
    });
return result.IsSuccess && result.Value == 42 ? 0 : 1;
