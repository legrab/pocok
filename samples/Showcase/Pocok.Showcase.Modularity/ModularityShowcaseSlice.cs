// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Hosting;
using Pocok.AppDefaults.Modularity;
using Pocok.Modularity;
using Pocok.Modularity.Contracts;
using Pocok.Modularity.Loading;
using Pocok.Showcase.Contracts;

namespace Pocok.Showcase.Modularity;

public sealed record ModularityInput
{
    public string SampleId { get; init; } = "direct";
    public string Mode { get; init; } = "direct";
    public bool IncludeOptional { get; init; }
    public bool IncludeLifecycle { get; init; }
}

public sealed record ModularityOutput(string Code);

public sealed class ModularityShowcaseSlice : ShowcaseSlice<ModularityInput, ModularityOutput>, IShowcasePackageCoverage
{
    private static readonly IReadOnlyList<ShowcaseSample<ModularityInput>> SampleDefinitions =
    [
        new(
            "direct",
            "Samples.direct.Name",
            "Samples.direct.Description",
            () => new ModularityInput
            {
                SampleId = "direct",
                Mode = "direct",
                IncludeOptional = false,
                IncludeLifecycle = false
            },
            true,
            "Recipe generated",
            "purpose",
            "recipe"),
        new(
            "app-defaults",
            "Samples.app-defaults.Name",
            "Samples.app-defaults.Description",
            () => new ModularityInput
            {
                SampleId = "app-defaults",
                Mode = "app-defaults",
                IncludeOptional = true,
                IncludeLifecycle = false
            },
            false,
            "Recipe generated",
            "purpose",
            "recipe"),
        new(
            "manifest",
            "Samples.manifest.Name",
            "Samples.manifest.Description",
            () => new ModularityInput
            {
                SampleId = "manifest",
                Mode = "manifest",
                IncludeOptional = false,
                IncludeLifecycle = true
            },
            false,
            "Recipe generated",
            "purpose",
            "recipe")
    ];

    public override ShowcaseSliceDescriptor Descriptor { get; } = new(
        "pocok.showcase.modularity",
        "Pocok.Modularity",
        "modularity",
        "Capability",
        "Experimental",
        "Package.Name",
        "Package.Summary",
        7,
        "src/Modularity/README.md",
        false,
        ShowcaseImplementationStatus.Available,
        "modularity",
        "1.0.0");

    public override Type PageComponentType => typeof(ModularityPage);
    public override IReadOnlyList<ShowcaseSample<ModularityInput>> TypedSamples => SampleDefinitions;

    public override ShowcaseGuide Guide { get; } = new(
        [new ShowcaseGuideSection("purpose", "Guide.Purpose.Title", ["Guide.Purpose.Body"], ["recipe"])],
        [
            new ShowcaseCodeSnippet(
                "recipe",
                "Guide.Snippet.Title",
                "csharp",
                "Select a preset and adjust the constrained options.")
        ]);

    public IReadOnlyList<string> CoveredPackageIds { get; } =
    [
        "Pocok.Modularity.Contracts",
        "Pocok.Modularity",
        "Pocok.AppDefaults.Modularity"
    ];

    public override ValueTask<ModularityOutput> ExecuteAsync(
        ModularityInput input,
        ShowcaseExecutionContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var code = ModularityRecipeRenderer.Render(input);
        context.Output.Write(code);
        return ValueTask.FromResult(new ModularityOutput(code));
    }

    protected override ShowcaseRunResult CreateRunResult(ModularityOutput output, TimeSpan elapsed)
    {
        return new ShowcaseRunResult(
            ShowcaseRunStatus.Success,
            "Recipe generated",
            codePreview: output.Code,
            elapsed: elapsed,
            tipKeys: ["Tips.Generated", "Tips.Ownership"]);
    }
}

public static class ModularityRecipeRenderer
{
    public static IReadOnlyList<string> Modes { get; } = ["direct", "app-defaults", "manifest"];

    public static string Render(ModularityInput input)
    {
        var registration = input.Mode == "app-defaults"
            ? """
              HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
              builder.AddPocokModularityDefaults(
                  defaults =>
                  {
                      defaults.PluginDirectory = "plugins";
                      defaults.SearchRecursively = true;
                      defaults.ThrowOnOptionalFailure = false;
                      defaults.SharedAssemblyNames.Add("Pocok.Modularity.Contracts");
                  });
              """
            : """
              HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
              builder.Services.AddPocokModules(builder.Configuration, options =>
              {
                  options.AddDirectory("plugins");
                  options.ShareAssemblyContaining<IServiceModule>();
                  options.SearchRecursively = true;
                  options.ThrowOnOptionalFailure = false;
              });
              """;
        var manifest = $$"""
                         {
                           "manifestVersion": 1,
                           "id": "sample.module",
                           "version": "1.0.0",
                           "entryAssembly": "Sample.Module.dll",
                           "required": {{(input.IncludeOptional ? "false" : "true")}},
                           "sharedAssemblies": [ "Pocok.Modularity.Contracts" ],
                           "supportedOperatingSystems": [ "linux", "windows" ],
                           "supportedArchitectures": [ "x64" ]
                         }
                         """;
        const string header = """
                              // Install Pocok.Modularity, Pocok.Modularity.Contracts, and Pocok.AppDefaults.Modularity.
                              using Microsoft.Extensions.DependencyInjection;
                              using Microsoft.Extensions.Hosting;
                              using Pocok.AppDefaults.Modularity;
                              using Pocok.Modularity;
                              using Pocok.Modularity.Contracts;

                              """;
        var recipe = input.Mode == "manifest" || input.IncludeLifecycle
            ? registration + "\n\n// pocok.module.json\n" + manifest
            : registration;
        return header + recipe;
    }

    internal static void CompileProof()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Services.AddPocokModules(builder.Configuration, options =>
        {
            options.AddDirectory("plugins");
            options.ShareAssemblyContaining<IServiceModule>();
        });
        builder.AddPocokModularityDefaults(defaults => defaults.PluginDirectory = "plugins");
        _ = new ModuleManifest
        {
            ManifestVersion = 1,
            Id = "sample.module",
            Version = "1.0.0",
            EntryAssembly = "Sample.Module.dll",
            Required = true
        };
    }
}
