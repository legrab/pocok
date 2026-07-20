// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Showcase.AppDefaults.Logging;
using Pocok.Showcase.BackgroundWork;
using Pocok.Showcase.Contracts;
using Pocok.Showcase.Localization;
using Pocok.Showcase.Modularity;
using Pocok.Showcase.Readiness;
using Pocok.Showcase.Signals;
using Pocok.Showcase.Subscriptions;

namespace Pocok.Showcase.Samples.Tests;

[TestFixture, NonParallelizable]
public sealed class ReleaseReadinessShowcaseTests
{
    public static IEnumerable<TestCaseData> Samples()
    {
        IShowcaseSlice[] slices =
        [
            new LoggingShowcaseSlice(),
            new LocalizationShowcaseSlice(),
            new ReadinessShowcaseSlice(),
            new BackgroundWorkShowcaseSlice(),
            new ModularityShowcaseSlice(),
            new SignalsShowcaseSlice(),
            new SubscriptionsShowcaseSlice()
        ];

        foreach (IShowcaseSlice slice in slices)
            foreach (IShowcaseSample sample in slice.Samples)
                yield return new TestCaseData(slice, sample.Id, sample.ExpectedHeadlineResult)
                    .SetName($"{slice.Descriptor.PackageId}_{sample.Id}");
    }

    [TestCaseSource(nameof(Samples))]
    public async Task EveryPhaseTwoSampleProducesItsExpectedHeadline(
        IShowcaseSlice slice,
        string sampleId,
        string expectedHeadline)
    {
        IShowcaseSample sample = slice.Samples.Single(item => item.Id == sampleId);
        ShowcaseRunResult result = await TestSupport.ExecuteAsync(slice, sample.CreateInput());
        result.Headline.ShouldBe(expectedHeadline);
    }

    [Test]
    public void RecipeRenderersUseCurrentPublicApiNames()
    {
        ReadinessRecipeRenderer.Render(new ReadinessInput()).ShouldContain("ReadinessSource");
        BackgroundWorkRecipeRenderer.Render(new BackgroundWorkInput { Mode = "debounce" }).ShouldContain("DebouncedTaskRunner");
        ModularityRecipeRenderer.Render(new ModularityInput { Mode = "manifest" }).ShouldContain("pocok.module.json");
        SignalsRecipeRenderer.Render(new SignalsInput { Mode = "subscribe" }).ShouldContain("SignalQuality");
        SubscriptionsRecipeRenderer.Render(new SubscriptionsInput { Mode = "filter-map" }).ShouldContain("WithValueMapper");
    }

    [Test]
    public async Task LoggingSampleEmitsOnlyBoundedSafeRecords()
    {
        var slice = new LoggingShowcaseSlice();
        ShowcaseRunResult result = await TestSupport.ExecuteAsync(slice, new LoggingInput { EventCount = 25, IncludeException = true });
        result.Status.ShouldBe(ShowcaseRunStatus.Success);
        result.Fields.Count.ShouldBe(20);
        result.Fields.ShouldAllBe(field => !field.Value!.Contains(" at ", StringComparison.Ordinal));
    }

    [Test]
    public async Task LocalizationSampleUsesRealProvidersAndDoesNotExposeTheTemporaryPath()
    {
        var slice = new LocalizationShowcaseSlice();
        ShowcaseRunResult result = await TestSupport.ExecuteAsync(slice, new LocalizationInput { Culture = "hu-HU", Reload = false });
        result.Status.ShouldBe(ShowcaseRunStatus.Success);
        result.Headline.ShouldBe("Szia Pocok");
        result.Fields.ShouldAllBe(field => !field.Value!.Contains(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public void EveryNewSliceHasFreshSamplesAndExactlyOneDefault()
    {
        IShowcaseSlice[] slices =
        [
            new LoggingShowcaseSlice(), new LocalizationShowcaseSlice(), new ReadinessShowcaseSlice(),
            new BackgroundWorkShowcaseSlice(), new ModularityShowcaseSlice(), new SignalsShowcaseSlice(),
            new SubscriptionsShowcaseSlice()
        ];
        foreach (IShowcaseSlice slice in slices)
        {
            slice.Samples.Count(sample => sample.IsDefault).ShouldBe(1);
            IShowcaseSample sample = slice.Samples[0];
            ReferenceEquals(sample.CreateInput(), sample.CreateInput()).ShouldBeFalse();
        }
    }
}
