// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Diagnostics;
using System.Text.Json;

PublishArguments arguments = PublishArguments.Parse(args);
string repositoryRoot = Path.GetFullPath(arguments.RepositoryRoot);
string outputRoot = Path.GetFullPath(arguments.OutputPath);
string webProject = Path.Combine(repositoryRoot, "showcase", "src", "Pocok.Showcase.Web", "Pocok.Showcase.Web.csproj");
string samplesRoot = Path.Combine(repositoryRoot, "samples", "Showcase");

if (!File.Exists(webProject)) throw new FileNotFoundException("Showcase web project was not found.", webProject);
if (Directory.Exists(outputRoot)) Directory.Delete(outputRoot, true);
Directory.CreateDirectory(outputRoot);

var common = new List<string>
{
    "publish", webProject, "--configuration", "Release", "--output", outputRoot, "--nologo", "--maxcpucount:1"
};
if (arguments.NoRestore) common.Add("--no-restore");
Console.WriteLine("Publishing showcase host...");
await RunAsync(arguments.DotNetPath, common, repositoryRoot);

PackageDocument packages = LoadPackageCatalog(repositoryRoot);
string contentRoot = Path.Combine(outputRoot, "Content");
Directory.CreateDirectory(contentRoot);
await File.WriteAllTextAsync(Path.Combine(contentRoot, "package-catalog.json"),
    JsonSerializer.Serialize(packages, PublishJson.Options) + Environment.NewLine);

var inventory = new List<SliceInventoryItem>();
var moduleIds = new HashSet<string>(StringComparer.Ordinal);
var packageIds = new HashSet<string>(StringComparer.Ordinal);
var slugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
IEnumerable<string> projects = Directory.Exists(samplesRoot)
    ? Directory.EnumerateFiles(samplesRoot, "Pocok.Showcase.*.csproj", SearchOption.AllDirectories)
        .Where(path => !Path.GetRelativePath(samplesRoot, path).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(static segment => segment.StartsWith('_')))
        .Order(StringComparer.Ordinal)
    : [];

foreach (string project in projects)
{
    string projectDirectory = Path.GetDirectoryName(project)!;
    string manifestPath = Path.Combine(projectDirectory, "pocok.module.json");
    if (!File.Exists(manifestPath)) throw new InvalidOperationException($"Missing module manifest beside {project}.");
    ModuleManifest manifest = JsonSerializer.Deserialize<ModuleManifest>(await File.ReadAllTextAsync(manifestPath), PublishJson.Options)
                              ?? throw new InvalidOperationException($"Manifest {manifestPath} is empty.");
    ValidateManifest(manifest, manifestPath, moduleIds, packageIds, slugs);

    string pluginOutput = Path.Combine(outputRoot, "plugins", manifest.Id);
    Directory.CreateDirectory(pluginOutput);
    var publish = new List<string>
    {
        "publish", project, "--configuration", "Release", "--output", pluginOutput, "--nologo", "--maxcpucount:1"
    };
    if (arguments.NoRestore) publish.Add("--no-restore");
    Console.WriteLine($"Publishing slice {manifest.Metadata.PackageId}...");
    await RunAsync(arguments.DotNetPath, publish, repositoryRoot);

    string publishedManifest = Path.Combine(pluginOutput, "pocok.module.json");
    if (!File.Exists(publishedManifest)) File.Copy(manifestPath, publishedManifest);
    string entryAssembly = Path.Combine(pluginOutput, manifest.EntryAssembly);
    string deps = Path.ChangeExtension(entryAssembly, ".deps.json");
    if (!File.Exists(entryAssembly)) throw new InvalidOperationException($"Published plugin is missing {manifest.EntryAssembly}.");
    if (!File.Exists(deps)) throw new InvalidOperationException($"Published plugin is missing {Path.GetFileName(deps)}.");
    inventory.Add(new SliceInventoryItem(manifest.Id, manifest.Version, manifest.Required,
        manifest.Metadata.PackageId, manifest.Metadata.Slug, Path.GetRelativePath(outputRoot, pluginOutput).Replace('\\', '/')));
}

