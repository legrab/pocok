// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pocok.Modularity.Fixtures;

namespace Pocok.Modularity.Integration.Tests;

public sealed class ModuleLoaderIntegrationTests
{
    [Test]
    public void PluginLoadsPrivateDependencyAndSharesApplicationContract()
    {
        using var fixture = PluginFixture.Create();
        var services = new ServiceCollection();
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Modules:fixture.greeting:Prefix"] = "Welcome"
            })
            .Build();

        ModuleCatalog catalog = ModuleLoader.Load(services, configuration, Options(fixture.Root));
        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IGreetingProvider>().Greet("Pocok").ShouldBe("Welcome Pocok!");
        catalog.Modules.ShouldHaveSingleItem().Status.ShouldBe(ModuleStatus.Registered);
        catalog.Modules[0].Identity!.Id.ShouldBe("fixture.greeting");
    }

    [Test]
    public void MultiplePluginsRegisterOrdinaryEnumerableServices()
    {
        using var first = PluginFixture.Create();
        using var second = PluginFixture.Create();
        ModuleManifest secondManifest = second.ReadManifest();
        secondManifest.Id = "fixture.greeting.secondary";
        second.WriteManifest(secondManifest);
        var root = PluginFixture.CreateRootContaining(first, second);
        var services = new ServiceCollection();

        ModuleCatalog catalog = ModuleLoader.Load(
            services,
            new ConfigurationBuilder().Build(),
            Options(root));
        using ServiceProvider provider = services.BuildServiceProvider();

        catalog.Modules.Count.ShouldBe(2);
        catalog.Modules.ShouldAllBe(module => module.Status == ModuleStatus.Registered);
        provider.GetServices<IGreetingProvider>().Count().ShouldBe(2);
        PluginFixture.TryDeleteDirectory(root);
    }

    [Test]
    public void IncompatiblePluginIsSkippedBeforeAssemblyLoading()
    {
        using var fixture = PluginFixture.Create();
        ModuleManifest manifest = fixture.ReadManifest();
        manifest.SupportedOperatingSystems =
        [
            OperatingSystem.IsWindows() ? "linux" : "windows"
        ];
        fixture.WriteManifest(manifest);
        File.WriteAllBytes(Path.Combine(fixture.Root, manifest.EntryAssembly), [0x00, 0x01, 0x02, 0x03]);
        var services = new ServiceCollection();

        ModuleCatalog catalog = ModuleLoader.Load(
            services,
            new ConfigurationBuilder().Build(),
            Options(fixture.Root));

        catalog.Modules.ShouldHaveSingleItem().Status.ShouldBe(ModuleStatus.Skipped);
        services.ShouldBeEmpty();
    }

    [Test]
    public void OptionalFailureIsDiagnosedWithoutPartialRegistration()
    {
        using var fixture = PluginFixture.Create();
        ModuleManifest manifest = fixture.ReadManifest();
        manifest.EntryAssembly = "missing.dll";
        fixture.WriteManifest(manifest);
        var services = new ServiceCollection();

        ModuleCatalog catalog = ModuleLoader.Load(
            services,
            new ConfigurationBuilder().Build(),
            Options(fixture.Root));

        catalog.Modules.ShouldHaveSingleItem().Status.ShouldBe(ModuleStatus.Failed);
        catalog.Modules[0].Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "modularity.manifest-invalid");
        services.ShouldBeEmpty();
    }

    [Test]
    public void MissingPrivateDependencyIsDiagnosedWithoutPartialRegistration()
    {
        using var fixture = PluginFixture.Create();
        File.Delete(Path.Combine(fixture.Root, "Pocok.Modularity.FixtureDependency.dll"));
        var services = new ServiceCollection();

        ModuleCatalog catalog = ModuleLoader.Load(
            services,
            new ConfigurationBuilder().Build(),
            Options(fixture.Root));

        catalog.Modules.ShouldHaveSingleItem().Status.ShouldBe(ModuleStatus.Failed);
        catalog.Modules[0].Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "modularity.load-failed");
        services.ShouldBeEmpty();
    }

    [Test]
    public void RegistrationFailureIsAtomicForThePlugin()
    {
        using var fixture = PluginFixture.Create();
        var services = new ServiceCollection();
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Modules:fixture.greeting:ThrowDuringRegistration"] = "true"
            })
            .Build();

        ModuleCatalog catalog = ModuleLoader.Load(services, configuration, Options(fixture.Root));

        catalog.Modules.ShouldHaveSingleItem().Status.ShouldBe(ModuleStatus.Failed);
        services.ShouldBeEmpty();
    }

    [Test]
    public void EntryAssemblyWithoutModuleIsDiagnosed()
    {
        using var fixture = PluginFixture.Create();
        ModuleManifest manifest = fixture.ReadManifest();
        manifest.EntryAssembly = "Pocok.Modularity.FixtureDependency.dll";
        fixture.WriteManifest(manifest);
        var services = new ServiceCollection();

        ModuleCatalog catalog = ModuleLoader.Load(
            services,
            new ConfigurationBuilder().Build(),
            Options(fixture.Root));

        catalog.Modules.ShouldHaveSingleItem().Status.ShouldBe(ModuleStatus.Failed);
        catalog.Modules[0].Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "modularity.load-failed");
        services.ShouldBeEmpty();
    }

    [Test]
    public void RequiredFailureRollsBackSuccessfulPluginRegistrations()
    {
        using var good = PluginFixture.Create();
        using var bad = PluginFixture.Create();
        ModuleManifest badManifest = bad.ReadManifest();
        badManifest.Id = "fixture.required-broken";
        badManifest.Required = true;
        badManifest.EntryAssembly = "missing.dll";
        bad.WriteManifest(badManifest);
        var root = PluginFixture.CreateRootContaining(good, bad);
        var services = new ServiceCollection();

        ModuleLoadException exception = Should.Throw<ModuleLoadException>(() => ModuleLoader.Load(
            services,
            new ConfigurationBuilder().Build(),
            Options(root)));

        services.ShouldBeEmpty();
        exception.Catalog.Modules.Count.ShouldBe(2);
        exception.Catalog.Modules.ShouldContain(module => module.Status == ModuleStatus.Registered);
        exception.Catalog.Modules.ShouldContain(module => module.Status == ModuleStatus.Failed && module.Required);
        PluginFixture.TryDeleteDirectory(root);
    }

    [Test]
    public void RequiredDuplicateIdStopsStartupEvenWithoutAnInnerException()
    {
        using var first = PluginFixture.Create();
        using var second = PluginFixture.Create();
        ModuleManifest firstManifest = first.ReadManifest();
        firstManifest.Required = true;
        first.WriteManifest(firstManifest);
        var root = PluginFixture.CreateRootContaining(first, second);
        var services = new ServiceCollection();

        ModuleLoadException exception = Should.Throw<ModuleLoadException>(() => ModuleLoader.Load(
            services,
            new ConfigurationBuilder().Build(),
            Options(root)));

        services.ShouldBeEmpty();
        exception.Catalog.Modules.Count.ShouldBe(2);
        exception.Catalog.Modules.ShouldAllBe(module => module.Status == ModuleStatus.Failed);
        PluginFixture.TryDeleteDirectory(root);
    }

    [Test]
    public void DuplicateIdsAreRejectedDeterministically()
    {
        using var first = PluginFixture.Create();
        using var second = PluginFixture.Create();
        var root = PluginFixture.CreateRootContaining(first, second);
        var services = new ServiceCollection();

        ModuleCatalog catalog = ModuleLoader.Load(
            services,
            new ConfigurationBuilder().Build(),
            Options(root));

        catalog.Modules.Count.ShouldBe(2);
        catalog.Modules.ShouldAllBe(module => module.Status == ModuleStatus.Failed);
        catalog.Modules.ShouldAllBe(module =>
            module.Diagnostics.Any(diagnostic => diagnostic.Code == "modularity.duplicate-id"));
        services.ShouldBeEmpty();
        PluginFixture.TryDeleteDirectory(root);
    }

    [Test]
    public void UnsupportedManifestVersionIsRejectedBeforeAssemblyLoading()
    {
        using var fixture = PluginFixture.Create();
        ModuleManifest manifest = fixture.ReadManifest();
        manifest.ManifestVersion = 2;
        fixture.WriteManifest(manifest);
        File.WriteAllBytes(Path.Combine(fixture.Root, manifest.EntryAssembly), [0x00, 0x01, 0x02, 0x03]);

        ModuleCatalog catalog = ModuleLoader.Load(
            new ServiceCollection(),
            new ConfigurationBuilder().Build(),
            Options(fixture.Root));

        catalog.Modules.ShouldHaveSingleItem().Status.ShouldBe(ModuleStatus.Failed);
        catalog.Modules[0].Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "modularity.manifest-invalid");
    }

    [Test]
    public void MalformedManifestAndPathEscapeAreRejected()
    {
        using var malformed = PluginFixture.Create();
        File.WriteAllText(malformed.ManifestPath, "{ not-json");
        using var escaping = PluginFixture.Create();
        ModuleManifest escapingManifest = escaping.ReadManifest();
        escapingManifest.Id = "fixture.escaping";
        escapingManifest.EntryAssembly = "../outside.dll";
        escaping.WriteManifest(escapingManifest);
        var root = PluginFixture.CreateRootContaining(malformed, escaping);

        ModuleCatalog catalog = ModuleLoader.Load(
            new ServiceCollection(),
            new ConfigurationBuilder().Build(),
            Options(root));

        catalog.Modules.Count.ShouldBe(2);
        catalog.Modules.ShouldAllBe(module => module.Status == ModuleStatus.Failed);
        PluginFixture.TryDeleteDirectory(root);
    }

    [Test]
    public void ServiceCollectionExtensionIsIdempotent()
    {
        using var fixture = PluginFixture.Create();
        var services = new ServiceCollection();
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();

        services.AddPocokModules(configuration, options => Configure(options, fixture.Root));
        services.AddPocokModules(configuration, options => Configure(options, fixture.Root));
        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetServices<IModuleCatalog>().ShouldHaveSingleItem();
        provider.GetServices<IGreetingProvider>().ShouldHaveSingleItem();
    }

    private static ModuleLoadOptions Options(string root)
    {
        var options = new ModuleLoadOptions();
        Configure(options, root);
        return options;
    }

    private static void Configure(ModuleLoadOptions options, string root)
    {
        options.AddDirectory(root);
        options.ShareAssemblyContaining<IGreetingProvider>();
    }

    private sealed class PluginFixture : IDisposable
    {
        private PluginFixture(string root)
        {
            Root = root;
            ManifestPath = Path.Combine(root, "pocok.module.json");
        }

        public string Root { get; }

        public string ManifestPath { get; }

        public void Dispose()
        {
            TryDeleteDirectory(Root);
        }

        public static PluginFixture Create()
        {
            var source = Path.Combine(TestContext.CurrentContext.TestDirectory, "plugin-fixture");
            Directory.Exists(source).ShouldBeTrue($"Plugin fixture was not copied to {source}.");
            var root = Path.Combine(Path.GetTempPath(), $"pocok-module-{Guid.NewGuid():N}");
            CopyDirectory(source, root);
            return new PluginFixture(root);
        }

        public static string CreateRootContaining(params PluginFixture[] fixtures)
        {
            var root = Path.Combine(Path.GetTempPath(), $"pocok-modules-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            for (var index = 0; index < fixtures.Length; index++)
                CopyDirectory(fixtures[index].Root, Path.Combine(root, $"plugin-{index:D2}"));

            return root;
        }

        public ModuleManifest ReadManifest()
        {
            return JsonSerializer.Deserialize<ModuleManifest>(File.ReadAllText(ManifestPath), JsonOptions())!;
        }

        public void WriteManifest(ModuleManifest manifest)
        {
            File.WriteAllText(ManifestPath, JsonSerializer.Serialize(manifest, JsonOptions()));
        }

        public static void TryDeleteDirectory(string path)
        {
            if (!Directory.Exists(path)) return;

            try
            {
                Directory.Delete(path, true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                TestContext.Progress.WriteLine($"Could not delete loaded plugin fixture '{path}': {exception.Message}");
            }
        }

        private static JsonSerializerOptions JsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(source, file);
                var target = Path.Combine(destination, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target, true);
            }
        }
    }
}
