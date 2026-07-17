// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Scripting.Execution;

ScriptResult<int> result = await new ScriptRunner().ExecuteAsync<int>(
    new ScriptExecutionRequest("consumer", "21 * 2;") { ExpectResult = true });
return result.IsSuccess && result.Value == 42 ? 0 : 1;
