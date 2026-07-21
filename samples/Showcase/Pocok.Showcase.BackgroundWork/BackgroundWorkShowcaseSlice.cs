// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.BackgroundWork.Coalescing;
using Pocok.BackgroundWork.Debouncing;
using Pocok.BackgroundWork.Observation;
using Pocok.BackgroundWork.Repetition;
using Pocok.Showcase.Contracts;

namespace Pocok.Showcase.BackgroundWork;

public sealed record BackgroundWorkInput
{
    public string SampleId { get; init; } = "observe";
    public string Mode { get; init; } = "observe";
    public bool IncludeOptional { get; init; }
    public bool IncludeLifecycle { get; init; }
    public int Capacity { get; init; } = 32;
}

public sealed record BackgroundWorkOutput(string Code);

public sealed class BackgroundWorkShowcaseSlice : ShowcaseSlice<BackgroundWorkInput, BackgroundWorkOutput>,
    IShowcasePackageCoverage
{
    private static readonly IReadOnlyList<ShowcaseSample<BackgroundWorkInput>> SampleDefinitions =
    [
        new(
            "observe",
            "Samples.observe.Name",
            "Samples.observe.Description",
            () => new BackgroundWorkInput
            {
                SampleId = "observe",
                Mode = "observe",
                Capacity = 250,
                IncludeOptional = false,
                IncludeLifecycle = false
            },
            true,
            "Recipe generated",
            "purpose",
            "recipe"),
        new(
            "coalesce",
            "Samples.coalesce.Name",
            "Samples.coalesce.Description",
            () => new BackgroundWorkInput
            {
                SampleId = "coalesce",
                Mode = "coalesce",
                Capacity = 250,
                IncludeOptional = true,
                IncludeLifecycle = false
            },
            false,
            "Recipe generated",
            "purpose",
            "recipe"),
        new(
            "debounce",
            "Samples.debounce.Name",
            "Samples.debounce.Description",
            () => new BackgroundWorkInput
            {
                SampleId = "debounce",
                Mode = "debounce",
                Capacity = 250,
                IncludeOptional = false,
                IncludeLifecycle = true
            },
            false,
            "Recipe generated",
            "purpose",
            "recipe"),
        new(
            "repeat",
            "Samples.repeat.Name",
            "Samples.repeat.Description",
            () => new BackgroundWorkInput
            {
                SampleId = "repeat",
                Mode = "repeat",
                Capacity = 250,
                IncludeOptional = false,
                IncludeLifecycle = false
            },
            false,
            "Recipe generated",
            "purpose",
            "recipe")
    ];

    public override ShowcaseSliceDescriptor Descriptor { get; } = new(
        "pocok.showcase.background-work",
        "Pocok.BackgroundWork",
        "background-work",
        "Capability",
        "Experimental",
        "Package.Name",
        "Package.Summary",
        9,
        "src/BackgroundWork/README.md",
        false,
        ShowcaseImplementationStatus.Available,
        "background-work",
        "1.0.0");

    public override Type PageComponentType => typeof(BackgroundWorkPage);
    public override IReadOnlyList<ShowcaseSample<BackgroundWorkInput>> TypedSamples => SampleDefinitions;

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
        "Pocok.BackgroundWork"
    ];

    public override ValueTask<BackgroundWorkOutput> ExecuteAsync(
        BackgroundWorkInput input,
        ShowcaseExecutionContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var code = BackgroundWorkRecipeRenderer.Render(input);
        context.Output.Write(code);
        return ValueTask.FromResult(new BackgroundWorkOutput(code));
    }

    protected override ShowcaseRunResult CreateRunResult(BackgroundWorkOutput output, TimeSpan elapsed)
    {
        return new ShowcaseRunResult(
            ShowcaseRunStatus.Success,
            "Recipe generated",
            codePreview: output.Code,
            elapsed: elapsed,
            tipKeys: ["Tips.Generated", "Tips.Ownership"]);
    }
}

public static class BackgroundWorkRecipeRenderer
{
    public static IReadOnlyList<string> Modes { get; } = ["observe", "coalesce", "debounce", "repeat"];

