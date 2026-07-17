// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Scripting.Execution;
using Pocok.Showcase.Contracts;
using Pocok.Showcase.Scripting;
using Pocok.Showcase.Scripting.Models;

namespace Pocok.Showcase.Tests;

[TestFixture]
public sealed class ScriptingShowcaseTests
{
    private ScriptingShowcaseSlice _slice = null!;

    [SetUp]
    public void SetUp() => _slice = new ScriptingShowcaseSlice(new ScriptRunner());

    public static IEnumerable<TestCaseData> Samples()
    {
        var slice = new ScriptingShowcaseSlice(new ScriptRunner());
        foreach (IShowcaseSample sample in slice.Samples)
            yield return new TestCaseData(sample.Id, sample.ExpectedHeadlineResult).SetName($"Sample_{sample.Id}");
    }

    [TestCaseSource(nameof(Samples))]
    public async Task EverySampleProducesExpectedHeadline(string id, string expected)
    {
        IShowcaseSample sample = _slice.Samples.Single(item => item.Id == id);
        ShowcaseRunResult result = await TestSupport.ExecuteAsync(_slice, sample.CreateInput());
        result.Headline.ShouldBe(expected);
    }

    [Test]
    public void SamplesCreateFreshInputs()
    {
        IShowcaseSample sample = _slice.Samples.Single(item => item.IsDefault);
        object first = sample.CreateInput();
        object second = sample.CreateInput();
        ReferenceEquals(first, second).ShouldBeFalse();
    }

    [Test]
    public async Task CompleteScriptReturnsItsFinalExpression()
    {
        var input = new ScriptingInput
        {
            Script = "function add(left, right) { return left + right; }\nadd(20, 22);"
        };

        ShowcaseRunResult result = await TestSupport.ExecuteAsync(_slice, input);

        result.Status.ShouldBe(ShowcaseRunStatus.Success);
        result.Headline.ShouldBe("42");
        result.Fields.Single(field => field.Name == "Result.Fields.ReturnValue").Value.ShouldBe("42");
        result.CodePreview.ShouldBe(input.Script);
    }

    [Test]
    public async Task MissingResultCanBeAllowedExplicitly()
    {
        var input = new ScriptingInput
        {
            Script = "const message = 'done';",
            ExpectResult = false
        };

        ShowcaseRunResult result = await TestSupport.ExecuteAsync(_slice, input);

        result.Status.ShouldBe(ShowcaseRunStatus.Success);
        result.Headline.ShouldBe("Completed without a required return value");
        result.Fields.Single(field => field.Name == "Result.Fields.ReturnValue").Value.ShouldBe("null");
    }

    [Test]
    public async Task JavaScriptFailureRemainsExpectedFailure()
    {
        var input = new ScriptingInput { Script = "function broken( {" };

        ShowcaseRunResult result = await TestSupport.ExecuteAsync(_slice, input);

        result.Status.ShouldBe(ShowcaseRunStatus.ExpectedFailure);
        result.Diagnostics.Single().Code.ShouldStartWith("scripting.");
        result.Diagnostics.Single().Message.ShouldNotContain("Jint.Runtime");
    }

    [Test]
    public async Task OversizedScriptIsRejectedBeforeJintExecution()
    {
        var input = new ScriptingInput { Script = new string('x', 40_000) };

        ShowcaseRunResult result = await TestSupport.ExecuteAsync(_slice, input);

        result.Status.ShouldBe(ShowcaseRunStatus.Rejected);
        result.Diagnostics.Single().Code.ShouldBe("showcase.script-too-large");
    }

    [Test]
    public void ResultFormattingIsDeterministic()
    {
        ScriptingShowcaseSlice.FormatResult(null).ShouldBe("null");
        ScriptingShowcaseSlice.FormatResult(true).ShouldBe("true");
        ScriptingShowcaseSlice.FormatResult(12.5).ShouldBe("12.5");
        ScriptingShowcaseSlice.FormatResult("hello").ShouldBe("\"hello\"");
    }
}
