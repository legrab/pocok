// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Microsoft.Extensions.Localization;
using Pocok.Localization.Composition;
using Pocok.Localization.FileResources;
using Pocok.Localization.Tests.TestSupport;

namespace Pocok.Localization.Tests.FileResources;

public sealed class FileStringLocalizerCultureTests
{
    [Test]
    public async Task ExactCultureFallsBackPerKeyToParentThenInvariant()
    {
        using var directory = TemporaryDirectory.Create();
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "Messages.json"),
            """{"InvariantOnly":"invariant","Shared":"invariant"}""");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "Messages.de.json"),
            """{"ParentOnly":"parent","Shared":"parent"}""");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "Messages.de-AT.json"), """{"ExactOnly":"exact"}""");
        using var culture = new TemporaryCulture("de-AT");

        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(directory.Path);

        localizer["ExactOnly"].Value.ShouldBe("exact");
        localizer["ParentOnly"].Value.ShouldBe("parent");
        localizer["InvariantOnly"].Value.ShouldBe("invariant");
        localizer["Shared"].Value.ShouldBe("parent");
    }

    [Test]
    public async Task BaseFileNameUsesPlatformPathComparison()
    {
        using var directory = TemporaryDirectory.Create();
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "messages.json"), """{"Value":"loaded"}""");

        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(directory.Path);

        localizer["Value"].ResourceNotFound.ShouldBe(!OperatingSystem.IsWindows());
    }

    [TestCase("en-US")]
    [TestCase("en")]
    [TestCase("")]
    public async Task InvariantFileProvidesNeutralFallback(string cultureName)
    {
        using var directory = TemporaryDirectory.Create();
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "Messages.json"), """{"Greeting":"Hello {0}"}""");
        using var culture = new TemporaryCulture(cultureName);

        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(directory.Path);

        localizer["Greeting", "world"].Value.ShouldBe("Hello world");
    }

    [Test]
    public async Task DefaultJsonPrecedenceWinsAndResxContributesMissingKeys()
    {
        using var directory = TemporaryDirectory.Create();
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "Messages.json"),
            """{"Shared":"json","JsonOnly":"json-only"}""");
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "Messages.resx"),
            FileLocalizationTestData.CreateResx(("Shared", "resx"), ("ResxOnly", "resx-only")));

        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(directory.Path);

        localizer["Shared"].Value.ShouldBe("json");
        localizer["JsonOnly"].Value.ShouldBe("json-only");
        localizer["ResxOnly"].Value.ShouldBe("resx-only");
    }

    [Test]
    public async Task ReversingFormatPrecedenceReversesSameCultureConflict()
    {
        using var directory = TemporaryDirectory.Create();
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "Messages.json"), """{"Shared":"json"}""");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "Messages.resx"),
            FileLocalizationTestData.CreateResx(("Shared", "resx")));

        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(
            directory.Path,
            options => options.FormatPrecedence = [LocalizationFileFormat.Resx, LocalizationFileFormat.Json]);

        localizer["Shared"].Value.ShouldBe("resx");
    }

    [Test]
    public async Task ExactCultureResxBeatsInvariantJson()
    {
        using var directory = TemporaryDirectory.Create();
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "Messages.json"), """{"Shared":"invariant-json"}""");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "Messages.de.resx"),
            FileLocalizationTestData.CreateResx(("Shared", "german-resx")));
        using var culture = new TemporaryCulture("de-AT");

        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(directory.Path);

        localizer["Shared"].Value.ShouldBe("german-resx");
    }

    [Test]
    public async Task EnumerationUsesLevelPrecedenceAndOrdinalKeyOrder()
    {
        using var directory = TemporaryDirectory.Create();
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "Messages.json"),
            """{"Z":"invariant-z","A":"invariant-a","Shared":"invariant"}""");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "Messages.de.json"),
            """{"B":"parent-b","Shared":"parent"}""");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "Messages.de-AT.json"), """{"C":"exact-c"}""");
        using var culture = new TemporaryCulture("de-AT");

        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(directory.Path);

        LocalizedString[] exact = localizer.GetAllStrings(false).ToArray();
        exact.Select(value => value.Name).ShouldBe(["C"]);

        LocalizedString[] all = localizer.GetAllStrings(true).ToArray();
        all.Select(value => value.Name).ShouldBe(["C", "B", "Shared", "A", "Z"]);
        all.Single(value => value.Name == "Shared").Value.ShouldBe("parent");
    }

    [Test]
    public async Task MissingKeyEchoesNameAndComposesWithExistingComposite()
    {
        using var directory = TemporaryDirectory.Create();
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "Messages.json"), """{"Shared":"external"}""");
        await using FileStringLocalizer files = FileLocalizationTestData.CreateLocalizer(directory.Path);
        var composite = new CompositeStringLocalizer([
            files,
            new DictionaryStringLocalizer(("Shared", "embedded"), ("EmbeddedOnly", "embedded-only"))
        ]);

        composite["Shared"].Value.ShouldBe("external");
        composite["EmbeddedOnly"].Value.ShouldBe("embedded-only");
        composite["Missing"].Value.ShouldBe("Missing");
        composite["Missing"].ResourceNotFound.ShouldBeTrue();
    }

    [Test]
    public void InvalidPathAndFormatOptionsAreRejected()
    {
        using var directory = TemporaryDirectory.Create();

        Should.Throw<ArgumentException>(() => new FileStringLocalizer(new FileStringLocalizerOptions
        {
            RootDirectory = directory.Path,
            BaseName = "../Messages"
        }));
        Should.Throw<ArgumentException>(() => new FileStringLocalizer(new FileStringLocalizerOptions
        {
            RootDirectory = directory.Path,
            BaseName = "Messages.json"
        }));
        Should.Throw<ArgumentException>(() => new FileStringLocalizer(new FileStringLocalizerOptions
        {
            RootDirectory = directory.Path,
            BaseName = "Messages",
            FormatPrecedence = []
        }));
        Should.Throw<ArgumentException>(() => new FileStringLocalizer(new FileStringLocalizerOptions
        {
            RootDirectory = directory.Path,
            BaseName = "Messages",
            FormatPrecedence = [LocalizationFileFormat.Json, LocalizationFileFormat.Json]
        }));
    }

    [Test]
    public async Task InvalidlyNamedSiblingFilesAreIgnored()
    {
        using var directory = TemporaryDirectory.Create();
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "Messages.de.backup.json"), "not-json");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "Other.de.json"), "not-json");

        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(directory.Path);

        localizer["Missing"].ResourceNotFound.ShouldBeTrue();
    }

    private sealed class DictionaryStringLocalizer(params (string Name, string Value)[] entries) : IStringLocalizer
    {
        private readonly Dictionary<string, string> _entries =
            entries.ToDictionary(entry => entry.Name, entry => entry.Value, StringComparer.Ordinal);

        public LocalizedString this[string name] => Find(name);

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                LocalizedString result = Find(name);
                return result.ResourceNotFound
                    ? result
                    : new LocalizedString(name, string.Format(CultureInfo.InvariantCulture, result.Value, arguments),
                        false);
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return _entries.Select(entry => new LocalizedString(entry.Key, entry.Value, false));
        }

        private LocalizedString Find(string name)
        {
            return _entries.TryGetValue(name, out var value)
                ? new LocalizedString(name, value, false)
                : new LocalizedString(name, name, true);
        }
    }
}
