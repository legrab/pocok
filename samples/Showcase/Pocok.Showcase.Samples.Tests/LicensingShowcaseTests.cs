// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;

using Pocok.Licensing.Documents;
using Pocok.Licensing.Validation;
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
    public void SamplesCreateFreshInputs()
    {
        foreach (IShowcaseSample sample in _slice.Samples)
        {
            object first = sample.CreateInput();
            object second = sample.CreateInput();

            ReferenceEquals(first, second).ShouldBeFalse($"Sample '{sample.Id}' reused its input instance.");
        }
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

    [Test]
    public void GeneratedLicenseCanBeImportedAndValidatedAtRuntime()
    {
        var input = (LicensingInput)_slice.Samples.Single(item => item.Id == "valid-module").CreateInput();

        GeneratedLicenseOutput generated = LicensingShowcaseSlice.GenerateLicense(input);
        LicenseValidationResult verified = LicenseReader.ReadAndVerify(
            generated.SignedLicense,
            new Dictionary<string, string> { [generated.KeyId] = generated.PublicKeyPem });

        verified.IsValid.ShouldBeTrue();
        verified.License!.LicenseId.ShouldBe(input.LicenseId);

        LicenseValidationResult runtime = LicenseValidator.Validate(
            verified.License!,
            new LicenseValidationContext
            {
                UtcNow = DateTimeOffset.Parse(input.UtcNow, CultureInfo.InvariantCulture),
                ProcessRuntime = TimeSpan.FromMinutes(input.ProcessRuntimeMinutes),
                RequiredModule = input.RequiredModule
            });

        runtime.IsValid.ShouldBeTrue();
    }

    [Test]
    public void LicenseEditorSectionsAreOpenAndCollapsibleByDefault()
    {
        string editor = File.ReadAllText(Path.Combine(
            TestSupport.RepositoryRoot,
            "samples",
            "Showcase",
            "Pocok.Showcase.Licensing",
            "LicensingEditor.razor"));

        editor.Split("<details class=\"panel collapsible-support editor-section\" open>", StringSplitOptions.None)
            .Length.ShouldBe(3);
    }

    [Test]
    public void LicenseGenerationUsesAnInteractiveSharedButton()
    {
        string page = File.ReadAllText(Path.Combine(
            TestSupport.RepositoryRoot,
            "samples",
            "Showcase",
            "Pocok.Showcase.Licensing",
            "LicensingPage.razor"));

        page.ShouldContain("<ShowcaseRunButton");
        page.ShouldContain("Clicked=\"GenerateLicense\"");
        page.ShouldNotContain("@onclick=\"GenerateLicense\"");
    }
}
