// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using System.Text;
using Microsoft.Extensions.Localization;
using Pocok.Localization;
using Pocok.Localization.Sample;

var directory = Path.Combine(Path.GetTempPath(), $"pocok-localization-sample-{Guid.NewGuid():N}");
Directory.CreateDirectory(directory);
try
{
    await File.WriteAllTextAsync(
        Path.Combine(directory, "Messages.json"),
        """{"Greeting":"Hello {0}","Navigation":{"Home":"Home"}}""",
        new UTF8Encoding(false));
    await File.WriteAllTextAsync(
        Path.Combine(directory, "Messages.de.resx"),
        """<root><data name="Greeting"><value>Hallo {0}</value></data></root>""",
        new UTF8Encoding(false));

    await using var files = new FileStringLocalizer(new FileStringLocalizerOptions
    {
        RootDirectory = directory,
        BaseName = "Messages"
    });
    var localizer = new CompositeStringLocalizer([
        files,
        new DictionaryStringLocalizer(("FallbackOnly", "embedded"))
    ]);

    CultureInfo previousCulture = CultureInfo.CurrentCulture;
    CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
    try
    {
        var culture = CultureInfo.GetCultureInfo("de-DE");
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        LocalizedString greeting = localizer["Greeting", "Pocok"];
        LocalizedString nested = localizer["Navigation.Home"];
        LocalizedString fallback = localizer["FallbackOnly"];
        LocalizedString missing = localizer["Missing"];
        Console.WriteLine(
            $"greeting={greeting.Value} nested={nested.Value} fallback={fallback.Value} " +
            $"missing={missing.ResourceNotFound} valid={files.Status.HasValidSnapshot}");

        return greeting.Value == "Hallo Pocok" &&
               nested.Value == "Home" &&
               fallback.Value == "embedded" &&
               missing.ResourceNotFound &&
               files.Status.HasValidSnapshot
            ? 0
            : 1;
    }
    finally
    {
        CultureInfo.CurrentCulture = previousCulture;
        CultureInfo.CurrentUICulture = previousUiCulture;
    }
}
finally
{
    Directory.Delete(directory, true);
}
