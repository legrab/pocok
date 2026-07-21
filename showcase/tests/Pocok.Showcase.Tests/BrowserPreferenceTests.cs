// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Showcase.Tests;

[TestFixture]
public sealed class BrowserPreferenceTests
{
    [Test]
    public void ThemeAndFeaturePreferencesUseScopedPersistentCookies()
    {
        string script = ReadWebFile("wwwroot", "theme.js");

        script.ShouldContain("pocok.showcase.theme");
        script.ShouldContain("pocok.showcase.feature.");
        script.ShouldContain("Max-Age=${cookieMaxAgeSeconds}; Path=/; SameSite=Lax");
        script.ShouldContain("window.location.protocol === \"https:\"");
        script.ShouldContain("readFeature(\"log-console\", true)");
    }

    [Test]
    public void PreferenceScriptRunsBeforeTheInteractiveApplication()
    {
        string app = ReadWebFile("Components", "App.razor");

        app.IndexOf("<script src=\"theme.js\"></script>", StringComparison.Ordinal)
            .ShouldBeLessThan(app.IndexOf("<Routes @rendermode=\"InteractiveServer\"/>", StringComparison.Ordinal));
    }

    private static string ReadWebFile(params string[] segments) =>
        File.ReadAllText(Path.Combine(
            TestSupport.RepositoryRoot,
            "showcase",
            "src",
            "Pocok.Showcase.Web",
            Path.Combine(segments)));
}
