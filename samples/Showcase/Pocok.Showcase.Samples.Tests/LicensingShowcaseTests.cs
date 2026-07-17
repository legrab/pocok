// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Showcase.Contracts;
using Pocok.Showcase.Licensing;
using Pocok.Showcase.Licensing.Models;

namespace Pocok.Showcase.Samples.Tests;

[TestFixture]
public sealed class LicensingShowcaseTests
{
    private LicensingShowcaseSlice _slice = null!;

    [SetUp]
    public void SetUp() => _slice = new LicensingShowcaseSlice();

    public static IEnumerable<TestCaseData> Samples()
    {
        var slice = new LicensingShowcaseSlice();
        foreach (IShowcaseSample sample in slice.Samples)
            yield return new TestCaseData(sample.Id, sample.ExpectedHeadlineResult).SetName($"Sample_{sample.Id}");
    }

    [TestCaseSource(nameof(Samples))]
    public async Task EverySampleProducesExpectedHeadline(string id, string expected)
    {
        IShowcaseSample sample = _slice.Samples.Single(item => item.Id == id);
        ShowcaseRunResult result = await TestSupport.ExecuteAsync(_slice, sample.CreateInput());
        result.Headline.ShouldBe(expected);
    }

    [Test]
    public async Task EditedModuleIsValidatedThroughTheRealLibrary()
    {
        var input = new LicensingInput
        {
            LicensedModules = "Reporting",
            RequiredModule = "Admin"
        };

        ShowcaseRunResult result = await TestSupport.ExecuteAsync(_slice, input);

        result.Status.ShouldBe(ShowcaseRunStatus.ExpectedFailure);
        result.Headline.ShouldBe("Module missing");
        result.Fields.Single(field => field.Name == "Result.Fields.Code").Value.ShouldBe("ModuleMissing");
    }

    [Test]
    public async Task InvalidTimestampIsRejectedBeforeValidation()
    {
        var input = new LicensingInput { UtcNow = "not-a-date" };

        ShowcaseRunResult result = await TestSupport.ExecuteAsync(_slice, input);

        result.Status.ShouldBe(ShowcaseRunStatus.Rejected);
        result.Diagnostics.Single().Code.ShouldBe("showcase.licensing-input");
    }

    [Test]
    public async Task MatchingPresharedKeySucceeds()
    {
        var input = new LicensingInput
        {
            LicensePresharedKey = "correct-high-entropy-demo-key",
            SuppliedPresharedKey = "correct-high-entropy-demo-key"
        };

        ShowcaseRunResult result = await TestSupport.ExecuteAsync(_slice, input);

        result.Status.ShouldBe(ShowcaseRunStatus.Success);
        result.Headline.ShouldBe("License valid");
    }

    [Test]
    public void SamplesExposeTheirConfiguredEditorValues()
    {
        var missingModule = (LicensingInput)_slice.Samples.Single(item => item.Id == "missing-module").CreateInput();
        var expired = (LicensingInput)_slice.Samples.Single(item => item.Id == "expired").CreateInput();

        missingModule.RequiredModule.ShouldBe("Admin");
        expired.UtcNow.ShouldBe("2027-01-01T00:00:00Z");
    }

    [Test]
    public async Task EditedIdentityAndModuleReachTheRealValidatorAndResult()
    {
        var input = (LicensingInput)_slice.Samples.Single(item => item.Id == "valid-module").CreateInput();

        ShowcaseRunResult result = await TestSupport.ExecuteAsync(
            _slice,
            input with { LicenseId = "edited-license", RequiredModule = "Admin" });

        result.Status.ShouldBe(ShowcaseRunStatus.ExpectedFailure);
        result.Fields.Single(field => field.Name == "Result.Fields.LicenseId").Value.ShouldBe("edited-license");
        result.Fields.Single(field => field.Name == "Result.Fields.RequiredModule").Value.ShouldBe("Admin");
    }
}
