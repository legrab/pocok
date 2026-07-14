// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Architecture.Tests;

public sealed class SourceHeaderTests
{
    private const string Header = "// SPDX-License-Identifier: Apache-2.0\n// Copyright 2026 Pocok contributors";

    [Test]
    public void HandAuthoredSourceFilesCarryTheLicenseHeader()
    {
        var files = Directory.EnumerateFiles(RepositoryRoot.Path, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{System.IO.Path.DirectorySeparatorChar}obj{System.IO.Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var missingHeaders = files
            .Where(path => !File.ReadAllText(path).Replace("\r\n", "\n").StartsWith(Header, StringComparison.Ordinal))
            .Select(path => System.IO.Path.GetRelativePath(RepositoryRoot.Path, path))
            .ToArray();

        missingHeaders.ShouldBeEmpty();
    }
}
