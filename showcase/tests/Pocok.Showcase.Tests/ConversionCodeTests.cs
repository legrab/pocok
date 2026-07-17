// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Conversion;
using Pocok.Showcase.Components;
using Pocok.Showcase.Conversion;
using Pocok.Showcase.Conversion.Models;

namespace Pocok.Showcase.Tests;

[TestFixture]
public sealed class ConversionCodeTests
{
    [TestCase("converter.Convert<int>(\"42\");")]
    [TestCase("converter.Convert<int>(\"42\", ConversionContext.Strict);")]
    [TestCase("converter.Convert(\"42\", typeof(int), ConversionContext.Strict);")]
    public void ParserAcceptsApprovedForms(string code)
    {
        ConversionParseResult result = ConversionCodeParser.Parse(code);
        result.IsSuccess.ShouldBeTrue(result.Error);
    }

    [TestCase("System.IO.File.ReadAllText(\"secret\")")]
    [TestCase("converter.Convert<System.Type>(typeof(string))")]
    [TestCase("converter.Convert<int>(GetValue())")]
    [TestCase("converter.Convert<int>(\"42\", new ConversionContext(CultureInfo.InvariantCulture, OverflowPolicy.Saturate))")]
    public void ParserRejectsArbitrarySyntax(string code)
    {
        ConversionCodeParser.Parse(code).IsSuccess.ShouldBeFalse();
    }

    [Test]
    public void ParserRejectsOversizedInput()
    {
        string code = new('x', ConversionCodeParser.MaximumCodeLength + 1);
        ConversionCodeParser.Parse(code).Error!.ShouldContain("8 KiB");
    }

    [TestCase("maximumDepth: 0")]
    [TestCase("maximumDepth: 65")]
    [TestCase("maximumCollectionItems: 501")]
    public void ParserEnforcesBounds(string policy)
    {
        string code = $"converter.Convert<int>(\"42\", new ConversionContext(CultureInfo.InvariantCulture, {policy}));";
        ConversionCodeParser.Parse(code).IsSuccess.ShouldBeFalse();
    }

    [Test]
    public void FormatterRoundTripsApprovedInput()
    {
        var input = new ConversionInput
        {
            SourceKind = ConversionSourceKind.Decimal,
            SourceValue = "12.7",
            TargetType = "int",
            NumericLoss = NumericLossPolicy.RoundToNearest
        };
        string code = ConversionCodeFormatter.Format(input);
        ConversionParseResult parsed = ConversionCodeParser.Parse(code);
        parsed.IsSuccess.ShouldBeTrue(parsed.Error);
        parsed.Input!.NumericLoss.ShouldBe(NumericLossPolicy.RoundToNearest);
    }

    [Test]
    public void LineCommentRemovalPreservesStringContent()
    {
        string code = "converter.Convert<string>(\"https://example.test\"); // trailing";
        ConversionCodeParser.RemoveLineComments(code).ShouldContain("https://example.test");
    }

    [Test]
    public void AutocompleteFiltersAtCursor()
    {
        var slice = new ConversionShowcaseSlice();
        IReadOnlyList<Pocok.Showcase.Contracts.ShowcaseCodeAssistItem> matches =
            ShowcaseCodeAssistFilter.Filter(slice.CodeAssist, "OverflowPolicy.Sat", 18, 8);
        matches.Count.ShouldBeGreaterThan(0);
        matches[0].Label.ShouldContain("Saturate");
    }
}
