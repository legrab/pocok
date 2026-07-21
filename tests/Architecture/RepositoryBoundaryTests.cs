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
        var rootPrefix = root + Path.DirectorySeparatorChar;

        foreach (var project in Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
                     .Where(IsRepositoryFile))
        {
            var document = XDocument.Load(project);

            foreach (XElement reference in document.Descendants("ProjectReference"))
            {
                var include = reference.Attribute("Include")?.Value;
                include.ShouldNotBeNullOrWhiteSpace();

                var resolved = Path.GetFullPath(
                    Path.Combine(Path.GetDirectoryName(project)!, include));

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

    [Test]
    public void PackableProjectsReferenceOnlyPackableRuntimeProjectsOrPrivateBuildAssets()
    {
        var root = RepositoryRoot.Path;
        var projects = Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(IsRepositoryFile)
            .ToDictionary(Path.GetFullPath, XDocument.Load, StringComparer.OrdinalIgnoreCase);

        foreach ((var projectPath, XDocument document) in projects)
        {
            if (!IsPackable(document)) continue;

            foreach (XElement reference in document.Descendants("ProjectReference"))
            {
                var include = reference.Attribute("Include")?.Value;
                include.ShouldNotBeNullOrWhiteSpace();
                var referencedPath = Path.GetFullPath(
                    Path.Combine(Path.GetDirectoryName(projectPath)!, include));

                projects.ContainsKey(referencedPath)
                    .ShouldBeTrue($"Unknown project reference: {projectPath} -> {include}");
                if (IsPackable(projects[referencedPath])) continue;

                IsApprovedPrivateBuildAssetReference(projectPath, referencedPath, reference).ShouldBeTrue(
                    $"Packable project {projectPath} references unapproved non-packable project {referencedPath}.");
            }
        }
    }

    [Test]
    public void RepositoryDoesNotIntroduceCatchAllProjects()
    {
        var forbiddenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Common",
            "Utils",
            "Utilities",
            "Foundation",
            "SharedKernel"
        };

        var offenders = Directory.EnumerateFiles(RepositoryRoot.Path, "*.csproj", SearchOption.AllDirectories)
            .Where(IsRepositoryFile)
            .Where(path => forbiddenNames.Contains(Path.GetFileNameWithoutExtension(path)))
            .Select(path => Path.GetRelativePath(RepositoryRoot.Path, path))
            .ToArray();

        offenders.ShouldBeEmpty();
    }

    [Test]
    public void SharedSourceIsLinkedExplicitlyAndRemainsInternal()
    {
        var root = RepositoryRoot.Path;
        var sharedRoot = Path.Combine(root, "src", "Shared");
        var sharedFiles = Directory.Exists(sharedRoot)
            ? Directory.EnumerateFiles(sharedRoot, "*.cs", SearchOption.AllDirectories).ToArray()
            : [];

        foreach (var file in sharedFiles)
        {
            var text = File.ReadAllText(file);
            text.ShouldNotContain("public class ");
            text.ShouldNotContain("public static class ");
            text.ShouldNotContain("public interface ");
            text.ShouldNotContain("public record ");
            text.ShouldNotContain("public enum ");
        }

        foreach (var project in Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
                     .Where(IsRepositoryFile))
        {
            var document = XDocument.Load(project);
            foreach (XElement compile in document.Descendants("Compile"))
            {
                var include = compile.Attribute("Include")?.Value;
                if (include is null || !include.Replace('\\', '/')
                        .Contains("src/Shared/", StringComparison.OrdinalIgnoreCase)) continue;

                include.ShouldNotContain("*");
                compile.Attribute("Link")?.Value.ShouldNotBeNullOrWhiteSpace();
            }
        }
    }

    private static bool IsApprovedPrivateBuildAssetReference(
        string projectPath,
        string referencedPath,
        XElement reference)
    {
        var root = RepositoryRoot.Path;
        var adapter = Path.GetFullPath(Path.Combine(
            root,
            "src",
            "Scripting.CSharp",
            "Pocok.Scripting.CSharp.csproj"));
        var worker = Path.GetFullPath(Path.Combine(
            root,
            "src",
            "Scripting.CSharp.Worker",
            "Pocok.Scripting.CSharp.Worker.csproj"));

        return string.Equals(projectPath, adapter, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(referencedPath, worker, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(
                   reference.Attribute("ReferenceOutputAssembly")?.Value,
                   "false",
                   StringComparison.OrdinalIgnoreCase) &&
               string.Equals(
                   reference.Attribute("PrivateAssets")?.Value,
                   "all",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPackable(XDocument document)
    {
        return string.Equals(
            document.Descendants("IsPackable").LastOrDefault()?.Value,
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRepositoryFile(string path)
    {
        return !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                   StringComparison.OrdinalIgnoreCase) &&
               !path.Contains($"{Path.DirectorySeparatorChar}artifacts{Path.DirectorySeparatorChar}",
                   StringComparison.OrdinalIgnoreCase);
    }
}
