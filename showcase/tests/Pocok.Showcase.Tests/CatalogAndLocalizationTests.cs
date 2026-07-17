// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Pocok.Showcase.Contracts;
using Pocok.Showcase.Conversion;
using Pocok.Showcase.Web.Services;

namespace Pocok.Showcase.Tests;

[TestFixture, NonParallelizable]
public sealed class CatalogAndLocalizationTests
{
    [Test]
    public void PackageCatalogLoadsCurrentPackages()
    {
        var catalog = new ShowcasePackageCatalog(TestSupport.WebEnvironment());
        catalog.Packages.Count.ShouldBe(15);
        catalog.Find("Pocok.Conversion").ShouldNotBeNull();
    }

    [Test]
    public void PartialCatalogAcceptsOneInstalledSlice()
    {
        var packages = new ShowcasePackageCatalog(TestSupport.WebEnvironment());
        var catalog = new ShowcaseSliceCatalog([new ConversionShowcaseSlice()], packages,
            Options.Create(new ShowcaseOptions { RequireCompleteCatalog = false }));
        catalog.Installed.Count.ShouldBe(1);
        catalog.CreateFacts(packages).Count(item => item.ImplementationStatus == ShowcaseImplementationStatus.Planned)
            .ShouldBe(14);
    }

    [Test]
    public void StrictCatalogRejectsMissingSlices()
    {
        var packages = new ShowcasePackageCatalog(TestSupport.WebEnvironment());
        Should.Throw<InvalidOperationException>(() => new ShowcaseSliceCatalog([new ConversionShowcaseSlice()], packages,
            Options.Create(new ShowcaseOptions { RequireCompleteCatalog = true }))).Message.ShouldContain("missing");
    }

    [Test]
    public async Task ShellAndSliceLocalizationLoad()
    {
        FakeWebHostEnvironment environment = TestSupport.WebEnvironment();
        ShowcaseResourceRegistration[] registrations =
        [
            new("shell", environment.ContentRootPath, "Content/Locales/Shell"),
            new("conversion", Path.Combine(TestSupport.RepositoryRoot, "samples", "Showcase", "Pocok.Showcase.Conversion"), "Content/Locales/Conversion")
        ];
        await using var text = new ShowcaseTextCatalog(registrations, environment);
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("hu");
            text.GetText("conversion", "Sandbox.Run").ShouldBe("Konverzió futtatása");
            text.GetText("shell", "Navigation.Home").ShouldBe("Csomagok");
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    [Test]
    public async Task CultureLookupsRemainIsolated()
    {
        FakeWebHostEnvironment environment = TestSupport.WebEnvironment();
        ShowcaseResourceRegistration[] registrations =
        [new("conversion", Path.Combine(TestSupport.RepositoryRoot, "samples", "Showcase", "Pocok.Showcase.Conversion"), "Content/Locales/Conversion")];
        await using var text = new ShowcaseTextCatalog(registrations, environment);
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
            text.GetText("conversion", "Package.Name").ShouldBe("Conversion");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("hu");
            text.GetText("conversion", "Package.Name").ShouldBe("Konverzió");
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }
    [Test]
    public void CatalogSlugsAreUniqueAndResolvePlannedPackages()
    {
        var catalog = new ShowcasePackageCatalog(TestSupport.WebEnvironment());
        catalog.Packages.Select(package => package.Slug)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count()
            .ShouldBe(catalog.Packages.Count);
        catalog.FindBySlug("readiness")?.Id.ShouldBe("Pocok.Readiness");
    }

    [Test]
    public void ConversionManifestSharesStableHostAssemblies()
    {
        string path = Path.Combine(
            TestSupport.RepositoryRoot,
            "samples",
            "Showcase",
            "Pocok.Showcase.Conversion",
            "pocok.module.json");
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(path));
        string[] shared = manifest.RootElement.GetProperty("sharedAssemblies")
            .EnumerateArray()
            .Select(item => item.GetString()!)
            .ToArray();
        shared.ShouldContain("Pocok.Showcase.Contracts");
        shared.ShouldContain("Pocok.Showcase.Components");
    }

    [Test]
    public void PublisherDiscoversSlicesWithoutAHostProjectList()
    {
        string source = File.ReadAllText(Path.Combine(
            TestSupport.RepositoryRoot,
            "showcase",
            "tools",
            "Pocok.Showcase.PublishTool",
            "Program.cs"));
        source.ShouldContain("Directory.EnumerateFiles(samplesRoot");
        source.ShouldContain("\"Pocok.Showcase.*.csproj\"");
        source.ShouldNotContain("Pocok.Showcase.Conversion.csproj");
    }

}
