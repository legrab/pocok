// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Xml.Linq;

namespace Pocok.Architecture.Tests;

public sealed class RepositoryBoundaryTests
{
    [Test]
    public void ProjectReferencesStayWithinRepository()
    {
        var root = RepositoryRoot.Path;
        var rootPrefix = root + System.IO.Path.DirectorySeparatorChar;

        foreach (var project in Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
                     .Where(IsRepositoryFile))
        {
            var document = XDocument.Load(project);

            foreach (var reference in document.Descendants("ProjectReference"))
            {
                var include = reference.Attribute("Include")?.Value;
                include.ShouldNotBeNullOrWhiteSpace();

                var resolved = System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(System.IO.Path.GetDirectoryName(project)!, include));

                resolved.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase).ShouldBeTrue(
                    $"Project reference escapes the repository: {project} -> {include}");
            }
        }
    }

    [Test]
    public void PackageVersionsAreDefinedCentrally()
    {
        var root = RepositoryRoot.Path;

        foreach (var project in Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
                     .Where(IsRepositoryFile))
        {
            var document = XDocument.Load(project);
            var versionedReferences = document.Descendants("PackageReference")
                .Where(reference => reference.Attribute("Version") is not null)
                .Select(reference => reference.Attribute("Include")?.Value)
                .ToArray();

            versionedReferences.ShouldBeEmpty($"Package versions must be central in {project}.");
        }
    }

    private static bool IsRepositoryFile(string path) =>
        !path.Contains($"{System.IO.Path.DirectorySeparatorChar}obj{System.IO.Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) &&
        !path.Contains($"{System.IO.Path.DirectorySeparatorChar}artifacts{System.IO.Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
}
