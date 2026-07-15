// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Pocok.Modularity;

/// <summary>Discovers trusted plugins, stages their services, and publishes an immutable catalog.</summary>
public static partial class ModuleLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    /// <summary>Loads and registers modules according to the supplied options.</summary>
    public static ModuleCatalog Load(
        IServiceCollection services,
        IConfiguration configuration,
        ModuleLoadOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(options);
        ValidateOptions(options);

        var catalogDiagnostics = new List<ModuleDiagnostic>();
        List<Candidate> candidates = Discover(options, catalogDiagnostics);
        var descriptors = new List<ModuleDescriptor>();
        var stagedServices = new ServiceCollection();
        var hasFatalFailure = false;
        Exception? fatalException = null;

        var duplicateIds = candidates
            .Where(candidate => candidate.Identity is not null)
            .GroupBy(candidate => candidate.Identity!.Id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (Candidate candidate in candidates.OrderBy(candidate => candidate.ManifestPath, StringComparer.Ordinal))
        {
            if (candidate.PreparationFailure is not null)
            {
                descriptors.Add(candidate.ToFailedDescriptor(candidate.PreparationFailure));
                if (candidate.Required || options.ThrowOnOptionalFailure)
                {
                    hasFatalFailure = true;
                    fatalException ??= candidate.PreparationFailure.Exception;
                }

                continue;
            }

            if (duplicateIds.Contains(candidate.Identity!.Id))
            {
                ModuleDiagnostic diagnostic = Error(
                    "modularity.duplicate-id",
                    $"Module id '{candidate.Identity.Id}' is declared by more than one manifest.");
                descriptors.Add(candidate.ToFailedDescriptor(diagnostic));
                if (candidate.Required || options.ThrowOnOptionalFailure)
                    hasFatalFailure = true;

                continue;
            }

            if (!IsCompatible(candidate.Manifest, out var skipReason))
            {
                descriptors.Add(candidate.ToDescriptor(
                    ModuleStatus.Skipped,
                    [
                        new ModuleDiagnostic("modularity.incompatible", skipReason,
                            ModuleDiagnosticSeverity.Information)
                    ]));
                continue;
            }

            try
            {
                ServiceCollection pluginServices = LoadPlugin(candidate, configuration, options);
                foreach (ServiceDescriptor descriptor in pluginServices) stagedServices.Add(descriptor);

                descriptors.Add(candidate.ToDescriptor(
                    ModuleStatus.Registered,
                    [
                        new ModuleDiagnostic(
                            "modularity.registered",
                            $"Module '{candidate.Identity.Id}' registered successfully.",
                            ModuleDiagnosticSeverity.Information)
                    ]));
            }
            catch (Exception exception) when (exception is not ModuleLoadException)
            {
                ModuleDiagnostic diagnostic = Error(
                    "modularity.load-failed",
                    $"Module '{candidate.Identity!.Id}' could not be loaded or registered.",
                    exception);
                descriptors.Add(candidate.ToFailedDescriptor(diagnostic));
                if (candidate.Required || options.ThrowOnOptionalFailure)
                {
                    hasFatalFailure = true;
                    fatalException ??= exception;
                }
            }
        }

        var catalog = new ModuleCatalog(descriptors, catalogDiagnostics);
        if (hasFatalFailure)
            throw new ModuleLoadException("One or more required modules failed to load.", catalog, fatalException);

        foreach (ServiceDescriptor descriptor in stagedServices) services.Add(descriptor);

        return catalog;
    }

    private static ServiceCollection LoadPlugin(
        Candidate candidate,
        IConfiguration configuration,
        ModuleLoadOptions options)
    {
        var sharedAssemblies = new HashSet<string>(options.SharedAssemblyNames, StringComparer.OrdinalIgnoreCase);
        sharedAssemblies.Add(typeof(IServiceModule).Assembly.GetName().Name!);
        sharedAssemblies.Add(typeof(IConfiguration).Assembly.GetName().Name!);
        sharedAssemblies.Add(typeof(IServiceCollection).Assembly.GetName().Name!);
        foreach (var sharedAssembly in candidate.Manifest.SharedAssemblies)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sharedAssembly);
            sharedAssemblies.Add(sharedAssembly);
        }

        var loadContext = new PluginLoadContext(candidate.EntryAssemblyPath!, sharedAssemblies);
        Assembly assembly = loadContext.LoadFromAssemblyPath(candidate.EntryAssemblyPath!);
        Type[] moduleTypes = GetModuleTypes(assembly);
        if (moduleTypes.Length == 0)
            throw new InvalidOperationException(
                $"Entry assembly '{Path.GetFileName(candidate.EntryAssemblyPath)}' contains no public IServiceModule implementation.");

        var sectionName = string.IsNullOrWhiteSpace(candidate.Manifest.ConfigurationSection)
            ? $"Modules:{candidate.Identity!.Id}"
            : candidate.Manifest.ConfigurationSection;
        var context = new ModuleContext(
            candidate.Identity!,
            Path.GetDirectoryName(candidate.ManifestPath)!,
            configuration.GetSection(sectionName),
            new Dictionary<string, string>(candidate.Manifest.Metadata, StringComparer.Ordinal));
        var staged = new ServiceCollection();

        foreach (Type moduleType in moduleTypes)
        {
            if (Activator.CreateInstance(moduleType) is not IServiceModule module)
                throw new InvalidOperationException($"Could not construct module entry point '{moduleType.FullName}'.");

            module.ConfigureServices(staged, context);
        }

        return staged;
    }

    private static Type[] GetModuleTypes(Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            var details = exception.LoaderExceptions
                .Where(loader => loader is not null)
                .Select(loader => loader!.Message)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            throw new InvalidOperationException(
                $"One or more types in '{assembly.GetName().Name}' could not be loaded: {string.Join(" | ", details)}",
                exception);
        }

        return
        [
            .. types
                .Where(type => typeof(IServiceModule).IsAssignableFrom(type))
                .Where(type => type is { IsClass: true, IsAbstract: false, IsPublic: true })
                .Where(type => type.GetConstructor(Type.EmptyTypes) is not null)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
        ];
    }

    private static List<Candidate> Discover(
        ModuleLoadOptions options,
        List<ModuleDiagnostic> catalogDiagnostics)
    {
        var manifests = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var configuredDirectory in options.Directories)
        {
            var directory = Path.GetFullPath(configuredDirectory);
            if (!Directory.Exists(directory))
            {
                var diagnostic = new ModuleDiagnostic(
                    "modularity.directory-missing",
                    $"Plugin directory does not exist: {directory}",
                    options.IgnoreMissingDirectories
                        ? ModuleDiagnosticSeverity.Warning
                        : ModuleDiagnosticSeverity.Error);
                catalogDiagnostics.Add(diagnostic);
                if (!options.IgnoreMissingDirectories) throw new DirectoryNotFoundException(diagnostic.Message);

                continue;
            }

            SearchOption search =
                options.SearchRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (var manifest in Directory.EnumerateFiles(directory, options.ManifestFileName, search))
                manifests.Add(Path.GetFullPath(manifest));
        }

        return [.. manifests.Select(ReadCandidate)];
    }

    private static Candidate ReadCandidate(string manifestPath)
    {
        try
        {
            using FileStream stream = File.OpenRead(manifestPath);
            ModuleManifest manifest = JsonSerializer.Deserialize<ModuleManifest>(stream, SerializerOptions)
                                      ?? throw new InvalidDataException("Manifest content was empty.");
            return Candidate.Create(manifestPath, manifest);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException
                                              or InvalidDataException)
        {
            return Candidate.Invalid(
                manifestPath,
                Error("modularity.manifest-invalid", $"Manifest '{manifestPath}' could not be read or validated.",
                    exception));
        }
    }

    private static bool IsCompatible(ModuleManifest manifest, out string reason)
    {
        var currentOperatingSystem = CurrentOperatingSystem();
        if (manifest.SupportedOperatingSystems.Count > 0 &&
            !manifest.SupportedOperatingSystems.Contains(currentOperatingSystem, StringComparer.OrdinalIgnoreCase))
        {
            reason = $"The plugin does not support operating system '{currentOperatingSystem}'.";
            return false;
        }

        var architecture = RuntimeInformation.ProcessArchitecture.ToString();
        if (manifest.SupportedArchitectures.Count > 0 &&
            !manifest.SupportedArchitectures.Contains(architecture, StringComparer.OrdinalIgnoreCase))
        {
            reason = $"The plugin does not support process architecture '{architecture}'.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static string CurrentOperatingSystem()
    {
        if (OperatingSystem.IsWindows()) return "windows";

        if (OperatingSystem.IsLinux()) return "linux";

        if (OperatingSystem.IsMacOS()) return "osx";

        return "unknown";
    }

    private static void ValidateOptions(ModuleLoadOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ManifestFileName);
        if (Path.GetFileName(options.ManifestFileName) != options.ManifestFileName)
            throw new ArgumentException("ManifestFileName must be a filename, not a path.", nameof(options));
    }

    private static ModuleDiagnostic Error(string code, string message, Exception? exception = null)
    {
        return new ModuleDiagnostic(code, message, ModuleDiagnosticSeverity.Error, exception);
    }

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex ModuleIdPattern();

    private sealed class Candidate
    {
        private Candidate(
            string manifestPath,
            ModuleManifest manifest,
            ModuleIdentity? identity,
            string? entryAssemblyPath,
            ModuleDiagnostic? preparationFailure)
        {
            ManifestPath = manifestPath;
            Manifest = manifest;
            Identity = identity;
            EntryAssemblyPath = entryAssemblyPath;
            PreparationFailure = preparationFailure;
        }

        public string ManifestPath { get; }

        public ModuleManifest Manifest { get; }

        public ModuleIdentity? Identity { get; }

        public string? EntryAssemblyPath { get; }

        public ModuleDiagnostic? PreparationFailure { get; }

        public bool Required => Manifest.Required;

        public static Candidate Create(string manifestPath, ModuleManifest manifest)
        {
            try
            {
                if (manifest.ManifestVersion != 1)
                    throw new InvalidDataException(
                        $"Manifest version {manifest.ManifestVersion} is not supported. Expected version 1.");

                if (string.IsNullOrWhiteSpace(manifest.Id))
                    throw new InvalidDataException("Module id is missing or empty.");

                if (!ModuleIdPattern().IsMatch(manifest.Id))
                    throw new InvalidDataException(
                        $"Module id '{manifest.Id}' may contain only letters, digits, dots, underscores, and hyphens.");

                if (string.IsNullOrWhiteSpace(manifest.Version))
                    throw new InvalidDataException("Module version is missing or empty.");

                if (!Version.TryParse(manifest.Version, out Version? version))
                    throw new InvalidDataException($"Module version '{manifest.Version}' is not a valid System.Version value.");

                if (string.IsNullOrWhiteSpace(manifest.EntryAssembly))
                    throw new InvalidDataException("Module entry assembly is missing or empty.");

                manifest.SupportedOperatingSystems ??= new List<string>();
                manifest.SupportedArchitectures ??= new List<string>();
                manifest.SharedAssemblies ??= new List<string>();
                manifest.Metadata ??= new Dictionary<string, string>(StringComparer.Ordinal);

                var pluginDirectory = Path.GetFullPath(Path.GetDirectoryName(manifestPath)!);
                var entryAssemblyPath = Path.GetFullPath(Path.Combine(pluginDirectory, manifest.EntryAssembly));
                var relativeEntryPath = Path.GetRelativePath(pluginDirectory, entryAssemblyPath);
                if (Path.IsPathRooted(relativeEntryPath) ||
                    relativeEntryPath.Equals("..", StringComparison.Ordinal) ||
                    relativeEntryPath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                    relativeEntryPath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal))
                    throw new InvalidDataException("EntryAssembly escapes the plugin directory.");

                if (!entryAssemblyPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("EntryAssembly must reference a DLL.");

                if (!File.Exists(entryAssemblyPath))
                    throw new FileNotFoundException("Entry assembly does not exist.", entryAssemblyPath);

                return new Candidate(
                    manifestPath,
                    manifest,
                    new ModuleIdentity(manifest.Id, version),
                    entryAssemblyPath,
                    null);
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidDataException or IOException)
            {
                return new Candidate(
                    manifestPath,
                    manifest,
                    null,
                    null,
                    Error("modularity.manifest-invalid", $"Manifest '{manifestPath}' failed validation.", exception));
            }
        }

        public static Candidate Invalid(string manifestPath, ModuleDiagnostic diagnostic)
        {
            return new Candidate(manifestPath, new ModuleManifest(), null, null, diagnostic);
        }

        public ModuleDescriptor ToFailedDescriptor(ModuleDiagnostic diagnostic)
        {
            return ToDescriptor(ModuleStatus.Failed, [diagnostic]);
        }

        public ModuleDescriptor ToDescriptor(ModuleStatus status, IReadOnlyList<ModuleDiagnostic> diagnostics)
        {
            return new ModuleDescriptor(Identity, ManifestPath, EntryAssemblyPath, Required, status, diagnostics);
        }
    }
}