if (arguments.RequireCompleteCatalog)
{
    string[] missing = packages.Packages.Where(package => !packageIds.Contains(package.Id)).Select(package => package.Id).ToArray();
    if (missing.Length > 0) throw new InvalidOperationException($"Strict catalog publication is missing: {string.Join(", ", missing)}.");
}

await File.WriteAllTextAsync(Path.Combine(contentRoot, "showcase-slices.json"),
    JsonSerializer.Serialize(new SliceInventoryDocument(inventory), PublishJson.Options) + Environment.NewLine);
Console.WriteLine($"Published Pocok Showcase to {outputRoot} with {inventory.Count} slice(s).");

static async Task RunAsync(string executable, IReadOnlyList<string> arguments, string workingDirectory)
{
    var start = new ProcessStartInfo(executable)
    {
        WorkingDirectory = workingDirectory,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };
    foreach (string argument in arguments) start.ArgumentList.Add(argument);
    using Process process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start {executable}.");
    Task<string> stdout = process.StandardOutput.ReadToEndAsync();
    Task<string> stderr = process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();
    string output = await stdout;
    string error = await stderr;
    if (output.Length > 0) Console.Write(output);
    if (error.Length > 0) Console.Error.Write(error);
    if (process.ExitCode != 0) throw new InvalidOperationException($"{executable} exited with code {process.ExitCode}.");
}

static PackageDocument LoadPackageCatalog(string repositoryRoot)
{
    string path = Path.Combine(repositoryRoot, "eng", "packages.json");
    using JsonDocument source = JsonDocument.Parse(File.ReadAllText(path));
    var result = new List<PackageEntry>();
    int sortOrder = 0;
    foreach (JsonElement package in source.RootElement.GetProperty("packages").EnumerateArray())
    {
        string state = package.GetProperty("state").GetString()!;
        if (state == "Retired") continue;
        string id = package.GetProperty("id").GetString()!;
        string project = package.GetProperty("project").GetString()!;
        result.Add(new PackageEntry(id, package.GetProperty("family").GetString()!, state,
            package.GetProperty("releasable").GetBoolean(), project, Summary(id),
            project[..project.LastIndexOf('/')] + "/README.md", ++sortOrder, Slug(id)));
    }
    return new PackageDocument(result);
}


static string Slug(string packageId)
{
    string value = packageId.StartsWith("Pocok.", StringComparison.Ordinal)
        ? packageId["Pocok.".Length..]
        : packageId;
    var builder = new System.Text.StringBuilder();
    foreach (char character in value)
    {
        if (character == '.')
        {
            if (builder.Length > 0 && builder[^1] != '-') builder.Append('-');
            continue;
        }

        if (char.IsUpper(character) && builder.Length > 0 && builder[^1] != '-')
            builder.Append('-');
        builder.Append(char.ToLowerInvariant(character));
    }

    return builder.ToString();
}

static string Summary(string id) => id switch
{
    "Pocok.Conversion" => "Policy-driven value conversion with explicit failures, collection support, and bounded behavior.",
    "Pocok.Readiness" => "Explicit startup, readiness, failure, cancellation, shutdown, and restart coordination.",
    "Pocok.AppDefaults" => "Composable application configurators for consistent host setup.",
    "Pocok.AppDefaults.Logging" => "Configuration-driven Microsoft.Extensions.Logging defaults.",
    "Pocok.AppDefaults.Logging.Serilog" => "Serilog defaults integrated through the Pocok application configurator model.",
    "Pocok.Modularity.Contracts" => "Stable service-module contracts shared between hosts and trusted plugins.",
    "Pocok.Modularity" => "Manifest-driven startup discovery and registration for trusted in-process modules.",
    "Pocok.AppDefaults.Modularity" => "Maintainer defaults for Pocok modularity configuration.",
    "Pocok.BackgroundWork" => "Small concurrency helpers for coalescing, debouncing, repetition, and observation.",
    "Pocok.Scripting" => "Bounded JavaScript execution with imports, modules, bindings, and structured failures.",
    "Pocok.Localization" => "Deterministic file-backed localization and provider composition.",
    "Pocok.Signals" => "Typed signal runtime abstractions for reading, writing, subscribing, and connection state.",
    "Pocok.Subscriptions" => "Concurrency-safe keyed subscription lifecycle management.",
    "Pocok.Licensing" => "Signed and encrypted license documents, validation, runtime enforcement, and CLI workflows.",
    "Pocok.AppDefaults.Licensing" => "Application defaults for configuring and enforcing Pocok licensing.",
    _ => id
};