    public static string Render(BackgroundWorkInput input)
    {
        var interval = Math.Clamp(input.Capacity, 1, 10_000);
        var failureOptions = input.IncludeOptional
            ? """

                  FailurePolicy = Pocok.BackgroundWork.FailureHandling.BackgroundWorkFailurePolicy.Continue,
                  OnFailure = (exception, _) =>
                  {
                      logger.LogError(exception, "Background work failed.");
                      return ValueTask.CompletedTask;
                  },
              """
            : string.Empty;
        var explicitStop = input.IncludeLifecycle
            ? "\nawait runner.StopAsync(cancellationToken);"
            : string.Empty;
        var timeoutFault = input.IncludeOptional
            ? "\n        .OnFault<TimeoutException>(exception => " +
              "logger.LogWarning(exception, \"Background work timed out.\"))"
            : string.Empty;
        var observationOutcome = input.IncludeLifecycle
            ? "\nConsole.WriteLine(outcome.Outcome);"
            : string.Empty;

        const string header = """
                              // Install Pocok.BackgroundWork.
                              using Pocok.BackgroundWork.Coalescing;
                              using Pocok.BackgroundWork.Debouncing;
                              using Pocok.BackgroundWork.Observation;
                              using Pocok.BackgroundWork.Repetition;

                              """;

        var recipe = input.Mode switch
        {
            "coalesce" => $$"""
                            await using var runner = new CoalescingTaskRunner(
                                async cancellationToken => await RefreshAsync(cancellationToken),
                                new CoalescingTaskRunnerOptions
                                {
                                    MinimumInterval = TimeSpan.FromMilliseconds({{interval}}),
                                    TimeProvider = TimeProvider.System,{{failureOptions}}
                                });
                            await runner.RequestAsync(cancellationToken);{{explicitStop}}
                            """,
            "debounce" => $$"""
                            await using var runner = new DebouncedTaskRunner(
                                async cancellationToken => await SaveAsync(cancellationToken),
                                new DebouncedTaskRunnerOptions
                                {
                                    QuietPeriod = TimeSpan.FromMilliseconds({{interval}}),
                                    TimeProvider = TimeProvider.System,{{failureOptions}}
                                });
                            await runner.RequestAsync(cancellationToken);{{explicitStop}}
                            """,
            "repeat" => $$"""
                          await TaskRepeater.RepeatAsync(
                              async cancellationToken => await PollAsync(cancellationToken),
                              new TaskRepeaterOptions
                              {
                                  Interval = TimeSpan.FromMilliseconds({{interval}}),
                                  MaximumIterations = {{(input.IncludeOptional ? "10" : "null")}},
                                  TimeProvider = TimeProvider.System
                              },
                              cancellationToken);{{(input.IncludeLifecycle ? "\n// cancellationToken owns the repetition lifetime." : string.Empty)}}
                          """,
            _ => $$"""
                   TaskObservation observation = backgroundTask.Observe(
                       exception => logger.LogError(exception, "Background work failed."),
                       options => options
                           .OnSuccess(() => logger.LogInformation("Background work completed."))
                           .OnCanceled(_ => logger.LogInformation("Background work was cancelled.")){{timeoutFault}});
                   TaskObservationResult outcome = await observation.Completion;{{observationOutcome}}
                   """
        };
        return header + recipe;
    }

    internal static async Task CompileProofAsync(Task backgroundTask, CancellationToken cancellationToken)
    {
        _ = backgroundTask.Observe(_ => { });
        await using var coalescing = new CoalescingTaskRunner(_ => ValueTask.CompletedTask);
        await coalescing.RequestAsync(cancellationToken);
        await using var debouncing = new DebouncedTaskRunner(_ => ValueTask.CompletedTask,
            new DebouncedTaskRunnerOptions { QuietPeriod = TimeSpan.FromMilliseconds(1) });
        await debouncing.RequestAsync(cancellationToken);
        await TaskRepeater.RepeatAsync(_ => ValueTask.CompletedTask,
            new TaskRepeaterOptions { Interval = TimeSpan.FromMilliseconds(1), MaximumIterations = 1 },
            cancellationToken);
    }
}
