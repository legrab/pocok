// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using System.Text.Json;
using Pocok.Showcase.Contracts;
using Pocok.Showcase.Web.Services;

namespace Pocok.Showcase.Tests;

[TestFixture, NonParallelizable]
public sealed class CatalogAndLocalizationTests
{
    [Test]
    public void PackageCatalogLoadsCurrentPackages()
    {
        var catalog = new ShowcasePackageCatalog(TestSupport.WebEnvironment());
        catalog.Packages.Count.ShouldBe(18);
        catalog.Find("Pocok.Conversion").ShouldNotBeNull();
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
    public async Task ShellLocalizationLoads()
    {
        FakeWebHostEnvironment environment = TestSupport.WebEnvironment();
        ShowcaseResourceRegistration[] registrations =
        [new("shell", environment.ContentRootPath, "Content/Locales/Shell")];
        await using var text = new ShowcaseTextCatalog(registrations, environment);
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("hu");
            text.GetText("shell", "Navigation.Home").ShouldBe("Főoldal");
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Test]
    public void ShellLocalizationsHaveMatchingKeys()
    {
        string directory = Path.Combine(
            TestSupport.RepositoryRoot,
            "showcase",
            "src",
            "Pocok.Showcase.Web",
            "Content",
            "Locales");

        ReadResourceKeys(Path.Combine(directory, "Shell.json"))
            .ShouldBe(ReadResourceKeys(Path.Combine(directory, "Shell.hu.json")));
    }

    [Test]
    public void PublisherDiscoversSlicesFromManifests()
    {
        string source = File.ReadAllText(Path.Combine(
            TestSupport.RepositoryRoot,
            "showcase",
            "tools",
            "Pocok.Showcase.PublishTool",
            "Program.cs"));
        source.ShouldContain("DiscoverPluginProjects");
        source.ShouldContain("pocok.module.json");
        source.ShouldNotContain("Pocok.Showcase.Conversion.csproj");
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
