// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Text.Json;

namespace Pocok.Architecture.Tests;

public sealed class ReleaseReadinessCatalogTests
{
    [Test]
    public void AllNonRetiredLibrariesAreAlphaReleasableAndHaveTagTriggers()
    {
        string root = RepositoryRoot.Path;
        using JsonDocument catalog = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "eng", "packages.json")));
        JsonElement[] packages = catalog.RootElement.GetProperty("packages").EnumerateArray()
            .Where(package => package.GetProperty("state").GetString() != "Retired")
            .ToArray();

        packages.Length.ShouldBe(18);
        packages.ShouldAllBe(package => package.GetProperty("releasable").GetBoolean());

        string workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "publish.yml"));
        foreach (JsonElement package in packages)
        {
            string prefix = package.GetProperty("tagPrefix").GetString()!;
            workflow.ShouldContain($"- '{prefix}*'");
        }
    }

    [Test]
    public void ShowcaseCatalogContainsExactlyTheNonRetiredLibraryIds()
    {
        string root = RepositoryRoot.Path;
        using JsonDocument packages = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "eng", "packages.json")));
        string showcaseCatalogPath = Path.Combine(
            root,
            "showcase",
            "src",
            "Pocok.Showcase.Web",
            "Content",
            "package-catalog.json");
        using JsonDocument showcase = JsonDocument.Parse(File.ReadAllText(showcaseCatalogPath));

        string[] expected = packages.RootElement.GetProperty("packages").EnumerateArray()
            .Where(package => package.GetProperty("state").GetString() != "Retired")
            .Select(package => package.GetProperty("id").GetString()!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] actual = showcase.RootElement.GetProperty("packages").EnumerateArray()
            .Select(package => package.GetProperty("id").GetString()!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        actual.ShouldBe(expected);
    }
}
