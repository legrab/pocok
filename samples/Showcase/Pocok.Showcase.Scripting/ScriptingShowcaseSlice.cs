// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Collections;
using System.Globalization;
using System.Text.Json;
using Pocok.Scripting.Execution;
using Pocok.Showcase.Contracts;
using Pocok.Showcase.Scripting.Models;

namespace Pocok.Showcase.Scripting;

public sealed class ScriptingShowcaseSlice(
    ScriptRunner runner,
    ScriptEngineRegistry registry,
    ScriptingShowcaseOptions showcaseOptions) : ShowcaseSlice<ScriptingInput, ScriptingOutput>
{
    private static readonly IReadOnlyList<ShowcaseSample<ScriptingInput>> SampleCatalog = CreateSamples();
    private static readonly ShowcaseGuide GuideCatalog = CreateGuide();

    private static readonly JsonSerializerOptions ResultJson = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly ScriptRunner _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    private readonly ScriptEngineRegistry _registry = registry ?? throw new ArgumentNullException(nameof(registry));

    private readonly ScriptingShowcaseOptions _showcaseOptions =
        showcaseOptions ?? throw new ArgumentNullException(nameof(showcaseOptions));

    public override ShowcaseSliceDescriptor Descriptor { get; } = new(
        "pocok.showcase.scripting",
        "Pocok.Scripting",
        "scripting",
        "Capability",
        "Active",
        "Package.Name",
        "Package.Summary",
        10,
        "src/Scripting/README.md",
        true,
        ShowcaseImplementationStatus.Available,
        "scripting",
        "1.0.0");

    public override Type PageComponentType => typeof(ScriptingPage);
    public override IReadOnlyList<ShowcaseSample<ScriptingInput>> TypedSamples => SampleCatalog;
    public override ShowcaseGuide Guide => GuideCatalog;
    public override ShowcaseCodeAssistCatalog CodeAssist => ShowcaseCodeAssistCatalog.Empty;

    public override async ValueTask<ScriptingOutput> ExecuteAsync(
        ScriptingInput input,
        ShowcaseExecutionContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        ScriptingOutput? rejected = ValidateInput(input);
        if (rejected is not null)
            return rejected;

        ScriptEngineDescriptor? descriptor = _registry.Descriptors
            .FirstOrDefault(item => item.Id.Value == input.EngineId);
        if (descriptor is null)
        {
            return Failed(
                input,
                "scripting.engine.unknown",
                "The selected engine is not registered.");
        }

        ScriptingInput boundedInput = ApplyServerLimits(input, descriptor);

        await context.Progress.ReportAsync(
            "validate",
            $"Validating {descriptor.Language} source and engine capabilities.",
            cancellationToken).ConfigureAwait(false);

        var options = new ScriptExecutionOptions
        {
            Timeout = TimeSpan.FromMilliseconds(boundedInput.TimeoutMilliseconds),
            MaxSourceCharacters = _showcaseOptions.MaximumSourceCharacters,
            MaxOutputBytes = _showcaseOptions.MaximumOutputBytes,
            MaxStatements = boundedInput.MaxStatements,
            MaxRecursionDepth = boundedInput.MaxRecursionDepth,
            MaxMemoryBytes = boundedInput.MaxMemoryMegabytes is null
                ? null
                : boundedInput.MaxMemoryMegabytes.Value * 1024L * 1024L
        };

        await context.Progress.ReportAsync(
            "execute",
            $"Executing with {descriptor.Language}.",
            cancellationToken).ConfigureAwait(false);

        ScriptResult<object?> result = await _runner.ExecuteAsync(
            new ScriptExecutionRequest(
                new ScriptEngineId(input.EngineId),
                $"showcase.{boundedInput.SampleId}",
                boundedInput.Source)
            {
                ExpectResult = boundedInput.ExpectResult
            },
            options,
            cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            ScriptFailure failure = result.Failure!;
            return Failed(
                boundedInput,
                failure.Code,
                failure.Message,
                failure.Line,
                failure.Column);
        }

        string formatted = FormatResult(result.Value);
        context.Output.Write(formatted);
        await context.Progress.ReportAsync(
            "complete",
            $"{descriptor.Language} execution completed.",
            cancellationToken).ConfigureAwait(false);

        return new ScriptingOutput(
            true,
            Headline(result.Value, formatted, boundedInput.ExpectResult),
            formatted,
            ResultType(result.Value),
            boundedInput.ExpectResult,
            boundedInput.EngineId,
            null,
            null,
            null,
            null,
            boundedInput.Source,
            FormatLimits(boundedInput),
            ["Tips.Isolation", "Tips.Bounds", "Tips.Capabilities"]);
    }

    protected override ShowcaseRunResult CreateRunResult(ScriptingOutput output, TimeSpan elapsed)
    {
        var fields = new List<ShowcaseResultField>
        {
            new("Result.Fields.Engine", output.EngineId, true, true),
            new("Result.Fields.ResultType", output.ResultType, false, true),
            new("Result.Fields.ExpectResult", output.ExpectResult ? "true" : "false", true, true),
            new("Result.Fields.Limits", output.LimitsSummary, false, true)
        };

        if (output.IsSuccess)
            fields.Insert(0, new ShowcaseResultField("Result.Fields.ReturnValue", output.Result, true, true));
        if (output.FailureLine is not null)
        {
            fields.Add(new ShowcaseResultField(
                "Result.Fields.Line",
                output.FailureLine.Value.ToString(CultureInfo.InvariantCulture),
                true,
                true));
        }

        if (output.FailureColumn is not null)
        {
            fields.Add(new ShowcaseResultField(
                "Result.Fields.Column",
                output.FailureColumn.Value.ToString(CultureInfo.InvariantCulture),
                true,
                true));
        }

        if (!output.IsSuccess)
        {
            ShowcaseRunStatus status = output.FailureCode?.StartsWith(
                "showcase.",
                StringComparison.Ordinal) == true
                ? ShowcaseRunStatus.Rejected
                : ShowcaseRunStatus.ExpectedFailure;
            return new ShowcaseRunResult(
                status,
                output.Headline,
                fields,
                diagnostics:
                [
                    new ShowcaseDiagnostic(
                        output.FailureCode ?? "scripting.failure",
                        output.FailureMessage ?? "Script execution failed.",
                        "warning")
                ],
                codePreview: output.ScriptPreview,
                elapsed: elapsed,
                tipKeys: output.TipKeys);
        }

        return new ShowcaseRunResult(
            ShowcaseRunStatus.Success,
            output.Headline,
            fields,
            [
                new ShowcaseTimelineEvent(
                    DateTimeOffset.UtcNow,
                    output.EngineId,
                    "ScriptRunner returned a successful result.")
            ],
            codePreview: output.ScriptPreview,
            elapsed: elapsed,
            tipKeys: output.TipKeys);
    }

    public static string FormatResult(object? value)
    {
        if (value is null)
            return "null";
        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => JsonSerializer.Serialize(element.GetString()),
                JsonValueKind.Null or JsonValueKind.Undefined => "null",
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Number => element.GetRawText(),
                _ => JsonSerializer.Serialize(element, ResultJson)
            };
        }

        if (value is string text)
            return JsonSerializer.Serialize(text);
        if (value is bool boolean)
            return boolean ? "true" : "false";
        if (value is double doubleValue)
            return doubleValue.ToString("R", CultureInfo.InvariantCulture);
        if (value is float floatValue)
            return floatValue.ToString("R", CultureInfo.InvariantCulture);
        if (value is decimal decimalValue)
            return decimalValue.ToString(CultureInfo.InvariantCulture);
        if (value is IFormattable formattable && value.GetType().IsPrimitive)
            return formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;

        try
        {
            return JsonSerializer.Serialize(value, value.GetType(), ResultJson);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return value.GetType().Name;
        }
    }

    private ScriptingInput ApplyServerLimits(
        ScriptingInput input,
        ScriptEngineDescriptor descriptor
    ) =>
        input with
        {
            MaxStatements = descriptor.Capabilities.EnforcesStatementLimit
                ? input.MaxStatements ?? _showcaseOptions.MaximumStatements
                : null,
            MaxRecursionDepth = descriptor.Capabilities.EnforcesRecursionLimit
                ? input.MaxRecursionDepth ?? _showcaseOptions.MaximumRecursionDepth
                : null,
            MaxMemoryMegabytes = descriptor.Capabilities.EnforcesMemoryLimit
                ? input.MaxMemoryMegabytes ?? _showcaseOptions.MaximumMemoryMegabytes
                : null
        };

    private ScriptingOutput? ValidateInput(ScriptingInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Source))
        {
            return Failed(
                input,
                "showcase.script-empty",
                "The script cannot be empty.");
        }

        if (input.Source.Length > _showcaseOptions.MaximumSourceCharacters)
        {
            return Failed(
                input,
                "showcase.script-too-large",
                $"The script exceeds the {_showcaseOptions.MaximumSourceCharacters.ToString(CultureInfo.InvariantCulture)} " +
                "character Showcase limit.");
        }

        if (input.TimeoutMilliseconds is < 50
            || input.TimeoutMilliseconds > _showcaseOptions.MaximumTimeoutMilliseconds)
        {
            return Failed(
                input,
                "showcase.timeout-bounds",
                $"Timeout must be between 50 and " +
                $"{_showcaseOptions.MaximumTimeoutMilliseconds.ToString(CultureInfo.InvariantCulture)} milliseconds.");
        }

        if (input.MaxStatements is int statements
            && (statements <= 0 || statements > _showcaseOptions.MaximumStatements))
        {
            return Failed(
                input,
                "showcase.statement-bounds",
                $"Statement limit must be no more than " +
                $"{_showcaseOptions.MaximumStatements.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (input.MaxRecursionDepth is int recursionDepth
            && (recursionDepth <= 0 || recursionDepth > _showcaseOptions.MaximumRecursionDepth))
        {
            return Failed(
                input,
                "showcase.recursion-bounds",
                $"Recursion limit must be no more than " +
                $"{_showcaseOptions.MaximumRecursionDepth.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (input.MaxMemoryMegabytes is int memoryMegabytes
            && (memoryMegabytes <= 0 || memoryMegabytes > _showcaseOptions.MaximumMemoryMegabytes))
        {
            return Failed(
                input,
                "showcase.memory-bounds",
                $"Memory limit must be no more than " +
                $"{_showcaseOptions.MaximumMemoryMegabytes.ToString(CultureInfo.InvariantCulture)} MiB.");
        }

        return null;
    }

    private ScriptingOutput Failed(
        ScriptingInput input,
        string code,
        string message,
        int? line = null,
        int? column = null) => new(
        false,
        "Script failed safely",
        null,
        "n/a",
        input.ExpectResult,
        input.EngineId,
        code,
        message,
        line,
        column,
        input.Source,
        FormatLimits(input),
        ["Tips.Bounds", "Tips.Failures"]);

    private string FormatLimits(ScriptingInput input)
    {
        var limits = new List<string>
        {
            $"{input.TimeoutMilliseconds.ToString(CultureInfo.InvariantCulture)} ms",
            $"{_showcaseOptions.MaximumSourceCharacters.ToString(CultureInfo.InvariantCulture)} characters",
            $"{_showcaseOptions.MaximumOutputBytes.ToString(CultureInfo.InvariantCulture)} output bytes"
        };
        if (input.MaxStatements is not null)
            limits.Add($"{input.MaxStatements.Value.ToString(CultureInfo.InvariantCulture)} statements");
        if (input.MaxRecursionDepth is not null)
            limits.Add($"depth {input.MaxRecursionDepth.Value.ToString(CultureInfo.InvariantCulture)}");
        if (input.MaxMemoryMegabytes is not null)
            limits.Add($"{input.MaxMemoryMegabytes.Value.ToString(CultureInfo.InvariantCulture)} MiB");
        return string.Join(" · ", limits);
    }

    private static string Headline(object? value, string formatted, bool expectResult)
    {
        if (!expectResult && value is null)
            return "Completed without a required return value";
        if (IsStructured(value))
            return "Structured result";

        string singleLine = formatted.ReplaceLineEndings(" ").Trim();
        return singleLine.Length <= 160
            ? singleLine
            : string.Concat(singleLine.AsSpan(0, 160), "…");
    }

    private static bool IsStructured(object? value) => value switch
    {
        JsonElement { ValueKind: JsonValueKind.Array or JsonValueKind.Object } => true,
        IDictionary => true,
        IEnumerable when value is not string => true,
        _ => false
    };

    private static string ResultType(object? value) => value switch
    {
        null => "null",
        JsonElement element => element.ValueKind.ToString(),
        _ => value.GetType().Name
    };

    private static IReadOnlyList<ShowcaseSample<ScriptingInput>> CreateSamples() =>
    [
        Sample(
            "arithmetic",
            "78",
            true,
            "function triangular(value) { return value * (value + 1) / 2; }\ntriangular(12);",
            "int Triangular(int value) => value * (value + 1) / 2;\nTriangular(12)",
            "def triangular(value):\n    return value * (value + 1) // 2\ntriangular(12)"),
        Sample(
            "object-result",
            "Structured result",
            false,
            "({ count: 3, values: [2, 4, 8] });",
            "new { count = 3, values = new[] { 2, 4, 8 } }",
            "{\"count\": 3, \"values\": [2, 4, 8]}"),
        Sample(
            "string-result",
            "\"Executed by Pocok.Scripting\"",
            false,
            "`Executed by Pocok.Scripting`;",
            "\"Executed by Pocok.Scripting\"",
            "'Executed by Pocok.Scripting'"),
        Sample(
            "missing-result",
            "Script failed safely",
            false,
            "const message = 'missing';",
            "var message = \"missing\";",
            "message = 'missing'",
            expectResult: true),
        Sample(
            "syntax-error",
            "Script failed safely",
            false,
            "function broken( {",
            "var broken = ;",
            "def broken(:"),
        Sample(
            "bounded-runaway",
            "Script failed safely",
            false,
            "while (true) { }",
            "while (true) { }",
            "while True:\n    pass",
            timeoutMilliseconds: 100),
        Sample(
            "validator-rejection",
            "Script failed safely",
            false,
            "eval('1');",
            "System.IO.File.ReadAllText(\"x\")",
            "open('x').read()")
    ];

    private static ShowcaseSample<ScriptingInput> Sample(
        string id,
        string expected,
        bool isDefault,
        string javaScript,
        string cSharp,
        string python,
        bool expectResult = true,
        int timeoutMilliseconds = 1_000) => new(
        id,
        $"Samples.{id}.Name",
        $"Samples.{id}.Description",
        () => new ScriptingInput
        {
            SampleId = id,
            EngineId = ScriptEngineId.JavaScript.Value,
            Source = javaScript,
            Sources = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [ScriptEngineId.JavaScript.Value] = javaScript,
                [ScriptEngineId.CSharp.Value] = cSharp,
                [ScriptEngineId.Python.Value] = python
            },
            ExpectResult = expectResult,
            TimeoutMilliseconds = timeoutMilliseconds
        },
        isDefault,
        expected,
        "quick-start",
        "runner");

    private static ShowcaseGuide CreateGuide() => new(
        [
            new ShowcaseGuideSection("purpose", "Guide.Purpose.Title", ["Guide.Purpose.Body"]),
            new ShowcaseGuideSection(
                "quick-start",
                "Guide.QuickStart.Title",
                ["Guide.QuickStart.Body"],
                ["runner"]),
            new ShowcaseGuideSection("bounds", "Guide.Bounds.Title", ["Guide.Bounds.Body"]),
            new ShowcaseGuideSection("production", "Guide.Production.Title", ["Guide.Production.Body"])
        ],
        [
            new ShowcaseCodeSnippet(
                "runner",
                "Guide.Snippet.RunnerTitle",
                "csharp",
                "var registry = new ScriptEngineRegistry(adapters);\n" +
                "var runner = new ScriptRunner(registry);\n" +
                "await runner.ExecuteAsync(request, options, cancellationToken);")
        ]);
}
