// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using System.Text;
using Microsoft.Extensions.Localization;
using Pocok.Localization.Composition;
using Pocok.Localization.Cultures;
using Pocok.Localization.FileResources;
using Pocok.Showcase.Contracts;

namespace Pocok.Showcase.Localization;

public sealed record LocalizationInput
{
    public string SampleId { get; init; } = "hungarian";
    public string Culture { get; init; } = "hu-HU";
    public bool WatchForChanges { get; init; } = true;
    public bool Reload { get; init; }
}

public sealed record LocalizationOutput(
    string Greeting,
    string JsonGreeting,
    string ResxGreeting,
    string Fallback,
    bool Missing,
    string EnumText,
    string Culture,
    bool Reloaded);

public sealed class LocalizationShowcaseSlice : ShowcaseSlice<LocalizationInput, LocalizationOutput>, IShowcasePackageCoverage
{
    private static readonly IReadOnlyList<ShowcaseSample<LocalizationInput>> SampleDefinitions =
    [
        new(
            "hungarian",
            "Samples.hungarian.Name",
            "Samples.hungarian.Description",
            () => new LocalizationInput(),
            true,
            "Szia Pocok",
            "purpose",
            "localizer"),
        new(
            "english",
            "Samples.english.Name",
            "Samples.english.Description",
            () => new LocalizationInput
            {
                SampleId = "english",
                Culture = "en-US"
            },
            false,
            "Hello Pocok",
            "purpose",
            "localizer"),
        new(
            "reload",
            "Samples.reload.Name",
            "Samples.reload.Description",
            () => new LocalizationInput
            {
                SampleId = "reload",
                Culture = "en-US",
                Reload = true
            },
            false,
            "Hello again Pocok",
            "purpose",
            "localizer")
    ];

    public IReadOnlyList<string> CoveredPackageIds { get; } = ["Pocok.Localization"];

    public override ShowcaseSliceDescriptor Descriptor { get; } = new(
        "pocok.showcase.localization",
        "Pocok.Localization",
        "localization",
        "Capability",
        "Experimental",
        "Package.Name",
        "Package.Summary",
        14,
        "src/Localization/README.md",
        true,
        ShowcaseImplementationStatus.Available,
        "localization",
        "1.0.0");

    public override Type PageComponentType => typeof(LocalizationPage);
    public override IReadOnlyList<ShowcaseSample<LocalizationInput>> TypedSamples => SampleDefinitions;
    public override ShowcaseGuide Guide { get; } = new(
        [new ShowcaseGuideSection("purpose", "Guide.Purpose.Title", ["Guide.Purpose.Body"], ["localizer"])],
        [
            new ShowcaseCodeSnippet(
                "localizer",
                "Guide.Snippet.Title",
                "csharp",
                "await using var localizer = new FileStringLocalizer(options);")
        ]);

    public override async ValueTask<LocalizationOutput> ExecuteAsync(
        LocalizationInput input,
        ShowcaseExecutionContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);
        CultureInfo culture = input.Culture switch
        {
            "hu-HU" => CultureInfo.GetCultureInfo("hu-HU"),
            "en-US" => CultureInfo.GetCultureInfo("en-US"),
            _ => throw new ArgumentOutOfRangeException(
                nameof(input),
                input.Culture,
                "Only en-US and hu-HU are supported.")
        };
        string root = Path.Combine(
            Path.GetTempPath(),
            "pocok-showcase-localization",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            await WriteResourcesAsync(root, cancellationToken).ConfigureAwait(false);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            await using var json = new FileStringLocalizer(new FileStringLocalizerOptions
            {
                RootDirectory = root,
                BaseName = "JsonMessages",
                FormatPrecedence = [LocalizationFileFormat.Json],
                WatchForChanges = input.WatchForChanges,
                ReloadDebounce = TimeSpan.FromMilliseconds(25)
            });
            await using var resx = new FileStringLocalizer(new FileStringLocalizerOptions
            {
                RootDirectory = root,
                BaseName = "ResxMessages",
                FormatPrecedence = [LocalizationFileFormat.Resx]
            });
            var fallback = new DictionaryStringLocalizer(
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["FallbackOnly"] = "embedded fallback",
                    ["DemoStatus.Ready"] = culture.TwoLetterISOLanguageName == "hu" ? "Kész" : "Ready"
                });
            var composite = new CompositeStringLocalizer([json, fallback]);
            string greeting = composite["Greeting", "Pocok"].Value;
            string jsonGreeting = json["Greeting", "Pocok"].Value;
            string resxGreeting = resx["Greeting", "Pocok"].Value;
            bool missing = composite["Missing"].ResourceNotFound;
            string enumText = DemoStatus.Ready.Translate(fallback);
            bool reloaded = false;

