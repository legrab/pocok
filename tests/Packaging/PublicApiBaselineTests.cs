// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Reflection;
using Pocok.AppDefaults;
using Pocok.AppDefaults.Logging;
using Pocok.AppDefaults.Modularity;
using Pocok.Conversion;
using Pocok.Modularity;
using Pocok.Readiness;
using SerilogDefaultsOptions = Pocok.AppDefaults.Logging.Serilog.SerilogDefaultsOptions;

namespace Pocok.Packaging.Tests;

public sealed class PublicApiBaselineTests
{
    public static IEnumerable<TestCaseData> Packages()
    {
        yield return Case(typeof(IValueConverter).Assembly, "Conversion");
        yield return Case(typeof(IReadinessSignal).Assembly, "Readiness");
        yield return Case(typeof(IApplicationConfigurator).Assembly, "AppDefaults");
        yield return Case(typeof(LoggingDefaultsOptions).Assembly, "AppDefaults.Logging");
        yield return Case(typeof(SerilogDefaultsOptions).Assembly, "AppDefaults.Logging.Serilog");
        yield return Case(typeof(IServiceModule).Assembly, "Modularity.Contracts");
        yield return Case(typeof(ModuleLoader).Assembly, "Modularity");
        yield return Case(typeof(ModularityDefaultsOptions).Assembly, "AppDefaults.Modularity");
    }

    [TestCaseSource(nameof(Packages))]
    public void ExportedTypesMatchReviewedBaseline(Assembly assembly, string projectDirectory)
    {
        var baselinePath = Path.Combine(
            RepositoryRoot.Path,
            "src",
            projectDirectory,
            "PublicAPI.Shipped.txt");
        var expected = File.ReadAllLines(baselinePath)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var actual = assembly.ExportedTypes
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        actual.ShouldBe(expected);
    }

    private static TestCaseData Case(Assembly assembly, string projectDirectory)
    {
        var testName = $"{projectDirectory}_exported_types_match_baseline";
        return new TestCaseData(assembly, projectDirectory) { TestName = testName };
    }
}
