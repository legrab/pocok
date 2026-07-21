// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Pocok.Scripting.Execution;
using Pocok.Scripting.JavaScript;
using Pocok.Showcase.AppDefaults.Logging;
using Pocok.Showcase.BackgroundWork;
using Pocok.Showcase.Contracts;
using Pocok.Showcase.Conversion;
using Pocok.Showcase.Licensing;
using Pocok.Showcase.Localization;
using Pocok.Showcase.Modularity;
using Pocok.Showcase.Readiness;
using Pocok.Showcase.Scripting;
using Pocok.Showcase.Signals;
using Pocok.Showcase.Subscriptions;
using Pocok.Showcase.Web.Services;

namespace Pocok.Showcase.Samples.Tests;

[TestFixture]
[NonParallelizable]
public sealed class CatalogAndLocalizationTests
{
    private static IShowcaseSlice[] AllSlices()
    {
        return
        [
            new ConversionShowcaseSlice(), CreateScriptingSlice(), new LicensingShowcaseSlice(),
            new LoggingShowcaseSlice(), new LocalizationShowcaseSlice(), new ReadinessShowcaseSlice(),
            new BackgroundWorkShowcaseSlice(), new ModularityShowcaseSlice(), new SignalsShowcaseSlice(),
            new SubscriptionsShowcaseSlice()
        ];
    }

    [Test]
    public void PartialCatalogAcceptsOneInstalledSlice()
    {
        var packages = new ShowcasePackageCatalog(TestSupport.WebEnvironment());
        var catalog = new ShowcaseSliceCatalog([new ConversionShowcaseSlice()], packages,
            Options.Create(new ShowcaseOptions { RequireCompleteCatalog = false }));
        catalog.Installed.Count.ShouldBe(1);
        catalog.CreateFacts(packages).Count(item => item.ImplementationStatus == ShowcaseImplementationStatus.Planned)
            .ShouldBe(17);
    }

    [Test]
    public void StrictCatalogAcceptsTheTenPluginEighteenPackageCoverageMap()
    {
        var packages = new ShowcasePackageCatalog(TestSupport.WebEnvironment());
        var catalog = new ShowcaseSliceCatalog(AllSlices(), packages,
            Options.Create(new ShowcaseOptions { RequireCompleteCatalog = true }));
        catalog.Installed.Count.ShouldBe(10);
        catalog.CreateFacts(packages).Count(fact => fact.ImplementationStatus == ShowcaseImplementationStatus.Available)
            .ShouldBe(18);
    }

    [Test]
    public void ExactCoverageHasNoMissingOrDuplicatePackageOwner()
    {
        var packages = new ShowcasePackageCatalog(TestSupport.WebEnvironment());
        var catalog = new ShowcaseSliceCatalog(
            AllSlices(),
            packages,
            Options.Create(new ShowcaseOptions { RequireCompleteCatalog = true }));
        var covered = catalog.CreateFacts(packages)
            .Where(fact => fact.ImplementationStatus == ShowcaseImplementationStatus.Available)
            .Select(fact => fact.Id)
            .Order(StringComparer.Ordinal)
            .ToArray();
        covered.Length.ShouldBe(18);
        covered.Distinct(StringComparer.Ordinal).Count().ShouldBe(18);
    }

    [Test]
    public async Task ShellAndSliceLocalizationLoad()
    {
        FakeWebHostEnvironment environment = TestSupport.WebEnvironment();
        ShowcaseResourceRegistration[] registrations =
        [
            new("shell", environment.ContentRootPath, "Content/Locales/Shell"),
            new(
                "localization",
                Path.Combine(
                    TestSupport.RepositoryRoot,
                    "samples",
                    "Showcase",
                    "Pocok.Showcase.Localization"),
                "Content/Locales/Localization")
        ];
        await using var text = new ShowcaseTextCatalog(registrations, environment);
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("hu");
            text.GetText("localization", "Sandbox.Run").ShouldBe("Erőforrások betöltése");
            text.GetText("shell", "Navigation.Home").ShouldBe("Főoldal");
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Test]
    public void AllTenPluginLocalizationsHaveMatchingKeys()
    {
        var samplesRoot = Path.Combine(TestSupport.RepositoryRoot, "samples", "Showcase");
        (string Directory, string BaseName)[] resources =
        [
            ("Pocok.Showcase.Conversion", "Conversion"), ("Pocok.Showcase.Scripting", "Scripting"),
            ("Pocok.Showcase.Licensing", "Licensing"), ("Pocok.Showcase.AppDefaults.Logging", "Logging"),
            ("Pocok.Showcase.Localization", "Localization"), ("Pocok.Showcase.Readiness", "Readiness"),
            ("Pocok.Showcase.BackgroundWork", "BackgroundWork"), ("Pocok.Showcase.Modularity", "Modularity"),
            ("Pocok.Showcase.Signals", "Signals"), ("Pocok.Showcase.Subscriptions", "Subscriptions")
        ];
        foreach (var (directory, baseName) in resources)
        {
            var localeRoot = Path.Combine(samplesRoot, directory, "Content", "Locales");
            ReadResourceKeys(Path.Combine(localeRoot, $"{baseName}.json"))
                .ShouldBe(ReadResourceKeys(Path.Combine(localeRoot, $"{baseName}.hu.json")));
        }
    }

    private static ScriptingShowcaseSlice CreateScriptingSlice()
    {
        var registry = new ScriptEngineRegistry(
        [
            new JavaScriptScriptEngineAdapter(),
            new UnavailableScriptEngineAdapter(ScriptEngineId.CSharp, "C#", "scripting.engine.trusted_only",
                "C# requires explicit enablement."),
            new UnavailableScriptEngineAdapter(
                ScriptEngineId.Python,
                "Python",
                "scripting.engine.trusted_only",
                "Python requires explicit enablement.")
        ]);
        return new ScriptingShowcaseSlice(
            new ScriptRunner(registry),
            registry,
            new ScriptingShowcaseOptions());
    }

    private static string[] ReadResourceKeys(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return EnumerateKeys(document.RootElement, string.Empty).Order(StringComparer.Ordinal).ToArray();
    }

    private static IEnumerable<string> EnumerateKeys(JsonElement element, string prefix)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
            if (property.Value.ValueKind == JsonValueKind.Object)
                foreach (var nested in EnumerateKeys(property.Value, key))
                    yield return nested;
            else yield return key;
        }
    }
}
