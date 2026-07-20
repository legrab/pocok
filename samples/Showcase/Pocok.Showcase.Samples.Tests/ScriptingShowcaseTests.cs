// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Scripting.CSharp;
using Pocok.Scripting.Execution;
using Pocok.Scripting.JavaScript;
using Pocok.Scripting.Python;
using Pocok.Showcase.Contracts;
using Pocok.Showcase.Scripting;
using Pocok.Showcase.Scripting.Models;
using Shouldly;

namespace Pocok.Showcase.Samples.Tests;

[TestFixture]
public sealed class ScriptingShowcaseTests
{
    private ScriptEngineRegistry _registry = null!;
    private ScriptRunner _runner = null!;
    private ScriptingShowcaseSlice _slice = null!;

    [SetUp]
    public void SetUp()
    {
        _registry = new ScriptEngineRegistry(
        [
            new JavaScriptScriptEngineAdapter(),
            new UnavailableScriptEngineAdapter(
                ScriptEngineId.CSharp,
                "C#",
                "scripting.engine.trusted_only",
                "C# is trusted/local only."),
            new UnavailableScriptEngineAdapter(
                ScriptEngineId.Python,
                "Python",
                "scripting.engine.trusted_only",
                "Python is trusted/local only.")
        ]);
        _runner = new ScriptRunner(_registry);
        _slice = new ScriptingShowcaseSlice(_runner, _registry);
    }

    public static IEnumerable<TestCaseData> Samples()
    {
        ScriptEngineRegistry registry = CreateRegistry();
        var slice = new ScriptingShowcaseSlice(new ScriptRunner(registry), registry);
        foreach (IShowcaseSample sample in slice.Samples)
        {
            yield return new TestCaseData(sample.Id, sample.ExpectedHeadlineResult)
                .SetName($"Sample_{sample.Id}");
        }
    }

    [TestCaseSource(nameof(Samples))]
    public async Task EveryDefaultJavaScriptSampleProducesExpectedHeadline(string id, string expected)
    {
        IShowcaseSample sample = _slice.Samples.Single(item => item.Id == id);

        ShowcaseRunResult result = await TestSupport.ExecuteAsync(_slice, sample.CreateInput());

        result.Headline.ShouldBe(expected);
    }

    [Test]
    public void EveryConceptualSampleContainsAllThreeLanguageVariants()
    {
        foreach (IShowcaseSample sample in _slice.Samples)
        {
            var input = (ScriptingInput)sample.CreateInput();
            input.Sources.Keys.OrderBy(static value => value, StringComparer.Ordinal).ShouldBe(
                ["csharp", "javascript", "python"]);
            input.Sources.Values.ShouldAllBe(static value => !string.IsNullOrWhiteSpace(value));
        }
    }

    [Test]
    public void PublicDescriptorsKeepTrustedEnginesUnavailable()
    {
        _registry.Descriptors.Single(item => item.Id == ScriptEngineId.JavaScript).IsAvailable.ShouldBeTrue();
        _registry.Descriptors.Single(item => item.Id == ScriptEngineId.CSharp).IsAvailable.ShouldBeFalse();
        _registry.Descriptors.Single(item => item.Id == ScriptEngineId.Python).IsAvailable.ShouldBeFalse();
    }

    [Test]
    public void SamplesCreateFreshInputsAndSourceMaps()
    {
        IShowcaseSample sample = _slice.Samples.Single(static item => item.IsDefault);
        var first = (ScriptingInput)sample.CreateInput();
        var second = (ScriptingInput)sample.CreateInput();

        ReferenceEquals(first, second).ShouldBeFalse();
        ReferenceEquals(first.Sources, second.Sources).ShouldBeFalse();
    }

    [Test]
    public async Task OversizedSourceIsRejectedBeforeEngineExecution()
    {
        var input = new ScriptingInput { Source = new string('x', 40_000) };

        ShowcaseRunResult result = await TestSupport.ExecuteAsync(_slice, input);

        result.Status.ShouldBe(ShowcaseRunStatus.Rejected);
        result.Diagnostics.Single().Code.ShouldBe("showcase.script-too-large");
    }

