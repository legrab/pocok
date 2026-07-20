// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Scripting.Execution;
using Shouldly;

namespace Pocok.Scripting.JavaScript.Tests;

[TestFixture]
public sealed class JavaScriptAdapterTests
{
    [Test]
    public async Task ExecutesBoundedExpression()
    {
        var adapter = new JavaScriptScriptEngineAdapter();
        var runner = new ScriptRunner(new ScriptEngineRegistry([adapter]));

        ScriptResult<int> result = await runner.ExecuteAsync<int>(
            new ScriptExecutionRequest(ScriptEngineId.JavaScript, "test", "21 * 2;")
            {
                ExpectResult = true
            },
            new ScriptExecutionOptions
            {
                MaxStatements = 1_000,
                MaxRecursionDepth = 32,
                MaxMemoryBytes = 8 * 1024 * 1024
            });

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [TestCase("eval('1')", "scripting.javascript.eval")]
    [TestCase("new Function('return 1')()", "scripting.javascript.function_constructor")]
    [TestCase("import('x')", "scripting.javascript.dynamic_import")]
    public async Task RejectsDynamicCodeBeforeExecution(string source, string code)
    {
        var validator = new JavaScriptScriptValidator();

        ScriptValidationResult result = await validator.ValidateAsync(
            new ScriptExecutionRequest(ScriptEngineId.JavaScript, "test", source),
            new ScriptExecutionOptions());

        result.IsValid.ShouldBeFalse();
        result.Diagnostics.ShouldContain(item => item.Code == code);
    }

    [Test]
    public async Task RejectsEvalInsideTemplateInterpolation()
    {
        var validator = new JavaScriptScriptValidator();

        ScriptValidationResult result = await validator.ValidateAsync(
            new ScriptExecutionRequest(
                ScriptEngineId.JavaScript,
                "test",
                "const value = `${eval('1')}`; value;"),
            new ScriptExecutionOptions());

        result.IsValid.ShouldBeFalse();
        result.Diagnostics.ShouldContain(item => item.Code == "scripting.javascript.eval");
    }

    [Test]
    public async Task RejectsTrivialEvalAliasBeforeExecution()
    {
        var validator = new JavaScriptScriptValidator();

        ScriptValidationResult result = await validator.ValidateAsync(
            new ScriptExecutionRequest(
                ScriptEngineId.JavaScript,
                "test",
                "const indirect = eval; indirect('1');"),
            new ScriptExecutionOptions());

        result.IsValid.ShouldBeFalse();
        result.Diagnostics.ShouldContain(item => item.Code == "scripting.javascript.eval_alias");
    }

    [Test]
    public async Task InfiniteLoopStopsAtTheConfiguredTimeout()
    {
        var runner = new ScriptRunner(new ScriptEngineRegistry([new JavaScriptScriptEngineAdapter()]));

        ScriptResult<object?> result = await runner.ExecuteAsync(
            new ScriptExecutionRequest(ScriptEngineId.JavaScript, "timeout", "while (true) {}"),
            new ScriptExecutionOptions
            {
                Timeout = TimeSpan.FromMilliseconds(100),
                MaxStatements = 1_000_000,
                MaxRecursionDepth = 32,
                MaxMemoryBytes = 8 * 1024 * 1024
            });

        result.IsSuccess.ShouldBeFalse();
        result.Failure!.Code.ShouldBeOneOf("scripting.execution.timeout", "scripting.javascript.failed");
    }

    [Test]
    public async Task SyntaxDiagnosticsDoNotEchoSource()
    {
        const string secret = "private-source-marker";
        var runner = new ScriptRunner(new ScriptEngineRegistry([new JavaScriptScriptEngineAdapter()]));

        ScriptResult<object?> result = await runner.ExecuteAsync(
            new ScriptExecutionRequest(ScriptEngineId.JavaScript, "syntax", $"function broken( {{ /* {secret} */"));

        result.IsSuccess.ShouldBeFalse();
        result.Failure!.Message.ShouldNotContain(secret);
    }

    [Test]
    public async Task DoesNotRejectWordsInsideCommentsOrStrings()
    {
        var validator = new JavaScriptScriptValidator();
        const string source = "// eval('ignored')\nconst text = \"Function import eval\"; text;";

        ScriptValidationResult result = await validator.ValidateAsync(
            new ScriptExecutionRequest(ScriptEngineId.JavaScript, "test", source),
            new ScriptExecutionOptions());

        result.IsValid.ShouldBeTrue();
    }
}
