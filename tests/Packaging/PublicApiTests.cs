// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Reflection;
using Pocok.AppDefaults;
using Pocok.AppDefaults.Licensing;
using Pocok.AppDefaults.Logging;
using Pocok.AppDefaults.Logging.Serilog;
using Pocok.AppDefaults.Modularity;
using Pocok.BackgroundWork.Coalescing;
using Pocok.Conversion;
using Pocok.Licensing.Runtime;
using Pocok.Localization.Composition;
using Pocok.Modularity.Contracts;
using Pocok.Modularity.Loading;
using Pocok.Readiness;
using Pocok.Scripting.Execution;
using Pocok.Signals.Sources;
using Pocok.Subscriptions;
using PublicApiGenerator;

namespace Pocok.Packaging.Tests;

[TestFixture]
public class PublicApiTests
{
    public static IEnumerable<TestCaseData> Packages()
    {
        yield return Case(typeof(IValueConverter).Assembly, "Conversion");
        yield return Case(typeof(IReadinessSignal).Assembly, "Readiness");
        yield return Case(typeof(IApplicationConfigurator).Assembly, "AppDefaults");
        yield return Case(typeof(LoggingDefaultsOptions).Assembly, "AppDefaults.Logging");
        yield return Case(typeof(SerilogDefaultsOptions).Assembly, "AppDefaults.Logging.Serilog");

#if INCLUDE_EXPERIMENTAL
        yield return Case(typeof(IServiceModule).Assembly, "Modularity.Contracts");
        yield return Case(typeof(ModuleLoader).Assembly, "Modularity");
        yield return Case(typeof(ModularityDefaultsOptions).Assembly, "AppDefaults.Modularity");
        yield return Case(typeof(ILicenseService).Assembly, "Licensing");
        yield return Case(typeof(LicensingApplicationConfigurator).Assembly, "AppDefaults.Licensing");
        yield return Case(typeof(CoalescingTaskRunner).Assembly, "BackgroundWork");
        yield return Case(typeof(ScriptRunner).Assembly, "Scripting");
        yield return Case(typeof(SignalAddress).Assembly, "Signals");
        yield return Case(typeof(CompositeStringLocalizer).Assembly, "Localization");
        yield return Case(typeof(KeyedSubscriptionHub<>).Assembly, "Subscriptions");
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
        await Verify(publicApi).UseParameters(name);
    }

    private static TestCaseData Case(Assembly assembly, string name)
    {
        return new TestCaseData(assembly, name) { TestName = $"PublicApi_{name}" };
    }
}
