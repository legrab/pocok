// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Xml.Linq;

namespace Pocok.Packaging.Tests;

public sealed class RepositoryMetadataTests
{
    [TestCase("LICENSE")]
    [TestCase("NOTICE")]
    [TestCase("README.md")]
    [TestCase("STEWARDSHIP.md")]
    [TestCase("SECURITY.md")]
    [TestCase("CONTRIBUTING.md")]
    public void RequiredPublicFileExists(string relativePath) =>
        File.Exists(System.IO.Path.Combine(RepositoryRoot.Path, relativePath)).ShouldBeTrue();

    [Test]
    public void SharedPackageMetadataIsComplete()
    {
        var document = XDocument.Load(System.IO.Path.Combine(RepositoryRoot.Path, "Directory.Build.props"));
        var properties = document.Descendants("PropertyGroup")
            .Elements()
            .GroupBy(element => element.Name.LocalName)
            .ToDictionary(group => group.Key, group => group.Last().Value);

        properties["PackageLicenseExpression"].ShouldBe("Apache-2.0");
        properties["RepositoryUrl"].ShouldBe("https://github.com/legrab/pocok");
        properties["PackageReadmeFile"].ShouldBe("README.md");
        properties["IncludeSymbols"].ShouldBe("true");
        properties["SymbolPackageFormat"].ShouldBe("snupkg");
        properties["PublishRepositoryUrl"].ShouldBe("true");
        properties["MinVerIgnoreHeight"].ShouldBe("true");
    }

    [Test]
    public void SharedPackageItemsIncludeRequiredDocuments()
    {
        var document = XDocument.Load(System.IO.Path.Combine(RepositoryRoot.Path, "Directory.Build.props"));
        var includes = document.Descendants("None")
            .Select(element => element.Attribute("Include")?.Value)
            .ToArray();

        includes.ShouldContain("$(MSBuildThisFileDirectory)LICENSE");
        includes.ShouldContain("$(MSBuildThisFileDirectory)NOTICE");
        includes.ShouldContain("$(MSBuildProjectDirectory)/README.md");
    }
}
