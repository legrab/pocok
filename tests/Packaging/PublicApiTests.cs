// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Reflection;
using PublicApiGenerator;
using VerifyNUnit;

namespace Pocok.Packaging.Tests;

[TestFixture]
public class PublicApiTests
{
    public static IEnumerable<TestCaseData> Packages()
    {
        yield return Case(typeof(Pocok.Conversion.IValueConverter).Assembly, "Conversion");
        yield return Case(typeof(Pocok.Readiness.IReadinessSignal).Assembly, "Readiness");
        yield return Case(typeof(Pocok.AppDefaults.IApplicationConfigurator).Assembly, "AppDefaults");
        yield return Case(typeof(Pocok.AppDefaults.Logging.LoggingDefaultsOptions).Assembly, "AppDefaults.Logging");
        yield return Case(typeof(Pocok.AppDefaults.Logging.Serilog.SerilogDefaultsOptions).Assembly, "AppDefaults.Logging.Serilog");

#if INCLUDE_EXPERIMENTAL
        yield return Case(typeof(Pocok.Modularity.IServiceModule).Assembly, "Modularity.Contracts");
        yield return Case(typeof(Pocok.Modularity.ModuleLoader).Assembly, "Modularity");
        yield return Case(typeof(Pocok.AppDefaults.Modularity.ModularityDefaultsOptions).Assembly, "AppDefaults.Modularity");
#endif
    }

    [TestCaseSource(nameof(Packages))]
    public async Task PublicApiMatchesSnapshot(Assembly assembly, string name)
    {
        var publicApi = assembly.GeneratePublicApi(new ApiGeneratorOptions
        {
            IncludeAssemblyAttributes = false,
            ExcludeAttributes = ["System.Runtime.Versioning.TargetFrameworkAttribute"]
        });
        await Verifier.Verify(publicApi).UseParameters(name);
    }

    private static TestCaseData Case(Assembly assembly, string name)
    {
        return new TestCaseData(assembly, name) { TestName = $"PublicApi_{name}" };
    }
}