            if (input.Reload)
            {
                string next = culture.TwoLetterISOLanguageName == "hu"
                    ? "Szia újra {0}"
                    : "Hello again {0}";
                string path = Path.Combine(
                    root,
                    $"JsonMessages.{culture.TwoLetterISOLanguageName}.json");
                await File.WriteAllTextAsync(
                    path,
                    $$"""{"Greeting":"{{next}}"}""",
                    new UTF8Encoding(false),
                    cancellationToken).ConfigureAwait(false);
                await json.ReloadAsync(cancellationToken).ConfigureAwait(false);
                greeting = json["Greeting", "Pocok"].Value;
                reloaded = true;
            }

            _ = ResourceCulture.GetCultureFromFileName(
                Path.Combine(root, "JsonMessages.hu.json"),
                CultureInfo.InvariantCulture);
            return new LocalizationOutput(
                greeting,
                jsonGreeting,
                resxGreeting,
                composite["FallbackOnly"].Value,
                missing,
                enumText,
                culture.Name,
                reloaded);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
            TryDeleteDirectory(root);
        }
    }

    protected override ShowcaseRunResult CreateRunResult(LocalizationOutput output, TimeSpan elapsed) =>
        new(
            ShowcaseRunStatus.Success,
            output.Greeting,
            [
                new ShowcaseResultField("Result.Json", output.JsonGreeting, true, true),
                new ShowcaseResultField("Result.Resx", output.ResxGreeting, true, true),
                new ShowcaseResultField("Result.Fallback", output.Fallback, false, true),
                new ShowcaseResultField("Result.Missing", output.Missing.ToString(), false, true),
                new ShowcaseResultField("Result.Enum", output.EnumText, false, true),
                new ShowcaseResultField("Result.Culture", output.Culture, true, true)
            ],
            codePreview: "FileStringLocalizer + CompositeStringLocalizer + ReloadAsync",
            elapsed: elapsed,
            tipKeys: ["Tips.Fallback", "Tips.Reload"]);

    private static async Task WriteResourcesAsync(string root, CancellationToken cancellationToken)
    {
        var encoding = new UTF8Encoding(false);
        await File.WriteAllTextAsync(
            Path.Combine(root, "JsonMessages.en.json"),
            "{\"Greeting\":\"Hello {0}\"}",
            encoding,
            cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(
            Path.Combine(root, "JsonMessages.hu.json"),
            "{\"Greeting\":\"Szia {0}\"}",
            encoding,
            cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(
            Path.Combine(root, "ResxMessages.en.resx"),
            "<root><data name=\"Greeting\"><value>Hello {0}</value></data></root>",
            encoding,
            cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(
            Path.Combine(root, "ResxMessages.hu.resx"),
            "<root><data name=\"Greeting\"><value>Szia {0}</value></data></root>",
            encoding,
            cancellationToken).ConfigureAwait(false);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private enum DemoStatus
    {
        Ready
    }

    private sealed class DictionaryStringLocalizer(
        IReadOnlyDictionary<string, string> values) : IStringLocalizer
    {
        public LocalizedString this[string name] => values.TryGetValue(name, out string? value)
            ? new LocalizedString(name, value)
            : new LocalizedString(name, name, resourceNotFound: true);

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                LocalizedString value = this[name];
                return value.ResourceNotFound
                    ? value
                    : new LocalizedString(
                        name,
                        string.Format(CultureInfo.CurrentCulture, value.Value, arguments));
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            _ = includeParentCultures;
            return values.Select(static pair => new LocalizedString(pair.Key, pair.Value));
        }
    }
}
