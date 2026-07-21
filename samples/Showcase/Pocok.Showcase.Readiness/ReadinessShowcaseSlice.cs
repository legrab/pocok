// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Readiness;
using Pocok.Showcase.Contracts;

namespace Pocok.Showcase.Readiness;

public sealed record ReadinessInput
{
    public string SampleId { get; init; } = "ready";
    public string Mode { get; init; } = "ready";
    public bool IncludeOptional { get; init; }
    public bool IncludeLifecycle { get; init; }
    public int Capacity { get; init; } = 10;
}

public sealed record ReadinessOutput(string Code);

public sealed class ReadinessShowcaseSlice : ShowcaseSlice<ReadinessInput, ReadinessOutput>, IShowcasePackageCoverage
{
    private static readonly IReadOnlyList<ShowcaseSample<ReadinessInput>> SampleDefinitions =
    [
        new(
            "ready",
            "Samples.ready.Name",
            "Samples.ready.Description",
            () => new ReadinessInput
            {
                SampleId = "ready",
                Mode = "ready",
                Capacity = 10,
                IncludeOptional = false,
                IncludeLifecycle = false
            },
            true,
            "Recipe generated",
            "purpose",
            "recipe"),
        new(
            "failure",
            "Samples.failure.Name",
            "Samples.failure.Description",
            () => new ReadinessInput
            {
                SampleId = "failure",
                Mode = "failure",
                Capacity = 10,
                IncludeOptional = true,
                IncludeLifecycle = false
            },
            false,
            "Recipe generated",
            "purpose",
            "recipe"),
        new(
            "shutdown",
            "Samples.shutdown.Name",
            "Samples.shutdown.Description",
            () => new ReadinessInput
            {
                SampleId = "shutdown",
                Mode = "shutdown",
                Capacity = 10,
                IncludeOptional = false,
                IncludeLifecycle = true
            },
            false,
            "Recipe generated",
            "purpose",
            "recipe")
    ];

    public override ShowcaseSliceDescriptor Descriptor { get; } = new(
        "pocok.showcase.readiness",
        "Pocok.Readiness",
        "readiness",
        "Capability",
        "Active",
        "Package.Name",
        "Package.Summary",
        2,
        "src/Readiness/README.md",
        false,
        ShowcaseImplementationStatus.Available,
        "readiness",
        "1.0.0");

    public override Type PageComponentType => typeof(ReadinessPage);
    public override IReadOnlyList<ShowcaseSample<ReadinessInput>> TypedSamples => SampleDefinitions;

    public override ShowcaseGuide Guide { get; } = new(
        [new ShowcaseGuideSection("purpose", "Guide.Purpose.Title", ["Guide.Purpose.Body"], ["recipe"])],
        [
            new ShowcaseCodeSnippet(
                "recipe",
                "Guide.Snippet.Title",
                "csharp",
                "Select a preset and adjust the constrained options.")
        ]);

    public IReadOnlyList<string> CoveredPackageIds { get; } =
    [
        "Pocok.Readiness"
    ];

    public override ValueTask<ReadinessOutput> ExecuteAsync(
        ReadinessInput input,
        ShowcaseExecutionContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var code = ReadinessRecipeRenderer.Render(input);
        context.Output.Write(code);
        return ValueTask.FromResult(new ReadinessOutput(code));
    }

    protected override ShowcaseRunResult CreateRunResult(ReadinessOutput output, TimeSpan elapsed)
    {
        return new ShowcaseRunResult(
            ShowcaseRunStatus.Success,
            "Recipe generated",
            codePreview: output.Code,
            elapsed: elapsed,
            tipKeys: ["Tips.Generated", "Tips.Ownership"]);
    }
}

public static class ReadinessRecipeRenderer
{
    public static IReadOnlyList<string> Modes { get; } = ["ready", "failure", "shutdown"];

    public static string Render(ReadinessInput input)
    {
        var transition = input.Mode switch
        {
            "failure" =>
                "readiness.MarkFailed(cycle, new ReadinessFailure(\"startup.failed\", \"Startup failed safely.\"));",
            "shutdown" => "readiness.MarkReady(cycle);\nreadiness.BeginShutdown();\nreadiness.MarkStopped();",
            _ => "readiness.MarkReady(cycle);\nawait signal.WaitUntilReadyAsync(cancellationToken);"
        };
        var timeoutSeconds = Math.Clamp(input.Capacity, 1, 60);
        var cancellation = input.IncludeOptional
            ? $$"""

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds({{timeoutSeconds}}));
                CancellationToken cancellationToken = timeout.Token;
                """
            : "\nCancellationToken cancellationToken = CancellationToken.None;";
        var lifecycle = input.IncludeLifecycle
            ? "\nReadinessSnapshot snapshot = signal.Snapshot;\nConsole.WriteLine($\"{snapshot.State} #{snapshot.Sequence}\");"
            : string.Empty;
        return $$"""
                 // Install Pocok.Readiness.
                 using Pocok.Readiness;

                 var readiness = new ReadinessSource();
                 IReadinessSignal signal = readiness;{{cancellation}}
                 ReadinessCycle cycle = readiness.BeginStartup();
                 {{transition}}{{lifecycle}}
                 """;
    }

    internal static async Task CompileProofAsync(CancellationToken cancellationToken)
    {
        var readiness = new ReadinessSource();
        ReadinessCycle cycle = readiness.BeginStartup();
        readiness.MarkReady(cycle);
        await readiness.WaitUntilReadyAsync(cancellationToken);
        readiness.BeginShutdown();
        readiness.MarkStopped();
        ReadinessCycle failedCycle = readiness.BeginStartup();
        readiness.MarkFailed(failedCycle, new ReadinessFailure("startup.failed", "Failed safely."));
    }
}
