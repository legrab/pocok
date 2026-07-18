// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Pocok.Conversion;
using Pocok.Showcase.Conversion.Models;

namespace Pocok.Showcase.Conversion;

public static partial class ConversionCodeParser
{
    public const int MaximumCodeLength = 8 * 1024;

    private static readonly string[] ArrayPrefixes = ["new[]", "new string[]", "new object?[]"];

    private static readonly HashSet<string> TargetAliases = new(StringComparer.Ordinal)
    {
        "bool", "char", "string",
        "sbyte", "byte", "short", "ushort", "int", "uint", "long", "ulong",
        "float", "double", "decimal",
        "Guid", "DateTime", "DateTimeOffset", "DateOnly", "TimeOnly", "TimeSpan",
        "DemoColor", "DemoAccess",
        "bool[]", "int[]", "string[]", "Guid[]", "DateTimeOffset[]"
    };

    public static ConversionParseResult Parse(string code, string sampleId = "code")
    {
        if (code is null)
            return ConversionParseResult.Failure("Code is required.");
        if (code.Length > MaximumCodeLength)
            return ConversionParseResult.Failure("Code exceeds the 8 KiB limit.");

        string cleaned = RemoveLineComments(code).Trim();
        if (cleaned.EndsWith(';'))
            cleaned = cleaned[..^1].TrimEnd();

        Match generic = GenericCall().Match(cleaned);
        string target;
        string arguments;
        if (generic.Success)
        {
            target = generic.Groups["target"].Value.Trim();
            arguments = generic.Groups["args"].Value;
        }
        else
        {
            Match runtime = RuntimeCall().Match(cleaned);
            if (!runtime.Success)
                return ConversionParseResult.Failure("Only approved converter.Convert forms are supported.");
            target = runtime.Groups["target"].Value.Trim();
            arguments = string.Concat(runtime.Groups["value"].Value, ",", runtime.Groups["context"].Value);
        }

        if (!TargetAliases.Contains(target))
            return ConversionParseResult.Failure("The target type is not in the fixed showcase allowlist.");

        IReadOnlyList<string> args = SplitTopLevel(arguments, ',');
        if (args.Count is < 1 or > 2 || args.Any(static argument => argument.Length == 0))
            return ConversionParseResult.Failure("Convert accepts one value and an optional approved context.");
        if (!TryParseValue(args[0], out ConversionSourceKind sourceKind, out string sourceValue, out string? valueError))
            return ConversionParseResult.Failure(valueError!);

        var input = new ConversionInput
        {
            SampleId = sampleId,
            SourceKind = sourceKind,
            SourceValue = sourceValue,
            TargetType = target,
            EditorMode = ConversionEditorMode.Code,
            Code = code
        };
        if (args.Count == 2)
        {
            ConversionParseResult contextResult = ParseContext(args[1], input);
            if (!contextResult.IsSuccess)
                return contextResult;
            input = contextResult.Input! with { Code = code, EditorMode = ConversionEditorMode.Code, SampleId = sampleId };
        }
        return ConversionParseResult.Success(input);
    }

    public static string RemoveLineComments(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        var result = new StringBuilder(code.Length);
        bool inString = false;
        bool escaped = false;
        for (int index = 0; index < code.Length; index++)
        {
            char current = code[index];
            if (inString)
            {
                result.Append(current);
                if (escaped)
                    escaped = false;
                else if (current == '\\')
                    escaped = true;
                else if (current == '"')
                    inString = false;
                continue;
            }
            if (current == '"')
            {
                inString = true;
                result.Append(current);
                continue;
            }
            if (current == '/' && index + 1 < code.Length && code[index + 1] == '/')
            {
                while (index < code.Length && code[index] != '\n')
                    index++;
                if (index < code.Length)
                    result.Append('\n');
                continue;
            }
            result.Append(current);
        }
        return result.ToString();
    }

