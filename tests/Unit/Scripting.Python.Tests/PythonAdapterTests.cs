// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Scripting.Execution;
using Pocok.Scripting.Python;
using Shouldly;

namespace Pocok.Scripting.Python.Tests;

[TestFixture]
public sealed class PythonAdapterTests
{
    [Test]
    public void MissingPythonIsTruthfullyUnavailable()
    {
        var adapter = new PythonScriptEngineAdapter(new PythonScriptEngineOptions
        {
            PythonExecutable = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
        });

        adapter.Descriptor.IsAvailable.ShouldBeFalse();
        adapter.Descriptor.UnavailableCode.ShouldBe("scripting.python.executable_unavailable");
    }

    [Test]
    public async Task ConfiguredCpythonExecutesInChildProcess()
    {
        PythonScriptEngineAdapter adapter = CreateAvailableAdapterOrIgnore();
        var runner = new ScriptRunner(new ScriptEngineRegistry([adapter]));

        ScriptResult<int> result = await runner.ExecuteAsync<int>(
            new ScriptExecutionRequest(ScriptEngineId.Python, "test", "sum([20, 22])")
            {
                ExpectResult = true
            },
            new ScriptExecutionOptions { Timeout = TimeSpan.FromSeconds(5) });

        result.IsSuccess.ShouldBeTrue(result.Failure?.Message);
        result.Value.ShouldBe(42);
    }

    [Test]
    public async Task InfiniteLoopKillsTheWorkerAtTheConfiguredTimeout()
    {
        PythonScriptEngineAdapter adapter = CreateAvailableAdapterOrIgnore();
        var runner = new ScriptRunner(new ScriptEngineRegistry([adapter]));

        ScriptResult<object?> result = await runner.ExecuteAsync(
            new ScriptExecutionRequest(ScriptEngineId.Python, "timeout", "while True:\n    pass"),
            new ScriptExecutionOptions { Timeout = TimeSpan.FromMilliseconds(500) });

        result.IsSuccess.ShouldBeFalse();
        result.Failure!.Code.ShouldBe("scripting.execution.timeout");
    }

    [Test]
    public async Task AllowlistedSafeImportExecutes()
    {
        PythonScriptEngineAdapter adapter = CreateAvailableAdapterOrIgnore(
            new PythonScriptEngineOptions { AllowedImports = ["math"] });
        var runner = new ScriptRunner(new ScriptEngineRegistry([adapter]));

        ScriptResult<int> result = await runner.ExecuteAsync<int>(
            new ScriptExecutionRequest(ScriptEngineId.Python, "math", "import math\nmath.isqrt(1764)")
            {
                ExpectResult = true
            },
            new ScriptExecutionOptions { Timeout = TimeSpan.FromSeconds(5) });

        result.IsSuccess.ShouldBeTrue(result.Failure?.Message);
        result.Value.ShouldBe(42);
    }

    [Test]
    public async Task DangerousImportIsRejectedEvenWhenRequestedByHost()
    {
        PythonScriptEngineAdapter adapter = CreateAvailableAdapterOrIgnore(
            new PythonScriptEngineOptions { AllowedImports = ["os"] });

        ScriptValidationResult result = await adapter.Validator.ValidateAsync(
            new ScriptExecutionRequest(ScriptEngineId.Python, "test", "import os\nos.getcwd()"),
            new ScriptExecutionOptions { Timeout = TimeSpan.FromSeconds(5) });

        result.IsValid.ShouldBeFalse();
        result.Diagnostics.ShouldContain(item => item.Code == "scripting.python.import_denied");
    }

    private static PythonScriptEngineAdapter CreateAvailableAdapterOrIgnore(
        PythonScriptEngineOptions? options = null)
    {
        var adapter = new PythonScriptEngineAdapter(options);
        if (!adapter.Descriptor.IsAvailable)
            Assert.Ignore(
                adapter.Descriptor.UnavailableMessage
                ?? adapter.Descriptor.UnavailableCode
                ?? "The Python scripting adapter is unavailable.");
        return adapter;
    }
}
