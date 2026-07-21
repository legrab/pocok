// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Showcase.Contracts;
using Pocok.Subscriptions;

namespace Pocok.Showcase.Subscriptions;

public sealed record SubscriptionsInput
{
    public string SampleId { get; init; } = "subscribe";
    public string Mode { get; init; } = "subscribe";
    public bool IncludeOptional { get; init; }
    public bool IncludeLifecycle { get; init; }
    public int Capacity { get; init; } = 32;
}

public sealed record SubscriptionsOutput(string Code);

public sealed class SubscriptionsShowcaseSlice : ShowcaseSlice<SubscriptionsInput, SubscriptionsOutput>,
    IShowcasePackageCoverage
{
    private static readonly IReadOnlyList<ShowcaseSample<SubscriptionsInput>> SampleDefinitions =
    [
        new(
            "subscribe",
            "Samples.subscribe.Name",
            "Samples.subscribe.Description",
            () => new SubscriptionsInput
            {
                SampleId = "subscribe",
                Mode = "subscribe",
                Capacity = 32,
                IncludeOptional = false,
                IncludeLifecycle = false
            },
            true,
            "Recipe generated",
            "purpose",
            "recipe"),
        new(
            "filter-map",
            "Samples.filter-map.Name",
            "Samples.filter-map.Description",
            () => new SubscriptionsInput
            {
                SampleId = "filter-map",
                Mode = "filter-map",
                Capacity = 32,
                IncludeOptional = true,
                IncludeLifecycle = false
            },
            false,
            "Recipe generated",
            "purpose",
            "recipe"),
        new(
            "remove",
            "Samples.remove.Name",
            "Samples.remove.Description",
            () => new SubscriptionsInput
            {
                SampleId = "remove",
                Mode = "remove",
                Capacity = 32,
                IncludeOptional = false,
                IncludeLifecycle = true
            },
            false,
            "Recipe generated",
            "purpose",
            "recipe")
    ];

    public override ShowcaseSliceDescriptor Descriptor { get; } = new(
        "pocok.showcase.subscriptions",
        "Pocok.Subscriptions",
        "subscriptions",
        "Capability",
        "Experimental",
        "Package.Name",
        "Package.Summary",
        16,
        "src/Subscriptions/README.md",
        false,
        ShowcaseImplementationStatus.Available,
        "subscriptions",
        "1.0.0");

    public override Type PageComponentType => typeof(SubscriptionsPage);
    public override IReadOnlyList<ShowcaseSample<SubscriptionsInput>> TypedSamples => SampleDefinitions;

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
        "Pocok.Subscriptions"
    ];

    public override ValueTask<SubscriptionsOutput> ExecuteAsync(
        SubscriptionsInput input,
        ShowcaseExecutionContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var code = SubscriptionsRecipeRenderer.Render(input);
        context.Output.Write(code);
        return ValueTask.FromResult(new SubscriptionsOutput(code));
    }

    protected override ShowcaseRunResult CreateRunResult(SubscriptionsOutput output, TimeSpan elapsed)
    {
        return new ShowcaseRunResult(
            ShowcaseRunStatus.Success,
            "Recipe generated",
            codePreview: output.Code,
            elapsed: elapsed,
            tipKeys: ["Tips.Generated", "Tips.Ownership"]);
    }
}

public static class SubscriptionsRecipeRenderer
{
    public static IReadOnlyList<string> Modes { get; } = ["subscribe", "filter-map", "remove"];

    public static string Render(SubscriptionsInput input)
    {
        var configuration = input.Mode == "filter-map"
            ? """
              options => options
                  .WithObjectFilter(value => value is string)
                  .WithValueMapper(value => int.Parse((string)value!, System.Globalization.CultureInfo.InvariantCulture))
                  .WithValueFilter(value => value is >= 0 and <= 100)
              """
            : input.IncludeOptional
                ? "options => options.WithValueFilter(value => value >= 0)"
                : "null";
        var explicitOwnership = input.Mode == "remove" || input.IncludeLifecycle;
        var hubDeclaration = explicitOwnership
            ? "var hub = new KeyedSubscriptionHub<string>(StringComparer.Ordinal);"
            : "using var hub = new KeyedSubscriptionHub<string>(StringComparer.Ordinal);";
        var registrationDeclaration = explicitOwnership
            ? "IDisposable registration"
            : "using IDisposable registration";
        var removal = explicitOwnership
            ? "\nregistration.Dispose();\nhub.Dispose();"
            : string.Empty;
        var publishedValue = input.Mode == "filter-map" ? "\"21\"" : "21";
        return $$"""
                 // Install Pocok.Subscriptions.
                 using Pocok.Subscriptions;

                 {{hubDeclaration}}
                 {{registrationDeclaration}} = hub.Subscribe<int>(
                     "temperature",
                     (_, value) => Console.WriteLine(value),
                     {{configuration}});
                 int delivered = 0;
                 for (int index = 0; index < {{Math.Clamp(input.Capacity, 1, 100)}}; index++)
                     delivered += hub.Publish("temperature", {{publishedValue}});{{removal}}
                 """;
    }

    internal static void CompileProof()
    {
        using var hub = new KeyedSubscriptionHub<string>();
        using IDisposable registration = hub.Subscribe<int>("value", (_, _) => { },
            options => options.WithObjectFilter(value => value is int).WithValueMapper(value => (int)value!));
        _ = hub.Publish("value", 1);
    }
}