    private static ConversionParseResult ParseContext(string expression, ConversionInput baseline)
    {
        string trimmed = expression.Trim();
        if (trimmed == "ConversionContext.Strict")
            return ConversionParseResult.Success(baseline);

        const string prefix = "new ConversionContext(";
        if (!trimmed.StartsWith(prefix, StringComparison.Ordinal) || !trimmed.EndsWith(')'))
            return ConversionParseResult.Failure("Only ConversionContext.Strict or an approved constructor is supported.");

        IReadOnlyList<string> arguments = SplitTopLevel(trimmed[prefix.Length..^1], ',');
        if (arguments.Count == 0 || arguments.Any(static argument => argument.Length == 0)
            || !TryParseCulture(arguments[0], out string culture))
            return ConversionParseResult.Failure("Use an approved invariant, English, German, or Hungarian culture expression.");

        ConversionInput input = baseline with { Culture = culture };
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 1; index < arguments.Count; index++)
        {
            string item = arguments[index].Trim();
            int colon = item.IndexOf(':');
            if (colon <= 0)
                return ConversionParseResult.Failure("Policy arguments after culture must be named.");
            string name = item[..colon].Trim();
            string value = item[(colon + 1)..].Trim();
            if (!seen.Add(name))
                return ConversionParseResult.Failure($"Policy argument '{name}' is duplicated.");

            try
            {
                input = name switch
                {
                    "overflow" => input with { Overflow = ParseEnum<OverflowPolicy>(value, nameof(OverflowPolicy)) },
                    "nulls" => input with { Nulls = ParseEnum<NullPolicy>(value, nameof(NullPolicy)) },
                    "enums" => input with { Enums = ParseEnum<EnumPolicy>(value, nameof(EnumPolicy)) },
                    "numericLoss" => input with { NumericLoss = ParseEnum<NumericLossPolicy>(value, nameof(NumericLossPolicy)) },
                    "numericBooleans" => input with { NumericBooleans = ParseEnum<NumericBooleanPolicy>(value, nameof(NumericBooleanPolicy)) },
                    "temporalText" => input with { TemporalText = ParseEnum<TemporalTextPolicy>(value, nameof(TemporalTextPolicy)) },
                    "maximumDepth" => input with { MaximumDepth = ParseBoundedInt(value, 1, 64, name) },
                    "maximumCollectionItems" => input with { MaximumCollectionItems = ParseBoundedInt(value, 1, 500, name) },
                    _ => throw new FormatException($"Policy argument '{name}' is not supported.")
                };
            }
            catch (FormatException exception)
            {
                return ConversionParseResult.Failure(exception.Message);
            }
        }
        return ConversionParseResult.Success(input);
    }

    private static T ParseEnum<T>(string expression, string typeName)
        where T : struct, Enum
    {
        string prefix = typeName + ".";
        if (!expression.StartsWith(prefix, StringComparison.Ordinal)
            || !Enum.TryParse(expression[prefix.Length..], false, out T value)
            || !Enum.IsDefined(value))
            throw new FormatException($"Invalid {typeName} value.");
        return value;
    }

    private static int ParseBoundedInt(string expression, int minimum, int maximum, string name)
    {
        if (!int.TryParse(expression, NumberStyles.None, CultureInfo.InvariantCulture, out int value)
            || value < minimum || value > maximum)
            throw new FormatException($"{name} must be between {minimum} and {maximum}.");
        return value;
    }

    private static bool TryParseCulture(string expression, out string culture)
    {
        string normalized = expression.Trim();
        if (normalized == "CultureInfo.InvariantCulture")
        {
            culture = "invariant";
            return true;
        }

        Match match = CultureCall().Match(normalized);
        if (match.Success)
        {
            culture = match.Groups["culture"].Value switch
            {
                "en" or "en-US" => "en-US",
                "de" or "de-DE" => "de-DE",
                "hu" or "hu-HU" => "hu-HU",
                _ => string.Empty
            };
            return culture.Length > 0;
        }
        culture = string.Empty;
        return false;
    }

    private static bool TryParseValue(
        string expression,
        out ConversionSourceKind kind,
        out string value,
        out string? error)
    {
        string text = expression.Trim();
        if (text == "null")
        {
            kind = ConversionSourceKind.Null;
            value = string.Empty;
            error = null;
            return true;
        }
        if (text is "true" or "false")
        {
            kind = ConversionSourceKind.Boolean;
            value = text;
            error = null;
            return true;
        }
        if (text.StartsWith('"') && text.EndsWith('"'))
        {
            try
            {
                value = JsonSerializer.Deserialize<string>(text) ?? string.Empty;
                kind = ConversionSourceKind.Text;
                error = null;
                return true;
            }
            catch (JsonException)
            {
                kind = default;
                value = string.Empty;
                error = "String literals must be JSON-compatible.";
                return false;
            }
        }
        if (TryParseArray(text, out kind, out value))
        {
            error = null;
            return true;
        }
        if (TryStripSuffix(text, "UL", out string unsignedText)
            && ulong.TryParse(unsignedText, NumberStyles.None, CultureInfo.InvariantCulture, out _))
        {
            kind = ConversionSourceKind.UnsignedInteger;
            value = unsignedText;
            error = null;
            return true;
        }
        if (TryStripSuffix(text, "M", out string decimalText)
            && decimal.TryParse(decimalText, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            kind = ConversionSourceKind.Decimal;
            value = decimalText;
            error = null;
            return true;
        }
        if ((TryStripSuffix(text, "D", out string floatingText) || TryStripSuffix(text, "F", out floatingText))
            && double.TryParse(floatingText, NumberStyles.Float, CultureInfo.InvariantCulture, out double suffixedFloating)
            && double.IsFinite(suffixedFloating))
        {
            kind = ConversionSourceKind.FloatingPoint;
            value = floatingText;
            error = null;
            return true;
        }
        if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _))
        {
            kind = ConversionSourceKind.Integer;
            value = text;
            error = null;
            return true;
        }
        if (ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out _))
        {
            kind = ConversionSourceKind.UnsignedInteger;
            value = text;
            error = null;
            return true;
        }
        if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            kind = ConversionSourceKind.Decimal;
            value = text;
            error = null;
            return true;
        }
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double floating)
            && double.IsFinite(floating))
        {
            kind = ConversionSourceKind.FloatingPoint;
            value = text;
            error = null;
            return true;
        }

        kind = default;
        value = string.Empty;
        error = "Only null, strings, booleans, bounded numbers, and supported arrays are accepted.";
        return false;
    }

    private static bool TryParseArray(string expression, out ConversionSourceKind kind, out string json)
    {
        string body;
        string? prefix = ArrayPrefixes.FirstOrDefault(candidate => expression.StartsWith(candidate, StringComparison.Ordinal));
        if (prefix is not null)
        {
            string remainder = expression[prefix.Length..].TrimStart();
            if (!remainder.StartsWith('{') || !remainder.EndsWith('}'))
            {
                kind = default;
                json = string.Empty;
                return false;
            }
            body = remainder[1..^1];
        }
        else if (expression.StartsWith('[') && expression.EndsWith(']'))
        {
            body = expression[1..^1];
        }
        else
        {
            kind = default;
            json = string.Empty;
            return false;
        }

        IReadOnlyList<string> items = SplitTopLevel(body, ',');
        if (items.Count > 500)
        {
            kind = default;
            json = string.Empty;
            return false;
        }

        var values = new List<object?>(items.Count);
        bool stringsOnly = true;
        foreach (string item in items)
        {
            string trimmed = item.Trim();
            if (trimmed.Length == 0)
            {
                if (items.Count == 1)
                    continue;
                kind = default;
                json = string.Empty;
                return false;
            }
            if (!TryParseArrayItem(trimmed, out object? parsed, out bool isString))
            {
                kind = default;
                json = string.Empty;
                return false;
            }
            values.Add(parsed);
            stringsOnly &= isString;
        }

        kind = stringsOnly ? ConversionSourceKind.TextArray : ConversionSourceKind.ObjectArray;
        json = JsonSerializer.Serialize(values);
        return true;
    }

    private static bool TryStripSuffix(string text, string suffix, out string value)
    {
        if (text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) && text.Length > suffix.Length)
        {
            value = text[..^suffix.Length];
            return true;
        }
        value = string.Empty;
        return false;
    }

    private static bool TryParseArrayItem(string text, out object? value, out bool isString)
    {
        if (text == "null")
        {
            value = null;
            isString = false;
            return true;
        }
        if (text is "true" or "false")
        {
            value = text == "true";
            isString = false;
            return true;
        }
        if (text.StartsWith('"') && text.EndsWith('"'))
        {
            try
            {
                value = JsonSerializer.Deserialize<string>(text) ?? string.Empty;
                isString = true;
                return true;
            }
            catch (JsonException)
            {
                value = null;
                isString = false;
                return false;
            }
        }
        if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long signed))
        {
            value = signed;
            isString = false;
            return true;
        }
        if (ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out ulong unsigned))
        {
            value = unsigned;
            isString = false;
            return true;
        }
        if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal number))
        {
            value = number;
            isString = false;
            return true;
        }
        value = null;
        isString = false;
        return false;
    }

    internal static IReadOnlyList<string> SplitTopLevel(string value, char separator)
    {
        var result = new List<string>();
        int start = 0;
        int depth = 0;
        bool inString = false;
        bool escaped = false;
        for (int index = 0; index < value.Length; index++)
        {
            char current = value[index];
            if (inString)
            {
                if (escaped)
                    escaped = false;
                else if (current == '\\')
                    escaped = true;
                else if (current == '"')
                    inString = false;
                continue;
            }
            if (current == '"')
            {
                inString = true;
                continue;
            }
            if (current is '(' or '[' or '{')
                depth++;
            else if (current is ')' or ']' or '}')
                depth--;
            else if (current == separator && depth == 0)
            {
                result.Add(value[start..index].Trim());
                start = index + 1;
            }
            if (depth < 0)
                return [];
        }
        if (inString || depth != 0)
            return [];
        result.Add(value[start..].Trim());
        return result;
    }

    [GeneratedRegex(@"^converter\.Convert<(?<target>[A-Za-z0-9_\[\]]+)>\((?<args>.*)\)$", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex GenericCall();

    [GeneratedRegex(@"^converter\.Convert\((?<value>.*),\s*typeof\((?<target>[A-Za-z0-9_\[\]]+)\),\s*(?<context>.*)\)$", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RuntimeCall();

    [GeneratedRegex(@"^(?:CultureInfo\.GetCultureInfo|new CultureInfo)\(""(?<culture>[A-Za-z-]+)""\)$", RegexOptions.CultureInvariant)]
    private static partial Regex CultureCall();
}
