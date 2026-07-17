// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using System.Text;
using System.Text.Json;
using Pocok.Scripting.Execution;
using Pocok.Showcase.Contracts;
using Pocok.Showcase.Scripting.Models;

namespace Pocok.Showcase.Scripting;

public sealed class ScriptingShowcaseSlice(ScriptRunner runner) : ShowcaseSlice<ScriptingInput, ScriptingOutput>
{
    private const int MaximumScriptBytes = 32 * 1024;
    private static readonly IReadOnlyList<ShowcaseSample<ScriptingInput>> SampleCatalog = CreateSamples();
    private static readonly ShowcaseGuide GuideCatalog = CreateGuide();
    private static readonly ShowcaseCodeAssistCatalog AssistCatalog = CreateCodeAssist();
    private static readonly JsonSerializerOptions ResultJson = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly ScriptRunner _runner = runner ?? throw new ArgumentNullException(nameof(runner));

    public override ShowcaseSliceDescriptor Descriptor { get; } = new(
        "pocok.showcase.scripting",
        "Pocok.Scripting",
        "scripting",
        "Capability",
        "Experimental",
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
    public override ShowcaseCodeAssistCatalog CodeAssist => AssistCatalog;

    public override async ValueTask<ScriptingOutput> ExecuteAsync(
        ScriptingInput input,
        ShowcaseExecutionContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        await context.Progress.ReportAsync(
            "validate",
            "Validating script and execution bounds.",
            cancellationToken).ConfigureAwait(false);

        ScriptingOutput? rejected = Validate(input);
        if (rejected is not null)
            return rejected;

        var request = new ScriptExecutionRequest(
            $"showcase.{input.SampleId}",
            input.Script)
        {
            ExpectResult = input.ExpectResult
        };
        var options = new ScriptExecutionOptions
        {
            Timeout = TimeSpan.FromMilliseconds(input.TimeoutMilliseconds),
            MaxStatements = input.MaxStatements,
            MaxRecursionDepth = input.MaxRecursionDepth,
            MaxScriptLength = MaximumScriptBytes,
            MaxMemoryBytes = input.MaxMemoryMegabytes * 1024L * 1024L
        };

        await context.Progress.ReportAsync(
            "execute",
            "Executing through Pocok.Scripting.ScriptRunner.",
            cancellationToken).ConfigureAwait(false);

        ScriptResult<object?> result = await _runner.ExecuteAsync(request, options, cancellationToken)
            .ConfigureAwait(false);
        string limits = FormatLimits(input);
        if (!result.IsSuccess)
        {
            ScriptFailure failure = result.Failure!;
            return new ScriptingOutput(
                false,
                "Script failed safely",
                null,
                "n/a",
                input.ExpectResult,
                failure.Code,
                failure.Message,
                failure.Line,
                failure.Column,
                input.Script,
                limits,
                TipsFor(input, failure.Code));
        }

        string formatted = FormatResult(result.Value);
        string runtimeType = result.Value?.GetType().Name ?? "null";
        context.Output.Write(formatted);
        await context.Progress.ReportAsync(
            "complete",
            "Script execution completed.",
            cancellationToken).ConfigureAwait(false);

        return new ScriptingOutput(
            true,
            Headline(result.Value, formatted, input.ExpectResult),
            formatted,
            runtimeType,
            input.ExpectResult,
            null,
            null,
            null,
            null,
            input.Script,
            limits,
            TipsFor(input, null));
    }

    protected override ShowcaseRunResult CreateRunResult(ScriptingOutput output, TimeSpan elapsed)
    {
        var fields = new List<ShowcaseResultField>
        {
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
            ShowcaseRunStatus status = output.FailureCode?.StartsWith("showcase.", StringComparison.Ordinal) == true
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
            [new ShowcaseTimelineEvent(DateTimeOffset.UtcNow, "scripting", "ScriptRunner returned a successful result.")],
            codePreview: output.ScriptPreview,
            elapsed: elapsed,
            tipKeys: output.TipKeys);
    }

    public static string FormatResult(object? value)
    {
        if (value is null)
            return "null";
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
            return value.ToString() ?? value.GetType().Name;
        }
    }

