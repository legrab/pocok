// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Conversion;

namespace Pocok.Showcase.Conversion.Models;

public enum ConversionSourceKind
{
    Text,
    Integer,
    UnsignedInteger,
    Decimal,
    FloatingPoint,
    Boolean,
    Null,
    TextArray,
    ObjectArray
}

public enum ConversionEditorMode
{
    Fields,
    Code
}

public enum DemoColor
{
    Red = 1,
    Green = 2,
    Blue = 3
}

[Flags]
public enum DemoAccess
{
    None = 0,
    Read = 1,
    Write = 2,
    Execute = 4
}

public sealed record ConversionInput
{
    public string SampleId { get; init; } = string.Empty;
    public ConversionSourceKind SourceKind { get; init; }
    public string SourceValue { get; init; } = string.Empty;
    public string TargetType { get; init; } = "byte";
    public string Culture { get; init; } = "invariant";
    public OverflowPolicy Overflow { get; init; } = OverflowPolicy.Fail;
    public NullPolicy Nulls { get; init; } = NullPolicy.Preserve;
    public EnumPolicy Enums { get; init; } = EnumPolicy.DefinedValuesAndFlags;
    public NumericLossPolicy NumericLoss { get; init; } = NumericLossPolicy.Reject;
    public NumericBooleanPolicy NumericBooleans { get; init; } = NumericBooleanPolicy.Reject;
    public TemporalTextPolicy TemporalText { get; init; } = TemporalTextPolicy.RoundTrip;
    public int MaximumDepth { get; init; } = 32;
    public int MaximumCollectionItems { get; init; } = 10_000;
    public ConversionEditorMode EditorMode { get; init; }
    public string Code { get; init; } = string.Empty;
}

public sealed record ConversionOutput(
    bool IsSuccess,
    string Headline,
    string? Value,
    string TargetType,
    string SourceType,
    string? FailureCode,
    string? FailurePath,
    string? FailureMessage,
    string CodePreview,
    string PolicySummary,
    IReadOnlyList<string> TipKeys);

public sealed record ConversionParseResult(bool IsSuccess, ConversionInput? Input, string? Error)
{
    public static ConversionParseResult Success(ConversionInput input) => new(true, input, null);
    public static ConversionParseResult Failure(string error) => new(false, null, error);
}
