// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Showcase.Contracts;
using Pocok.Showcase.Conversion;
using Pocok.Showcase.Conversion.Models;

namespace Pocok.Showcase.Tests;

[TestFixture]
public sealed class ConversionShowcaseTests
{
    private static readonly int[] DeterministicValues = [1, 2];
    private ConversionShowcaseSlice _slice = null!;

    [SetUp]
    public void SetUp() => _slice = new ConversionShowcaseSlice();

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
        object first = sample.CreateInput();
        object second = sample.CreateInput();
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
    public void ValueFormatterIsDeterministic()
    {
        ConversionShowcaseSlice.FormatValue(true).ShouldBe("true");
        ConversionShowcaseSlice.FormatValue(DeterministicValues).ShouldBe("[1, 2]");
        ConversionShowcaseSlice.FormatValue(null).ShouldBe("null");
    }
}
