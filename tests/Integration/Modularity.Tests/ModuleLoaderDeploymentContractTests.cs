// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pocok.Modularity.Fixtures;

namespace Pocok.Modularity.Integration.Tests;

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
            .AddInMemoryCollection(new Dictionary<string, string?> { ["SupplierModules:Primary:Prefix"] = "Configured" })
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
        manifest.SupportedArchitectures = [RuntimeInformation.ProcessArchitecture == Architecture.X64 ? "Arm64" : "X64"];
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

    private static ModuleLoadOptions Options(string root) => new ModuleLoadOptions()
        .AddDirectory(root)
        .ShareAssemblyContaining<IGreetingProvider>();

    private sealed class Fixture : IDisposable
    {
        private Fixture(string root) { Root = root; ManifestPath = Path.Combine(root, "pocok.module.json"); }
        public string Root { get; }
        public string ManifestPath { get; }

        public static Fixture Create()
        {
            string source = Path.Combine(TestContext.CurrentContext.TestDirectory, "plugin-fixture");
            string root = Path.Combine(Path.GetTempPath(), $"pocok-deployment-{Guid.NewGuid():N}");
            CopyDirectory(source, root);
            return new Fixture(root);
        }

        public ModuleManifest ReadManifest() => JsonSerializer.Deserialize<ModuleManifest>(File.ReadAllText(ManifestPath), JsonOptions())!;
        public void WriteManifest(ModuleManifest manifest) => File.WriteAllText(ManifestPath, JsonSerializer.Serialize(manifest, JsonOptions()));
        public void Dispose() { try { Directory.Delete(Root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }

        private static JsonSerializerOptions JsonOptions() => new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                string target = Path.Combine(destination, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target, true);
            }
        }
    }
}
