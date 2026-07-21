// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Showcase.Contracts;
using Pocok.Showcase.Conversion;
using Pocok.Showcase.Conversion.Models;

namespace Pocok.Showcase.Samples.Tests;

[TestFixture]
public sealed class ConversionShowcaseTests
{
    [SetUp]
    public void SetUp()
    {
        _slice = new ConversionShowcaseSlice();
    }

    private static readonly int[] DeterministicValues = [1, 2];
    private ConversionShowcaseSlice _slice = null!;

    public static IEnumerable<TestCaseData> Samples()
    {
        var slice = new ConversionShowcaseSlice();
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
        var first = sample.CreateInput();
        var second = sample.CreateInput();
        ReferenceEquals(first, second).ShouldBeFalse();
    }

    [Test]
    public async Task UntypedBridgeRejectsWrongInput()
    {
        ShowcaseRunResult result = await TestSupport.ExecuteAsync(_slice, new object());
        result.Status.ShouldBe(ShowcaseRunStatus.Rejected);
    }

    [Test]
    public async Task NumericBooleanIsLowercase()
    {
        IShowcaseSample sample = _slice.Samples.Single(item => item.Id == "numeric-boolean");
        ShowcaseRunResult result = await TestSupport.ExecuteAsync(_slice, sample.CreateInput());
        result.Headline.ShouldBe("true");
    }

    [Test]
    public async Task SaturatingSampleIncludesPolicyInCodePreview()
    {
        IShowcaseSample sample = _slice.Samples.Single(item => item.Id == "saturating-byte");
        ShowcaseRunResult result = await TestSupport.ExecuteAsync(_slice, sample.CreateInput());
        result.CodePreview.ShouldNotBeNull();
        result.CodePreview.ShouldContain("OverflowPolicy.Saturate");
    }

    [Test]
    public void SamplesExposeTheirConfiguredEditorValues()
    {
        var strict = (ConversionInput)_slice.Samples.Single(item => item.Id == "strict-integer").CreateInput();
        var german = (ConversionInput)_slice.Samples.Single(item => item.Id == "german-decimal").CreateInput();

        strict.SourceValue.ShouldBe("42");
        strict.TargetType.ShouldBe("int");
        german.SourceValue.ShouldBe("1.234,5");
        german.Culture.ShouldBe("de-DE");
    }

    [Test]
    public async Task EditedSourceValueChangesTheRealConversionOutcome()
    {
        var input = (ConversionInput)_slice.Samples.Single(item => item.Id == "strict-integer").CreateInput();

        ShowcaseRunResult result = await TestSupport.ExecuteAsync(_slice, input with { SourceValue = "43" });

        result.Status.ShouldBe(ShowcaseRunStatus.Success);
        result.Headline.ShouldBe("43");
        result.CodePreview.ShouldNotBeNull().ShouldContain("\"43\"");
    }

    [Test]
    public void ValueFormatterIsDeterministic()
    {
        ConversionShowcaseSlice.FormatValue(true).ShouldBe("true");
        ConversionShowcaseSlice.FormatValue(DeterministicValues).ShouldBe("[1, 2]");
        ConversionShowcaseSlice.FormatValue(null).ShouldBe("null");
    }
}
