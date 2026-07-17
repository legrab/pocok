// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Pocok.Showcase.Contracts;
using Pocok.Showcase.Conversion;
using Pocok.Showcase.Web.Services;

namespace Pocok.Showcase.Samples.Tests;

[TestFixture, NonParallelizable]
public sealed class CatalogAndLocalizationTests
{
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
            text.GetText("conversion", "Sandbox.ShowSuggestions").ShouldBe("Javaslatok megnyitása");
            text.GetText("shell", "Navigation.Home").ShouldBe("Főoldal");
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
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
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Test]
    public void SampleLocalizationsHaveMatchingKeys()
    {
        string samplesRoot = Path.Combine(TestSupport.RepositoryRoot, "samples", "Showcase");
        (string Directory, string BaseName)[] resources =
        [
            ("Pocok.Showcase.Conversion", "Conversion"),
            ("Pocok.Showcase.Scripting", "Scripting"),
            ("Pocok.Showcase.Licensing", "Licensing")
        ];

        foreach ((string directory, string baseName) in resources)
        {
            string localeRoot = Path.Combine(samplesRoot, directory, "Content", "Locales");
            ReadResourceKeys(Path.Combine(localeRoot, $"{baseName}.json"))
                .ShouldBe(ReadResourceKeys(Path.Combine(localeRoot, $"{baseName}.hu.json")));
        }
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

    private static string[] ReadResourceKeys(string path)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        return EnumerateKeys(document.RootElement, string.Empty)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<string> EnumerateKeys(JsonElement element, string prefix)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            string key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (string nested in EnumerateKeys(property.Value, key))
                    yield return nested;
            }
            else
            {
                yield return key;
            }
        }
    }
}