    [Test]
    public async Task ValidatorRejectionRemainsAnExpectedFailure()
    {
        var input = new ScriptingInput { Source = "eval('1');" };

        ShowcaseRunResult result = await TestSupport.ExecuteAsync(_slice, input);

        result.Status.ShouldBe(ShowcaseRunStatus.ExpectedFailure);
        result.Diagnostics.Single().Code.ShouldBe("scripting.javascript.eval");
        result.Diagnostics.Single().Message.ShouldNotContain("Jint.Runtime");
    }

    [Test]
    public void ResultFormattingIsDeterministic()
    {
        ScriptingShowcaseSlice.FormatResult(null).ShouldBe("null");
        ScriptingShowcaseSlice.FormatResult(true).ShouldBe("true");
        ScriptingShowcaseSlice.FormatResult(12.5).ShouldBe("12.5");
        ScriptingShowcaseSlice.FormatResult("hello").ShouldBe("\"hello\"");
    }

    [Test]
    public async Task TrustedLocalAdaptersProduceEquivalentArithmeticWhenConfigured()
    {
        var csharp = new CSharpScriptEngineAdapter();
        var python = new PythonScriptEngineAdapter();
        if (!csharp.Descriptor.IsAvailable || !python.Descriptor.IsAvailable)
        {
            string reason = string.Join("; ", new[]
            {
                csharp.Descriptor.UnavailableMessage,
                python.Descriptor.UnavailableMessage
            }.Where(static value => !string.IsNullOrWhiteSpace(value)));
            Assert.Ignore(string.IsNullOrWhiteSpace(reason)
                ? "Trusted local scripting engines are unavailable."
                : reason);
        }

        var registry = new ScriptEngineRegistry(
        [
            new JavaScriptScriptEngineAdapter(),
            csharp,
            python
        ]);
        var runner = new ScriptRunner(registry);
        var sources = new Dictionary<ScriptEngineId, string>
        {
            [ScriptEngineId.JavaScript] = "21 * 2;",
            [ScriptEngineId.CSharp] = "21 * 2",
            [ScriptEngineId.Python] = "21 * 2"
        };

        foreach ((ScriptEngineId id, string source) in sources)
        {
            ScriptExecutionOptions options = id == ScriptEngineId.JavaScript
                ? new ScriptExecutionOptions
                {
                    Timeout = TimeSpan.FromSeconds(5),
                    MaxStatements = 1_000,
                    MaxRecursionDepth = 32,
                    MaxMemoryBytes = 8 * 1024 * 1024
                }
                : new ScriptExecutionOptions { Timeout = TimeSpan.FromSeconds(5) };
            ScriptResult<int> result = await runner.ExecuteAsync<int>(
                new ScriptExecutionRequest(id, "trusted-showcase-test", source)
                {
                    ExpectResult = true
                },
                options);

            result.IsSuccess.ShouldBeTrue(result.Failure?.Message);
            result.Value.ShouldBe(42);
        }
    }

    [Test]
    public async Task RuntimeWarmupUsesTheExplicitJavaScriptRegistry()
    {
        var warmup = new ScriptingRuntimeWarmupService(_runner);

        await warmup.StartAsync(CancellationToken.None);

        ScriptResult<int> result = await _runner.ExecuteAsync<int>(
            new ScriptExecutionRequest(ScriptEngineId.JavaScript, "interactive", "21 * 2;")
            {
                ExpectResult = true
            },
            new ScriptExecutionOptions
            {
                Timeout = TimeSpan.FromSeconds(1),
                MaxStatements = 1_000,
                MaxRecursionDepth = 32,
                MaxMemoryBytes = 8 * 1024 * 1024
            });
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    private static ScriptEngineRegistry CreateRegistry() => new(
    [
        new JavaScriptScriptEngineAdapter(),
        new UnavailableScriptEngineAdapter(
            ScriptEngineId.CSharp,
            "C#",
            "scripting.engine.trusted_only",
            "C# is trusted/local only."),
        new UnavailableScriptEngineAdapter(
            ScriptEngineId.Python,
            "Python",
            "scripting.engine.trusted_only",
            "Python is trusted/local only.")
    ]);
}
