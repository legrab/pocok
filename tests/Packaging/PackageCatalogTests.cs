// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Text.Json;
using System.Xml.Linq;

namespace Pocok.Packaging.Tests;

public sealed class PackageCatalogTests
{
    [Test]
    public void EveryActivePackableProjectHasOneCatalogEntry()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(RepositoryRoot.Path, "eng", "packages.json")));
        var activeProjects = document.RootElement.GetProperty("packages")
            .EnumerateArray()
            .Where(package => package.GetProperty("state").GetString() != "Retired")
            .Select(package => package.GetProperty("project").GetString()!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var packableProjects = Directory.EnumerateFiles(
                Path.Combine(RepositoryRoot.Path, "src"),
                "*.csproj",
                SearchOption.AllDirectories)
            .Where(path => string.Equals(
                XDocument.Load(path).Descendants("IsPackable").LastOrDefault()?.Value,
                "true",
                StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(RepositoryRoot.Path, path).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToArray();

        activeProjects.ShouldBe(packableProjects);
    }

    [Test]
    public void NonReleasablePackagesHaveNoPublishTagTrigger()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(RepositoryRoot.Path, "eng", "packages.json")));
        var workflow = File.ReadAllText(Path.Combine(
            RepositoryRoot.Path,
            ".github",
            "workflows",
            "publish.yml"));

        foreach (JsonElement package in document.RootElement.GetProperty("packages").EnumerateArray()
                     .Where(package => !package.GetProperty("releasable").GetBoolean()))
        {
            var prefix = package.GetProperty("tagPrefix").GetString();
            workflow.ShouldNotContain($"- '{prefix}*'");
        }
    }

    [Test]
    public void ReleasablePackagesHavePublishTagTriggers()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(RepositoryRoot.Path, "eng", "packages.json")));
        var workflow = File.ReadAllText(Path.Combine(
            RepositoryRoot.Path,
            ".github",
            "workflows",
            "publish.yml"));

        foreach (JsonElement package in document.RootElement.GetProperty("packages").EnumerateArray()
                     .Where(package => package.GetProperty("releasable").GetBoolean()))
        {
            var prefix = package.GetProperty("tagPrefix").GetString();
            workflow.ShouldContain($"- '{prefix}*'");
        }
    }
}
