// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using System.Text.Json;
using Pocok.Conversion;
using Pocok.Showcase.Conversion.Models;

namespace Pocok.Showcase.Conversion;

public static class ConversionCodeFormatter
{
    public static string Format(ConversionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        string value = FormatValue(input);
        var lines = new List<string>
        {
            $"converter.Convert<{input.TargetType}>(",
            $"    {value},"
        };

        if (IsStrict(input))
        {
            lines.Add("    ConversionContext.Strict");
        }
        else
        {
            lines.Add("    new ConversionContext(");
            List<string> contextArguments = ContextArguments(input);
            for (int index = 0; index < contextArguments.Count; index++)
            {
                string suffix = index == contextArguments.Count - 1 ? string.Empty : ",";
                lines.Add($"        {contextArguments[index]}{suffix}");
            }
            lines.Add("    )");
        }

        lines.Add(");");
        return string.Join('\n', lines);
    }

    public static string Format(string code)
    {
        ConversionParseResult parsed = ConversionCodeParser.Parse(code);
        if (!parsed.IsSuccess)
            throw new FormatException(parsed.Error);

        return Format(parsed.Input!);
    }

    public static string PolicySummary(ConversionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return string.Join(", ",
            $"culture={input.Culture}",
            $"overflow={input.Overflow}",
            $"nulls={input.Nulls}",
            $"enums={input.Enums}",
            $"numericLoss={input.NumericLoss}",
            $"numericBooleans={input.NumericBooleans}",
            $"temporalText={input.TemporalText}",
            $"depth={input.MaximumDepth.ToString(CultureInfo.InvariantCulture)}",
            $"items={input.MaximumCollectionItems.ToString(CultureInfo.InvariantCulture)}");
    }

    private static bool IsStrict(ConversionInput input) =>
        input.Culture == "invariant"
        && input.Overflow == OverflowPolicy.Fail
        && input.Nulls == NullPolicy.Preserve
        && input.Enums == EnumPolicy.DefinedValuesAndFlags
        && input.NumericLoss == NumericLossPolicy.Reject
        && input.NumericBooleans == NumericBooleanPolicy.Reject
        && input.TemporalText == TemporalTextPolicy.RoundTrip
        && input.MaximumDepth == 32
        && input.MaximumCollectionItems == 10_000;

    private static List<string> ContextArguments(ConversionInput input)
    {
        var arguments = new List<string>
        {
            FormatCulture(input.Culture),
            $"overflow: OverflowPolicy.{input.Overflow}",
            $"nulls: NullPolicy.{input.Nulls}",
            $"enums: EnumPolicy.{input.Enums}",
            $"numericLoss: NumericLossPolicy.{input.NumericLoss}",
            $"numericBooleans: NumericBooleanPolicy.{input.NumericBooleans}",
            $"temporalText: TemporalTextPolicy.{input.TemporalText}",
            $"maximumDepth: {input.MaximumDepth.ToString(CultureInfo.InvariantCulture)}"
        };
        if (input.MaximumCollectionItems != 10_000)
        {
            arguments.Add(
                $"maximumCollectionItems: {input.MaximumCollectionItems.ToString(CultureInfo.InvariantCulture)}");
        }

        return arguments;
    }

    private static string FormatValue(ConversionInput input) => input.SourceKind switch
    {
        ConversionSourceKind.Null => "null",
        ConversionSourceKind.Boolean => bool.TryParse(input.SourceValue, out bool boolean) && boolean ? "true" : "false",
        ConversionSourceKind.Integer => input.SourceValue,
        ConversionSourceKind.UnsignedInteger => input.SourceValue + "UL",
        ConversionSourceKind.Decimal => input.SourceValue + "m",
        ConversionSourceKind.FloatingPoint => input.SourceValue + "d",
        ConversionSourceKind.TextArray => FormatArray(input.SourceValue, false),
        ConversionSourceKind.ObjectArray => FormatArray(input.SourceValue, true),
        _ => JsonSerializer.Serialize(input.SourceValue)
    };

    private static string FormatArray(string value, bool objectArray)
    {
        try
        {
            JsonElement[] items = JsonSerializer.Deserialize<JsonElement[]>(value) ?? [];
            string values = string.Join(", ", items.Select(FormatArrayItem));
            return objectArray ? $"new object?[] {{ {values} }}" : $"new[] {{ {values} }}";
        }
        catch (JsonException)
        {
            string[] items = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return $"new[] {{ {string.Join(", ", items.Select(static item => JsonSerializer.Serialize(item)))} }}";
        }
    }

    private static string FormatArrayItem(JsonElement item) => item.ValueKind switch
    {
        JsonValueKind.String => JsonSerializer.Serialize(item.GetString() ?? string.Empty),
        JsonValueKind.Number => item.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => "null",
        _ => throw new FormatException("Only scalar array values are supported.")
    };

    private static string FormatCulture(string culture) => culture switch
    {
        "invariant" => "CultureInfo.InvariantCulture",
        "en" or "en-US" => "CultureInfo.GetCultureInfo(\"en-US\")",
        "de" or "de-DE" => "CultureInfo.GetCultureInfo(\"de-DE\")",
        "hu" or "hu-HU" => "CultureInfo.GetCultureInfo(\"hu-HU\")",
        _ => throw new ArgumentOutOfRangeException(nameof(culture), culture, "Unsupported showcase culture.")
    };
}
