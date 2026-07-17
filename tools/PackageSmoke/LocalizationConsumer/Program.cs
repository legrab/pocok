// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using System.Text;
using Microsoft.Extensions.Localization;
using Pocok.Localization.Composition;
using Pocok.Localization.Cultures;
using Pocok.Localization.FileResources;

string directory = Path.Combine(Path.GetTempPath(), $"pocok-localization-consumer-{Guid.NewGuid():N}");
Directory.CreateDirectory(directory);
try
{
    string jsonPath = Path.Combine(directory, "Messages.json");
    string resxPath = Path.Combine(directory, "Messages.de.resx");
    await File.WriteAllTextAsync(
        jsonPath,
        """{"Greeting":"Hello {0}","Navigation":{"Home":"Home"}}""",
        new UTF8Encoding(false));
    await File.WriteAllTextAsync(
        resxPath,
        """<root><data name="Greeting"><value>Hallo {0}</value></data></root>""",
        new UTF8Encoding(false));

    await using var files = new FileStringLocalizer(new FileStringLocalizerOptions
    {
        RootDirectory = directory,
        BaseName = "Messages"
    });
    var localizer = new CompositeStringLocalizer([files, new EmptyStringLocalizer()]);
    using var culture = new TemporaryCulture("de-DE");

    LocalizedString greeting = localizer["Greeting", "Pocok"];
    LocalizedString home = localizer["Navigation.Home"];
    CultureInfo resourceCulture = ResourceCulture.GetCultureFromFileName(
        "messages.de-DE.json",
        CultureInfo.InvariantCulture);
    string status = ConsumerStatus.Ready.Translate(localizer);

    return greeting.Value == "Hallo Pocok" &&
           home.Value == "Home" &&
           resourceCulture.Name == "de-DE" &&
           status == "Ready" &&
           files.Status.HasValidSnapshot
        ? 0
        : 1;
}
finally
{
    Directory.Delete(directory, true);
}

enum ConsumerStatus
{
    Ready
}

sealed class EmptyStringLocalizer : IStringLocalizer
{
    public LocalizedString this[string name] => new(name, name, true);
    public LocalizedString this[string name, params object[] arguments] => new(name, name, true);
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
}

sealed class TemporaryCulture : IDisposable
{
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;

    public TemporaryCulture(string name)
    {
        CultureInfo culture = CultureInfo.GetCultureInfo(name);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
    }
}
