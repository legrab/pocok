// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Scripting;

namespace Pocok.Scripting.Tests;

public sealed class ScriptRunnerTests
{
    [Test]
    public async Task RunnerRejectsSourceLargerThanConfiguredLimit()
    {
        ScriptResult<object?> result = await new ScriptRunner().ExecuteAsync(
            new ScriptExecutionRequest("large", "12345;"),
            new ScriptExecutionOptions { MaxScriptLength = 4 });

        result.IsSuccess.ShouldBeFalse();
        result.Failure!.Code.ShouldBe("scripting.script.too_large");
    }

    [Test]
    public async Task RunnerExecutesTypedResultAndExplicitFunctionBinding()
    {
        ScriptExecutionRequest request = new("sum", "add(20, 22);")
        {
            ExpectResult = true,
            Bindings = [ScriptBinding.ForFunction("add", (Func<int, int, int>)((left, right) => left + right))]
        };

        ScriptResult<int> result = await new ScriptRunner().ExecuteAsync<int>(request);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Test]
    public async Task RunnerDoesNotExposeAmbientClrOrBrowserObjects()
    {
        ScriptResult<string> result = await new ScriptRunner().ExecuteAsync<string>(
            new ScriptExecutionRequest("surface", "typeof process + ':' + typeof require + ':' + typeof System;")
            {
                ExpectResult = true
            });

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("undefined:undefined:undefined");
    }

    [Test]
    public async Task RunnerReturnsFailureWithJavaScriptLocation()
    {
        ScriptResult<object?> result = await new ScriptRunner().ExecuteAsync(
            new ScriptExecutionRequest("failure", "const value = 1;\nthrow new Error('broken');"));

        result.IsSuccess.ShouldBeFalse();
        result.Failure!.Code.ShouldBe("scripting.javascript.error");
        result.Failure.Line.ShouldBe(2);
    }

    [Test]
    public async Task RunnerHonorsStatementLimit()
    {
        ScriptResult<object?> result = await new ScriptRunner().ExecuteAsync(
            new ScriptExecutionRequest("bounded", "let value = 0; while (true) { value++; }")
            {
                ExpectResult = true
            },
            new ScriptExecutionOptions { MaxStatements = 100 });

        result.IsSuccess.ShouldBeFalse();
        result.Failure!.Code.ShouldBe("scripting.execution.failed");
    }

    [Test]
    public async Task RunnerPropagatesCancellation()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            new ScriptRunner().ExecuteAsync(new ScriptExecutionRequest("cancelled", "1;"),
                cancellationToken: cancellation.Token));
    }
}