static void ValidateManifest(ModuleManifest manifest, string path, HashSet<string> moduleIds, HashSet<string> packageIds, HashSet<string> slugs)
{
    if (manifest.ManifestVersion != 1) throw new InvalidOperationException($"Unsupported manifest version in {path}.");
    if (string.IsNullOrWhiteSpace(manifest.Id) || !moduleIds.Add(manifest.Id)) throw new InvalidOperationException($"Duplicate or empty module id in {path}.");
    if (string.IsNullOrWhiteSpace(manifest.Metadata.PackageId) || !packageIds.Add(manifest.Metadata.PackageId)) throw new InvalidOperationException($"Duplicate or empty package id in {path}.");
    if (string.IsNullOrWhiteSpace(manifest.Metadata.Slug) || !slugs.Add(manifest.Metadata.Slug)) throw new InvalidOperationException($"Duplicate or empty slug in {path}.");
    if (manifest.Metadata.Slug.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-')) throw new InvalidOperationException($"Unsafe slug in {path}.");
    if (string.IsNullOrWhiteSpace(manifest.EntryAssembly)) throw new InvalidOperationException($"Missing entry assembly in {path}.");
}

internal sealed record PublishArguments(string RepositoryRoot, string OutputPath, string DotNetPath, bool NoRestore, bool RequireCompleteCatalog)
{
    public static PublishArguments Parse(string[] args)
    {
        string repositoryRoot = Directory.GetCurrentDirectory();
        string output = Path.Combine(repositoryRoot, "artifacts", "showcase");
        string dotnet = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
        bool noRestore = false;
        bool strict = false;
        for (int index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--repository-root": repositoryRoot = Required(args, ref index); break;
                case "--output": output = Required(args, ref index); break;
                case "--dotnet": dotnet = Required(args, ref index); break;
                case "--no-restore": noRestore = true; break;
                case "--require-complete": strict = true; break;
                default: throw new ArgumentException($"Unknown argument '{args[index]}'.");
            }
        }
        return new PublishArguments(repositoryRoot, output, dotnet, noRestore, strict);
    }

    private static string Required(string[] args, ref int index)
    {
        if (++index >= args.Length) throw new ArgumentException("Missing argument value.");
        return args[index];
    }
}

internal sealed record PackageEntry(string Id, string Family, string State, bool Releasable, string Project,
    string Summary, string DocumentationPath, int SortOrder, string Slug);
internal sealed record PackageDocument(IReadOnlyList<PackageEntry> Packages);
internal sealed record SliceInventoryItem(string ModuleId, string Version, bool Required, string PackageId, string Slug, string Directory);
internal sealed record SliceInventoryDocument(IReadOnlyList<SliceInventoryItem> Slices);
internal sealed record ModuleManifest(int ManifestVersion, string Id, string Version, string EntryAssembly, bool Required,
    IReadOnlyList<string> SharedAssemblies, ModuleMetadata Metadata);
internal sealed record ModuleMetadata(string PackageId, string Slug, string Kind);

internal static class PublishJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web) { WriteIndented = true };
}
