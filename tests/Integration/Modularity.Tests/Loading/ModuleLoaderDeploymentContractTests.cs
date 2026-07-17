// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pocok.Modularity.Catalog;
using Pocok.Modularity.FixtureContracts;
using Pocok.Modularity.Loading;

namespace Pocok.Modularity.Integration.Tests.Loading;

public sealed class ModuleLoaderDeploymentContractTests
{
    [Test]
    public void CustomConfigurationSectionIsPassedToPlugin()
    {
        using var fixture = Fixture.Create();
        ModuleManifest manifest = fixture.ReadManifest();
        manifest.ConfigurationSection = "SupplierModules:Primary";
        fixture.WriteManifest(manifest);
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["SupplierModules:Primary:Prefix"] = "Configured" })
            .Build();
        var services = new ServiceCollection();

        ModuleLoader.Load(services, configuration, Options(fixture.Root));
        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IGreetingProvider>().Greet("module").ShouldBe("Configured module!");
    }

    [Test]
    public void OptionalArchitectureMismatchIsSkippedWithoutLoadingAssembly()
    {
        using var fixture = Fixture.Create();
        ModuleManifest manifest = fixture.ReadManifest();
        manifest.SupportedArchitectures =
            [RuntimeInformation.ProcessArchitecture == Architecture.X64 ? "Arm64" : "X64"];
        fixture.WriteManifest(manifest);
        File.WriteAllBytes(Path.Combine(fixture.Root, manifest.EntryAssembly), [0, 1, 2, 3]);

        ModuleCatalog catalog = ModuleLoader.Load(
            new ServiceCollection(), new ConfigurationBuilder().Build(), Options(fixture.Root));

        catalog.Modules.ShouldHaveSingleItem().Status.ShouldBe(ModuleStatus.Skipped);
    }

    [Test]
    public void OptionalFailureCanBePromotedToStartupFailure()
    {
        using var fixture = Fixture.Create();
        ModuleManifest manifest = fixture.ReadManifest();
        manifest.EntryAssembly = "missing.dll";
        fixture.WriteManifest(manifest);
        ModuleLoadOptions options = Options(fixture.Root);
        options.ThrowOnOptionalFailure = true;
        var services = new ServiceCollection();

        ModuleLoadException exception = Should.Throw<ModuleLoadException>(() =>
            ModuleLoader.Load(services, new ConfigurationBuilder().Build(), options));

        services.ShouldBeEmpty();
        exception.Catalog.Modules.ShouldHaveSingleItem().Status.ShouldBe(ModuleStatus.Failed);
    }

    private static ModuleLoadOptions Options(string root)
    {
        return new ModuleLoadOptions()
            .AddDirectory(root)
            .ShareAssemblyContaining<IGreetingProvider>();
    }

    private sealed class Fixture : IDisposable
    {
        private Fixture(string root)
        {
            Root = root;
            ManifestPath = Path.Combine(root, "pocok.module.json");
        }

        public string Root { get; }
        public string ManifestPath { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Root, true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        public static Fixture Create()
        {
            var source = Path.Combine(TestContext.CurrentContext.TestDirectory, "plugin-fixture");
            var root = Path.Combine(Path.GetTempPath(), $"pocok-deployment-{Guid.NewGuid():N}");
            CopyDirectory(source, root);
            return new Fixture(root);
        }

        public ModuleManifest ReadManifest()
        {
            return JsonSerializer.Deserialize<ModuleManifest>(File.ReadAllText(ManifestPath), JsonOptions())!;
        }

        public void WriteManifest(ModuleManifest manifest)
        {
            File.WriteAllText(ManifestPath, JsonSerializer.Serialize(manifest, JsonOptions()));
        }

        private static JsonSerializerOptions JsonOptions()
        {
            return new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                var target = Path.Combine(destination, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target, true);
            }
        }
    }
}