    private static ScriptingOutput? Validate(ScriptingInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Script))
            return Rejected(input, "Script rejected", "showcase.script-empty", "The script cannot be empty.");
        if (Encoding.UTF8.GetByteCount(input.Script) > MaximumScriptBytes)
        {
            return Rejected(
                input,
                "Script rejected",
                "showcase.script-too-large",
                $"The script exceeds the {MaximumScriptBytes} byte showcase limit.");
        }
        if (input.TimeoutMilliseconds is < 50 or > 2_000)
        {
            return Rejected(
                input,
                "Bounds rejected",
                "showcase.timeout-bounds",
                "Timeout must be between 50 and 2000 milliseconds.");
        }
        if (input.MaxStatements is < 100 or > 100_000)
        {
            return Rejected(
                input,
                "Bounds rejected",
                "showcase.statement-bounds",
                "Statement limit must be between 100 and 100000.");
        }
        if (input.MaxRecursionDepth is < 8 or > 128)
        {
            return Rejected(
                input,
                "Bounds rejected",
                "showcase.recursion-bounds",
                "Recursion depth must be between 8 and 128.");
        }
        if (input.MaxMemoryMegabytes is < 4 or > 64)
        {
            return Rejected(
                input,
                "Bounds rejected",
                "showcase.memory-bounds",
                "Memory limit must be between 4 and 64 MiB.");
        }

        return null;
    }

    private static ScriptingOutput Rejected(
        ScriptingInput input,
        string headline,
        string code,
        string message) => new(
        false,
        headline,
        null,
        "n/a",
        input.ExpectResult,
        code,
        message,
        null,
        null,
        input.Script,
        FormatLimits(input),
        ["Tips.Bounds"]);

    private static string FormatLimits(ScriptingInput input) => string.Create(
        CultureInfo.InvariantCulture,
        $"{input.TimeoutMilliseconds} ms · {input.MaxStatements} statements · depth {input.MaxRecursionDepth} · {input.MaxMemoryMegabytes} MiB");

    private static string Headline(object? value, string formatted, bool expectResult)
    {
        if (!expectResult && value is null)
            return "Completed without a required return value";
        if (value is not null and not string and not bool and not IFormattable)
            return "Structured result";

        string singleLine = formatted.ReplaceLineEndings(" ").Trim();
        return singleLine.Length <= 160
            ? singleLine
            : string.Concat(singleLine.AsSpan(0, 160), "…");
    }

    private static string[] TipsFor(ScriptingInput input, string? failureCode)
    {
        var tips = new List<string> { "Tips.Isolation", "Tips.Bounds" };
        if (input.ExpectResult)
            tips.Add("Tips.ReturnValue");
        if (!string.IsNullOrWhiteSpace(failureCode))
            tips.Add("Tips.Failures");
        tips.Add("Tips.Capabilities");
        return tips.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static IReadOnlyList<ShowcaseSample<ScriptingInput>> CreateSamples() =>
    [
        Sample(
            "arithmetic",
            """
            function triangular(value) {
                return value * (value + 1) / 2;
            }

            triangular(12);
            """,
            "78",
            isDefault: true),
        Sample(
            "object-result",
            """
            const values = [2, 4, 8];

            ({
                count: values.length,
                total: values.reduce((sum, value) => sum + value, 0),
                doubled: values.map(value => value * 2)
            });
            """,
            "Structured result"),
        Sample(
            "string-result",
            """
            const packageName = "Pocok.Scripting";
            `Executed by ${packageName}`;
            """,
            "\"Executed by Pocok.Scripting\""),
        Sample(
            "missing-result",
            "const message = \"This script ends with a declaration.\";",
            "Script failed safely"),
        Sample(
            "syntax-error",
            """
            function broken( {
                return 42;
            }
            """,
            "Script failed safely"),
        Sample(
            "recursion-limit",
            """
            function recurse() {
                return recurse();
            }

            recurse();
            """,
            "Script failed safely",
            maxRecursionDepth: 24)
    ];

    private static ShowcaseSample<ScriptingInput> Sample(
        string id,
        string script,
        string expected,
        bool isDefault = false,
        int maxRecursionDepth = 64) => new(
        id,
        $"Samples.{id}.Name",
        $"Samples.{id}.Description",
        () => new ScriptingInput
        {
            SampleId = id,
            Script = script,
            ExpectResult = true,
            MaxRecursionDepth = maxRecursionDepth
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
            ["runner", "script"]),
        new ShowcaseGuideSection("results", "Guide.Results.Title", ["Guide.Results.Body"]),
        new ShowcaseGuideSection("bounds", "Guide.Bounds.Title", ["Guide.Bounds.Body"]),
        new ShowcaseGuideSection("capabilities", "Guide.Capabilities.Title", ["Guide.Capabilities.Body"]),
        new ShowcaseGuideSection("imports", "Guide.Imports.Title", ["Guide.Imports.Body"]),
        new ShowcaseGuideSection("concurrency", "Guide.Concurrency.Title", ["Guide.Concurrency.Body"]),
        new ShowcaseGuideSection("production", "Guide.Production.Title", ["Guide.Production.Body"])
    ],
    [
        new ShowcaseCodeSnippet(
            "runner",
            "Guide.Snippet.RunnerTitle",
            "csharp",
            """
            ScriptResult<object?> result = await new ScriptRunner().ExecuteAsync(
                new ScriptExecutionRequest("demo", script) { ExpectResult = true },
                new ScriptExecutionOptions { Timeout = TimeSpan.FromSeconds(1) },
                cancellationToken);
            """),
        new ShowcaseCodeSnippet(
            "script",
            "Guide.Snippet.ScriptTitle",
            "javascript",
            """
            function add(left, right) {
                return left + right;
            }

            add(20, 22);
            """)
    ]);

    private static ShowcaseCodeAssistCatalog CreateCodeAssist() => new(
        "JavaScript",
        [
            new("const", "const", "const value = 42;", "Assist.Const", "keyword", true),
            new(
                "function",
                "function",
                """
                function calculate(value) {
                    return value;
                }
                """,
                "Assist.Function",
                "snippet",
                true),
            new("return", "return", "return ", "Assist.Return", "keyword"),
            new(
                "array",
                "Array sample",
                """
                const values = [1, 2, 3];
                values.map(value => value * 2);
                """,
                "Assist.Array",
                "snippet",
                true),
            new(
                "object",
                "Object result",
                "({ answer: 42, ok: true });",
                "Assist.Object",
                "snippet",
                true),
            new(
                "reduce",
                "reduce",
                "values.reduce((sum, value) => sum + value, 0)",
                "Assist.Reduce",
                "method"),
            new("map", "map", "values.map(value => value)", "Assist.Map", "method")
        ],
        [
            new("function name(arguments) { ... }", "Assist.Function"),
            new("array.map(callback)", "Assist.Map"),
            new("array.reduce(callback, initialValue)", "Assist.Reduce")
        ],
        ['.', '(', '[']);
}
