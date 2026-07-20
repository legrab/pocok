// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Reflection;
using Pocok.AppDefaults;
using Pocok.AppDefaults.Licensing;
using Pocok.AppDefaults.Logging;
using Pocok.AppDefaults.Logging.Serilog;
using Pocok.Conversion;
using Pocok.Licensing.Runtime;
using Pocok.Readiness;
using Pocok.Scripting.Execution;
using PublicApiGenerator;

#if INCLUDE_EXPERIMENTAL
using Pocok.AppDefaults.Modularity;
using Pocok.BackgroundWork.Coalescing;
using Pocok.Localization.Composition;
using Pocok.Modularity.Contracts;
using Pocok.Modularity.Loading;
using Pocok.Scripting.CSharp;
using Pocok.Scripting.JavaScript;
using Pocok.Scripting.Python;
using Pocok.Signals.Sources;
using Pocok.Subscriptions;
#endif

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
        yield return Case(typeof(ILicenseService).Assembly, "Licensing.Runtime");
        yield return Case(typeof(LicensingApplicationConfigurator).Assembly, "AppDefaults.Licensing");
        yield return Case(typeof(ScriptRunner).Assembly, "Scripting.Execution");

#if INCLUDE_EXPERIMENTAL
        yield return Case(typeof(JavaScriptScriptEngineAdapter).Assembly, "Scripting.JavaScript");
        yield return Case(typeof(CSharpScriptEngineAdapter).Assembly, "Scripting.CSharp");
        yield return Case(typeof(PythonScriptEngineAdapter).Assembly, "Scripting.Python");
        yield return Case(typeof(IServiceModule).Assembly, "Modularity.Contracts");
        yield return Case(typeof(ModuleLoader).Assembly, "Modularity.Loading");
        yield return Case(typeof(ModularityDefaultsOptions).Assembly, "AppDefaults.Modularity");
        yield return Case(typeof(CoalescingTaskRunner).Assembly, "BackgroundWork.Coalescing");
        yield return Case(typeof(SignalAddress).Assembly, "Signals.Sources");
        yield return Case(typeof(CompositeStringLocalizer).Assembly, "Localization.Composition");
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

    private static TestCaseData Case(Assembly assembly, string name) =>
        new TestCaseData(assembly, name) { TestName = $"PublicApi_{name}" };
}
