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
        yield return Case(typeof(Conversion.IValueConverter).Assembly, "Conversion");
        yield return Case(typeof(Readiness.IReadinessSignal).Assembly, "Readiness");
        yield return Case(typeof(AppDefaults.IApplicationConfigurator).Assembly, "AppDefaults");
        yield return Case(typeof(AppDefaults.Logging.LoggingDefaultsOptions).Assembly, "AppDefaults.Logging");
        yield return Case(typeof(Pocok.AppDefaults.Logging.Serilog.SerilogDefaultsOptions).Assembly, "AppDefaults.Logging.Serilog");

#if INCLUDE_EXPERIMENTAL
        yield return Case(typeof(Modularity.IServiceModule).Assembly, "Modularity.Contracts");
        yield return Case(typeof(Modularity.ModuleLoader).Assembly, "Modularity");
        yield return Case(typeof(Pocok.AppDefaults.Modularity.ModularityDefaultsOptions).Assembly, "AppDefaults.Modularity");
        yield return Case(typeof(Pocok.Licensing.ILicenseService).Assembly, "Licensing");
        yield return Case(typeof(Pocok.AppDefaults.Licensing.LicensingApplicationConfigurator).Assembly, "AppDefaults.Licensing");
        yield return Case(typeof(Pocok.BackgroundWork.CoalescingTaskRunner).Assembly, "BackgroundWork");
        yield return Case(typeof(Pocok.Scripting.ScriptRunner).Assembly, "Scripting");
        yield return Case(typeof(Pocok.Signals.SignalAddress).Assembly, "Signals");
        yield return Case(typeof(Pocok.Localization.CompositeStringLocalizer).Assembly, "Localization");
        yield return Case(typeof(Pocok.Subscriptions.KeyedSubscriptionHub<>).Assembly, "Subscriptions");
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
