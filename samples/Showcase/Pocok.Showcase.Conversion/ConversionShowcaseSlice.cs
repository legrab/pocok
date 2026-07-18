// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Pocok.Conversion;
using Pocok.Showcase.Contracts;
using Pocok.Showcase.Conversion.Models;

namespace Pocok.Showcase.Conversion;

public sealed class ConversionShowcaseSlice : ShowcaseSlice<ConversionInput, ConversionOutput>
{
    private static readonly IReadOnlyDictionary<string, Type> TargetTypes = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        ["bool"] = typeof(bool),
        ["char"] = typeof(char),
        ["string"] = typeof(string),
        ["sbyte"] = typeof(sbyte),
        ["byte"] = typeof(byte),
        ["short"] = typeof(short),
        ["ushort"] = typeof(ushort),
        ["int"] = typeof(int),
        ["uint"] = typeof(uint),
        ["long"] = typeof(long),
        ["ulong"] = typeof(ulong),
        ["float"] = typeof(float),
        ["double"] = typeof(double),
        ["decimal"] = typeof(decimal),
        ["Guid"] = typeof(Guid),
        ["DateTime"] = typeof(DateTime),
        ["DateTimeOffset"] = typeof(DateTimeOffset),
        ["DateOnly"] = typeof(DateOnly),
        ["TimeOnly"] = typeof(TimeOnly),
        ["TimeSpan"] = typeof(TimeSpan),
        ["DemoColor"] = typeof(DemoColor),
        ["DemoAccess"] = typeof(DemoAccess),
        ["bool[]"] = typeof(bool[]),
        ["int[]"] = typeof(int[]),
        ["string[]"] = typeof(string[]),
        ["Guid[]"] = typeof(Guid[]),
        ["DateTimeOffset[]"] = typeof(DateTimeOffset[])
    };

    private static readonly IReadOnlyList<ShowcaseSample<ConversionInput>> SampleCatalog = CreateSamples();
    private static readonly ShowcaseGuide GuideCatalog = CreateGuide();
    private static readonly ShowcaseCodeAssistCatalog AssistCatalog = CreateCodeAssist();

    public static IReadOnlyList<string> TargetAliases { get; } = TargetTypes.Keys.ToArray();

    public override ShowcaseSliceDescriptor Descriptor { get; } = new(
        "pocok.showcase.conversion",
        "Pocok.Conversion",
        "conversion",
        "Capability",
        "Active",
        "Package.Name",
        "Package.Summary",
        1,
        "src/Conversion/README.md",
        true,
        ShowcaseImplementationStatus.Available,
        "conversion",
        "1.0.0");

    public override Type PageComponentType => typeof(ConversionPage);
    public override IReadOnlyList<ShowcaseSample<ConversionInput>> TypedSamples => SampleCatalog;
    public override ShowcaseGuide Guide => GuideCatalog;
    public override ShowcaseCodeAssistCatalog CodeAssist => AssistCatalog;

    public override async ValueTask<ConversionOutput> ExecuteAsync(
        ConversionInput input,
        ShowcaseExecutionContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        await context.Progress.ReportAsync("validate", "Validating constrained conversion input.", cancellationToken).ConfigureAwait(false);

        ConversionInput effective = input;
        if (input.EditorMode == ConversionEditorMode.Code)
        {
            ConversionParseResult parse = ConversionCodeParser.Parse(input.Code, input.SampleId);
            if (!parse.IsSuccess)
                return Failure(input, "Code rejected", "showcase.code-rejected", "$", parse.Error ?? "Code was rejected.", ["Tips.Parser"]);
            effective = parse.Input!;
        }

        if (!TargetTypes.TryGetValue(effective.TargetType, out Type? targetType))
            return Failure(effective, "Target rejected", "showcase.target", "$", "The target alias is not approved.", ["Tips.Allowlist"]);

        bool collectionLimitValid = effective.MaximumCollectionItems == 10_000
            || effective.MaximumCollectionItems is >= 1 and <= 500;
        if (effective.MaximumDepth is < 1 or > 64 || !collectionLimitValid)
            return Failure(effective, "Bounds rejected", "showcase.bounds", "$",
                "Depth must be 1..64 and an explicit collection limit must be 1..500.", ["Tips.Bounds"]);

        if (Encoding.UTF8.GetByteCount(effective.SourceValue) > 65_536)
            return Failure(effective, "Source rejected", "showcase.input-limit", "$", "The source value exceeds the 64 KiB budget.", ["Tips.Bounds"]);

        object? value;
        try
        {
            value = ParseSource(effective);
        }
        catch (Exception exception) when (exception is FormatException or JsonException or OverflowException)
        {
            return Failure(effective, "Source rejected", "showcase.source", "$", exception.Message, ["Tips.Source"]);
        }

        CultureInfo culture;
        try
        {
            culture = CreateCulture(effective.Culture);
        }
        catch (FormatException exception)
        {
            return Failure(effective, "Culture rejected", "showcase.culture", "$", exception.Message, ["Tips.Culture"]);
        }

        var conversionContext = new ConversionContext(
            culture,
            effective.Overflow,
            effective.Nulls,
            effective.Enums,
            effective.NumericLoss,
            effective.NumericBooleans,
            effective.TemporalText,
            effective.MaximumDepth,
            effective.MaximumCollectionItems);

        await context.Progress.ReportAsync("execute", "Calling ValueConverter.Default.", cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
#pragma warning disable IL2026
        ConversionResult<object?> result = ValueConverter.Default.Convert(value, targetType, conversionContext);
#pragma warning restore IL2026
        string preview = ConversionCodeFormatter.Format(effective);
        string policySummary = ConversionCodeFormatter.PolicySummary(effective);
        if (result.IsFailure)
        {
            ConversionFailure failure = result.Error!;
            return new ConversionOutput(
                false,
                "Expected conversion failure",
                null,
                effective.TargetType,
                value?.GetType().Name ?? "null",
                failure.Code,
                failure.Path,
                failure.Message,
                preview,
                policySummary,
                TipsFor(effective, failure.Code));
        }

        string formatted = FormatValue(result.Value);
        context.Output.Write(formatted);
        await context.Progress.ReportAsync("complete", "Conversion completed.", cancellationToken).ConfigureAwait(false);
        return new ConversionOutput(
            true,
            formatted,
            formatted,
            effective.TargetType,
            value?.GetType().Name ?? "null",
            null,
            null,
            null,
            preview,
            policySummary,
            TipsFor(effective, null));
    }

    protected override ShowcaseRunResult CreateRunResult(ConversionOutput output, TimeSpan elapsed)
    {
        var fields = new List<ShowcaseResultField>
        {
            new("Result.Fields.Target", output.TargetType, false, true),
            new("Result.Fields.SourceType", output.SourceType, false, true),
            new("Result.Fields.Policies", output.PolicySummary, false, true)
        };
        if (output.IsSuccess)
            fields.Insert(0, new ShowcaseResultField("Result.Fields.Value", output.Value, true, true));
        if (!string.IsNullOrWhiteSpace(output.FailurePath))
            fields.Add(new ShowcaseResultField("Result.Fields.FailurePath", output.FailurePath, true, true));

        if (!output.IsSuccess)
        {
            ShowcaseRunStatus status = output.FailureCode?.StartsWith("showcase.", StringComparison.Ordinal) == true
                ? ShowcaseRunStatus.Rejected
                : ShowcaseRunStatus.ExpectedFailure;
            return new ShowcaseRunResult(
                status,
                output.Headline,
                fields,
                diagnostics: [new ShowcaseDiagnostic(output.FailureCode ?? "conversion.failure", output.FailureMessage ?? "Conversion failed.", "warning")],
                codePreview: output.CodePreview,
                elapsed: elapsed,
                tipKeys: output.TipKeys);
        }

        return new ShowcaseRunResult(
            ShowcaseRunStatus.Success,
            output.Headline,
            fields,
            [new ShowcaseTimelineEvent(DateTimeOffset.UtcNow, "conversion", "ValueConverter returned a successful result.")],
            codePreview: output.CodePreview,
            elapsed: elapsed,
            tipKeys: output.TipKeys);
    }

    private static ConversionOutput Failure(
        ConversionInput input,
        string headline,
        string code,
        string path,
        string message,
        IReadOnlyList<string> tips) => new(
            false,
            headline,
            null,
            input.TargetType,
            input.SourceKind.ToString(),
            code,
            path,
            message,
            SafePreview(input),
            ConversionCodeFormatter.PolicySummary(input),
            tips);

    private static string SafePreview(ConversionInput input)
    {
        try
        {
            return input.EditorMode == ConversionEditorMode.Code ? input.Code : ConversionCodeFormatter.Format(input);
        }
        catch (Exception exception) when (exception is FormatException or ArgumentOutOfRangeException)
        {
            return "// Code preview unavailable because the structured input is invalid.";
        }
    }

    private static object? ParseSource(ConversionInput input) => input.SourceKind switch
    {
        ConversionSourceKind.Text => input.SourceValue,
        ConversionSourceKind.Integer => long.Parse(input.SourceValue, NumberStyles.Integer, CultureInfo.InvariantCulture),
        ConversionSourceKind.UnsignedInteger => ulong.Parse(input.SourceValue, NumberStyles.Integer, CultureInfo.InvariantCulture),
        ConversionSourceKind.Decimal => decimal.Parse(input.SourceValue, NumberStyles.Float, CultureInfo.InvariantCulture),
        ConversionSourceKind.FloatingPoint => ParseFiniteDouble(input.SourceValue),
        ConversionSourceKind.Boolean => bool.Parse(input.SourceValue),
        ConversionSourceKind.Null => null,
        ConversionSourceKind.TextArray => JsonSerializer.Deserialize<string[]>(input.SourceValue)
                                          ?? throw new FormatException("The array cannot be null."),
        ConversionSourceKind.ObjectArray => ParseObjectArray(input.SourceValue),
        _ => throw new ArgumentOutOfRangeException(nameof(input), input.SourceKind, "Unsupported source kind.")
    };

    private static double ParseFiniteDouble(string value)
    {
        double result = double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        if (!double.IsFinite(result))
            throw new FormatException("Floating-point input must be finite.");
        return result;
    }

    private static object?[] ParseObjectArray(string value)
    {
        JsonElement[] elements = JsonSerializer.Deserialize<JsonElement[]>(value)
                                 ?? throw new FormatException("The array cannot be null.");
        return elements.Select(ParseObjectArrayItem).ToArray();
    }

    private static object? ParseObjectArrayItem(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Number when element.TryGetInt64(out long signed) => signed,
        JsonValueKind.Number when element.TryGetUInt64(out ulong unsigned) => unsigned,
        JsonValueKind.Number when element.TryGetDecimal(out decimal number) => number,
        JsonValueKind.Number => element.GetDouble(),
        _ => throw new FormatException("Object arrays may contain only scalar values.")
    };

    private static CultureInfo CreateCulture(string name) => name switch
    {
        "invariant" => CultureInfo.InvariantCulture,
        "en" or "en-US" => CultureInfo.GetCultureInfo("en-US"),
        "de" or "de-DE" => CultureInfo.GetCultureInfo("de-DE"),
        "hu" or "hu-HU" => CultureInfo.GetCultureInfo("hu-HU"),
        _ => throw new FormatException("Unsupported culture.")
    };

    public static string FormatValue(object? value)
    {
        if (value is null)
            return "null";
        if (value is bool boolean)
            return boolean ? "true" : "false";
        if (value is DateTime dateTime)
            return dateTime.ToString("O", CultureInfo.InvariantCulture);
        if (value is DateTimeOffset dateTimeOffset)
            return dateTimeOffset.ToString("O", CultureInfo.InvariantCulture);
        if (value is DateOnly dateOnly)
            return dateOnly.ToString("O", CultureInfo.InvariantCulture);
        if (value is TimeOnly timeOnly)
            return timeOnly.ToString("O", CultureInfo.InvariantCulture);
        if (value is TimeSpan timeSpan)
            return timeSpan.ToString("c", CultureInfo.InvariantCulture);
        if (value is string text)
            return text;
        if (value is IEnumerable enumerable)
        {
            var items = new List<string>();
            foreach (object? item in enumerable)
                items.Add(FormatValue(item));
            return $"[{string.Join(", ", items)}]";
        }
        return value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty
            : value.ToString() ?? string.Empty;
    }

    private static IReadOnlyList<ShowcaseSample<ConversionInput>> CreateSamples() =>
    [
        Sample("strict-integer", "42", ConversionSourceKind.Text, "int", "42"),
        Sample("saturating-byte", "300", ConversionSourceKind.Integer, "byte", "255", isDefault: true, overflow: OverflowPolicy.Saturate),
        Sample("german-decimal", "1.234,5", ConversionSourceKind.Text, "decimal", "1234.5", culture: "de-DE"),
        Sample("flags", "Read, Write", ConversionSourceKind.Text, "DemoAccess", "Read, Write"),
        Sample("collection-success", "[\"1\",\"2\",\"3\"]", ConversionSourceKind.TextArray, "int[]", "[1, 2, 3]"),
        Sample("collection-failure", "[\"1\",\"2\",\"bad\"]", ConversionSourceKind.TextArray, "int[]", "Expected conversion failure"),
        Sample("fraction-rejected", "12.7", ConversionSourceKind.Decimal, "int", "Expected conversion failure"),
        Sample("fraction-rounded", "12.7", ConversionSourceKind.Decimal, "int", "13", numericLoss: NumericLossPolicy.RoundToNearest),
        Sample("numeric-boolean", "1", ConversionSourceKind.Integer, "bool", "true", numericBooleans: NumericBooleanPolicy.ZeroOrOne),
        Sample("null-default", string.Empty, ConversionSourceKind.Null, "int", "0", nulls: NullPolicy.UseDefault),
        Sample("temporal-roundtrip", "2026-07-17T12:34:56.0000000+02:00", ConversionSourceKind.Text, "DateTimeOffset", "2026-07-17T12:34:56.0000000+02:00"),
        Sample("undefined-enum", "9", ConversionSourceKind.Integer, "DemoColor", "Expected conversion failure")
    ];

    private static ShowcaseSample<ConversionInput> Sample(
        string id,
        string value,
        ConversionSourceKind kind,
        string target,
        string expected,
        bool isDefault = false,
        string culture = "invariant",
        OverflowPolicy overflow = OverflowPolicy.Fail,
        NullPolicy nulls = NullPolicy.Preserve,
        NumericLossPolicy numericLoss = NumericLossPolicy.Reject,
        NumericBooleanPolicy numericBooleans = NumericBooleanPolicy.Reject)
    {
        return new ShowcaseSample<ConversionInput>(
            id,
            $"Samples.{id}.Name",
            $"Samples.{id}.Description",
            () =>
            {
                var input = new ConversionInput
                {
                    SampleId = id,
                    SourceKind = kind,
                    SourceValue = value,
                    TargetType = target,
                    Culture = culture,
                    Overflow = overflow,
                    Nulls = nulls,
                    NumericLoss = numericLoss,
                    NumericBooleans = numericBooleans
                };
                return input with { Code = ConversionCodeFormatter.Format(input) };
            },
            isDefault,
            expected,
            "quick-start",
            "basic");
    }

    private static string[] TipsFor(ConversionInput input, string? failureCode)
    {
        var tips = new List<string> { "Tips.Strict" };
        if (input.Overflow == OverflowPolicy.Saturate)
            tips.Add("Tips.Saturation");
        if (input.Culture != "invariant")
            tips.Add("Tips.Culture");
        if (input.NumericBooleans != NumericBooleanPolicy.Reject)
            tips.Add("Tips.NumericBoolean");
        if (input.SourceKind is ConversionSourceKind.TextArray or ConversionSourceKind.ObjectArray)
            tips.Add("Tips.Collection");
        if (!string.IsNullOrWhiteSpace(failureCode))
            tips.Add("Tips.FailurePath");
        tips.Add("Tips.NoSerializer");
        return tips.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static ShowcaseGuide CreateGuide() => new(
    [
        new ShowcaseGuideSection("purpose", "Guide.Purpose.Title", ["Guide.Purpose.Body"]),
        new ShowcaseGuideSection("quick-start", "Guide.QuickStart.Title", ["Guide.QuickStart.Body"], ["basic", "runtime"]),
        new ShowcaseGuideSection("targets", "Guide.Targets.Title", ["Guide.Targets.Body"]),
        new ShowcaseGuideSection("context", "Guide.Context.Title", ["Guide.Context.Body"]),
        new ShowcaseGuideSection("policies", "Guide.Policies.Title", ["Guide.Policies.Body"]),
        new ShowcaseGuideSection("failures", "Guide.Failures.Title", ["Guide.Failures.Body"]),
        new ShowcaseGuideSection("collections", "Guide.Collections.Title", ["Guide.Collections.Body"]),
        new ShowcaseGuideSection("enums", "Guide.Enums.Title", ["Guide.Enums.Body"]),
        new ShowcaseGuideSection("temporal", "Guide.Temporal.Title", ["Guide.Temporal.Body"]),
        new ShowcaseGuideSection("concurrency", "Guide.Concurrency.Title", ["Guide.Concurrency.Body"]),
        new ShowcaseGuideSection("boundaries", "Guide.Boundaries.Title", ["Guide.Boundaries.Body"]),
        new ShowcaseGuideSection("production", "Guide.Production.Title", ["Guide.Production.Body"])
    ],
    [
        new ShowcaseCodeSnippet("basic", "Guide.Snippet.TypedTitle", "csharp",
            "ConversionResult<int> result = ValueConverter.Default.Convert<int>(\"42\", ConversionContext.Strict);"),
        new ShowcaseCodeSnippet("runtime", "Guide.Snippet.RuntimeTitle", "csharp",
            "ConversionResult<object?> result = ValueConverter.Default.Convert(value, typeof(decimal), context);")
    ]);

    private static ShowcaseCodeAssistCatalog CreateCodeAssist()
    {
        var items = new List<ShowcaseCodeAssistItem>
        {
            new("converter", "converter", "converter", "Assist.Converter", "variable"),
            new("convert", "converter.Convert<", "converter.Convert<int>(\"42\", ConversionContext.Strict);", "Assist.Convert", "method", true),
            new("typeof", "typeof(...) ", "typeof(int)", "Assist.Typeof", "keyword"),
            new("strict", "ConversionContext.Strict", "ConversionContext.Strict", "Assist.Strict", "property"),
            new("context", "new ConversionContext(", "new ConversionContext(CultureInfo.InvariantCulture, overflow: OverflowPolicy.Fail)", "Assist.Context", "constructor", true),
            new("invariant", "CultureInfo.InvariantCulture", "CultureInfo.InvariantCulture", "Assist.Culture", "culture"),
            new("en", "CultureInfo.GetCultureInfo(\"en-US\")", "CultureInfo.GetCultureInfo(\"en-US\")", "Assist.Culture", "culture"),
            new("de", "CultureInfo.GetCultureInfo(\"de-DE\")", "CultureInfo.GetCultureInfo(\"de-DE\")", "Assist.Culture", "culture"),
            new("hu", "CultureInfo.GetCultureInfo(\"hu-HU\")", "CultureInfo.GetCultureInfo(\"hu-HU\")", "Assist.Culture", "culture"),
            new("overflow-arg", "overflow:", "overflow: ", "Assist.NamedArgument", "argument"),
            new("nulls-arg", "nulls:", "nulls: ", "Assist.NamedArgument", "argument"),
            new("enums-arg", "enums:", "enums: ", "Assist.NamedArgument", "argument"),
            new("loss-arg", "numericLoss:", "numericLoss: ", "Assist.NamedArgument", "argument"),
            new("boolean-arg", "numericBooleans:", "numericBooleans: ", "Assist.NamedArgument", "argument"),
            new("temporal-arg", "temporalText:", "temporalText: ", "Assist.NamedArgument", "argument"),
            new("depth-arg", "maximumDepth:", "maximumDepth: 32", "Assist.NamedArgument", "argument"),
            new("items-arg", "maximumCollectionItems:", "maximumCollectionItems: 200", "Assist.NamedArgument", "argument"),
            new("snippet-saturate", "Saturating byte sample", "converter.Convert<byte>(300, new ConversionContext(CultureInfo.InvariantCulture, overflow: OverflowPolicy.Saturate));", "Assist.Snippet", "snippet", true),
            new("snippet-runtime", "Runtime target sample", "converter.Convert(\"1.234,5\", typeof(decimal), new ConversionContext(CultureInfo.GetCultureInfo(\"de-DE\")));", "Assist.Snippet", "snippet", true)
        };

        foreach (string target in TargetTypes.Keys)
            items.Add(new ShowcaseCodeAssistItem($"target-{target}", target, target, "Assist.Target", "type"));
        AddEnumItems<OverflowPolicy>(items);
        AddEnumItems<NullPolicy>(items);
        AddEnumItems<EnumPolicy>(items);
        AddEnumItems<NumericLossPolicy>(items);
        AddEnumItems<NumericBooleanPolicy>(items);
        AddEnumItems<TemporalTextPolicy>(items);

        return new ShowcaseCodeAssistCatalog(
            "Constrained C#",
            items.ToArray(),
            [
                new ShowcaseCodeAssistSignature("converter.Convert<T>(value)", "Assist.Convert"),
                new ShowcaseCodeAssistSignature("converter.Convert<T>(value, context)", "Assist.Convert"),
                new ShowcaseCodeAssistSignature("converter.Convert(value, typeof(T), context)", "Assist.Runtime")
            ],
            ['.', '<', '(', ',']);
    }

    private static void AddEnumItems<T>(List<ShowcaseCodeAssistItem> items)
        where T : struct, Enum
    {
        foreach (T value in Enum.GetValues<T>())
        {
            string label = $"{typeof(T).Name}.{value}";
            items.Add(new ShowcaseCodeAssistItem($"policy-{typeof(T).Name}-{value}", label, label, "Assist.Policy", "enum"));
        }
    }
}
