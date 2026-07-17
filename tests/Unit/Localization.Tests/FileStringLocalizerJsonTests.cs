// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Text;
using Pocok.Localization;

namespace Pocok.Localization.Tests;

public sealed class FileStringLocalizerJsonTests
{
    [Test]
    public async Task FlatAndNestedStringsAreLoaded()
    {
        using var directory = TemporaryDirectory.Create();
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "Messages.json"),
            """{"Flat":"value","Navigation":{"Home":"home","Deep":{"Value":"deep"}},"Empty":""}""");

        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(directory.Path);

        localizer["Flat"].Value.ShouldBe("value");
        localizer["Navigation.Home"].Value.ShouldBe("home");
        localizer["Navigation.Deep.Value"].Value.ShouldBe("deep");
        localizer["Empty"].Value.ShouldBe(string.Empty);
    }

    [Test]
    public async Task Utf8TextAndCurrentCultureFormattingAreSupported()
    {
        using var directory = TemporaryDirectory.Create();
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "Messages.json"),
            """{"Greeting":"Árvíztűrő tükörfúrógép","Number":"Value: {0:N1}"}""",
            new UTF8Encoding(false));
        using var culture = new TemporaryCulture("de-DE");

        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(directory.Path);

        localizer["Greeting"].Value.ShouldBe("Árvíztűrő tükörfúrógép");
        localizer["Number", 12.5].Value.ShouldContain("12,5");
    }

    [Test]
    public void FlatAndNestedPathCollisionIsRejected()
    {
        using var directory = TemporaryDirectory.Create();
        File.WriteAllText(
            Path.Combine(directory.Path, "Messages.json"),
            """{"Navigation.Home":"flat","Navigation":{"Home":"nested"}}""");

        FormatException exception = Should.Throw<FormatException>(() =>
            FileLocalizationTestData.CreateLocalizer(directory.Path));

        exception.Message.ShouldContain("Navigation.Home");
        exception.Message.ShouldContain("Messages.json");
    }

    [Test]
    public void DuplicatePropertiesAreRejected()
    {
        using var directory = TemporaryDirectory.Create();
        File.WriteAllText(Path.Combine(directory.Path, "Messages.json"), """{"Value":"one","Value":"two"}""");

        Should.Throw<FormatException>(() => FileLocalizationTestData.CreateLocalizer(directory.Path))
            .Message.ShouldContain("Duplicate JSON property");
    }

    [TestCase("[]")]
    [TestCase("{\"Value\":null}")]
    [TestCase("{\"Value\":12}")]
    [TestCase("{\"Value\":true}")]
    [TestCase("{\"Value\":[]}")]
    [TestCase("{\"\":\"value\"}")]
    [TestCase("{\".Value\":\"value\"}")]
    [TestCase("{\"Value.\":\"value\"}")]
    public void UnsupportedJsonShapesAreRejected(string json)
    {
        using var directory = TemporaryDirectory.Create();
        File.WriteAllText(Path.Combine(directory.Path, "Messages.json"), json);

        Should.Throw<FormatException>(() => FileLocalizationTestData.CreateLocalizer(directory.Path));
    }

    [Test]
    public async Task CommentsAndTrailingCommasRequireExplicitOptIn()
    {
        using var directory = TemporaryDirectory.Create();
        var path = Path.Combine(directory.Path, "Messages.json");
        await File.WriteAllTextAsync(path, """{/* note */"Value":"accepted",}""");

        Should.Throw<FormatException>(() => FileLocalizationTestData.CreateLocalizer(directory.Path));

        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(
            directory.Path,
            options =>
            {
                options.AllowJsonComments = true;
                options.AllowTrailingCommas = true;
            });

        localizer["Value"].Value.ShouldBe("accepted");
    }

    [Test]
    public void MaximumFileSizeIsCheckedBeforeParsing()
    {
        using var directory = TemporaryDirectory.Create();
        File.WriteAllText(Path.Combine(directory.Path, "Messages.json"), """{"Value":"large"}""");

        FormatException exception = Should.Throw<FormatException>(() =>
            FileLocalizationTestData.CreateLocalizer(
                directory.Path,
                options => options.MaximumFileSizeBytes = 4));

        exception.Message.ShouldContain("exceeds");
    }

    [Test]
    public async Task Utf8BomIsAccepted()
    {
        using var directory = TemporaryDirectory.Create();
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "Messages.json"),
            """{"Value":"with-bom"}""",
            new UTF8Encoding(true));

        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(directory.Path);

        localizer["Value"].Value.ShouldBe("with-bom");
    }

    [Test]
    public void InvalidUtf8IsRejected()
    {
        using var directory = TemporaryDirectory.Create();
        File.WriteAllBytes(Path.Combine(directory.Path, "Messages.json"), [0x7B, 0x22, 0x80, 0x22, 0x3A, 0x22, 0x78, 0x22, 0x7D]);

        FormatException exception = Should.Throw<FormatException>(() =>
            FileLocalizationTestData.CreateLocalizer(directory.Path));

        exception.Message.ShouldContain("Failed to parse JSON localization file");
    }

    [Test]
    public void Utf16JsonIsRejected()
    {
        using var directory = TemporaryDirectory.Create();
        File.WriteAllText(
            Path.Combine(directory.Path, "Messages.json"),
            """{"Value":"utf16"}""",
            Encoding.Unicode);

        FormatException exception = Should.Throw<FormatException>(() =>
            FileLocalizationTestData.CreateLocalizer(directory.Path));

        exception.Message.ShouldContain("Failed to parse JSON localization file");
    }

    [Test]
    public async Task Utf8BomIsAcceptedDuringReload()
    {
        using var directory = TemporaryDirectory.Create();
        var path = Path.Combine(directory.Path, "Messages.json");
        await File.WriteAllTextAsync(path, """{"Value":"old"}""");
        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(directory.Path);

        await File.WriteAllTextAsync(path, """{"Value":"new"}""", new UTF8Encoding(true));
        await localizer.ReloadAsync();

        localizer["Value"].Value.ShouldBe("new");
        localizer.Status.LastError.ShouldBeNull();
    }

    [Test]
    public async Task InvalidUtf8AfterBomRetainsLastKnownGoodSnapshot()
    {
        using var directory = TemporaryDirectory.Create();
        var path = Path.Combine(directory.Path, "Messages.json");
        await File.WriteAllTextAsync(path, """{"Value":"old"}""");
        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(directory.Path);

        File.WriteAllBytes(path, [0xEF, 0xBB, 0xBF, 0x7B, 0x22, 0x80, 0x22, 0x7D]);

        await Should.ThrowAsync<FormatException>(async () => await localizer.ReloadAsync());

        localizer["Value"].Value.ShouldBe("old");
        localizer.Status.LastError.ShouldBeOfType<FormatException>();
    }

    [Test]
    public async Task KeysRemainCaseSensitive()
    {
        using var directory = TemporaryDirectory.Create();
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "Messages.json"), """{"Value":"upper","value":"lower"}""");

        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(directory.Path);

        localizer["Value"].Value.ShouldBe("upper");
        localizer["value"].Value.ShouldBe("lower");
    }
}
