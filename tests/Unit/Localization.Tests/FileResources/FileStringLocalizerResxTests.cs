// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Localization.FileResources;
using Pocok.Localization.Tests.TestSupport;

namespace Pocok.Localization.Tests.FileResources;

public sealed class FileStringLocalizerResxTests
{
    [Test]
    public async Task StringResourcesAndWhitespaceAreLoaded()
    {
        using var directory = TemporaryDirectory.Create();
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "Messages.resx"),
            FileLocalizationTestData.CreateResx(
                ("Greeting", "Helló"),
                ("Empty", string.Empty),
                ("Whitespace", "  kept  ")));

        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(directory.Path);

        localizer["Greeting"].Value.ShouldBe("Helló");
        localizer["Empty"].Value.ShouldBe(string.Empty);
        localizer["Whitespace"].Value.ShouldBe("  kept  ");
    }

    [Test]
    public async Task ResheadersAndCommentsAreIgnored()
    {
        using var directory = TemporaryDirectory.Create();
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "Messages.resx"),
            """
            <?xml version="1.0" encoding="utf-8"?>
            <root>
              <!-- normal RESX metadata -->
              <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
              <data name="Value"><value>loaded</value></data>
            </root>
            """);

        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(directory.Path);

        localizer["Value"].Value.ShouldBe("loaded");
    }

    [Test]
    public void DuplicateOrIncompleteEntriesAreRejected()
    {
        using var directory = TemporaryDirectory.Create();
        var path = Path.Combine(directory.Path, "Messages.resx");
        File.WriteAllText(path,
            """<root><data name="Value"><value>one</value></data><data name="Value"><value>two</value></data></root>""");
        Should.Throw<FormatException>(() => FileLocalizationTestData.CreateLocalizer(directory.Path));

        File.WriteAllText(path, """<root><data><value>one</value></data></root>""");
        Should.Throw<FormatException>(() => FileLocalizationTestData.CreateLocalizer(directory.Path));

        File.WriteAllText(path, """<root><data name="Value" /></root>""");
        Should.Throw<FormatException>(() => FileLocalizationTestData.CreateLocalizer(directory.Path));
    }

    [Test]
    public void UnsupportedDataChildrenAreRejected()
    {
        using var directory = TemporaryDirectory.Create();
        File.WriteAllText(
            Path.Combine(directory.Path, "Messages.resx"),
            """<root><data name="Value"><value>value</value><assembly alias="unexpected" /></data></root>""");

        Should.Throw<FormatException>(() => FileLocalizationTestData.CreateLocalizer(directory.Path));
    }

    [TestCase("type")]
    [TestCase("mimetype")]
    public void TypedOrSerializedEntriesAreRejected(string attributeName)
    {
        using var directory = TemporaryDirectory.Create();
        File.WriteAllText(
            Path.Combine(directory.Path, "Messages.resx"),
            $"<root><data name=\"Value\" {attributeName}=\"unsafe\"><value>value</value></data></root>");

        Should.Throw<FormatException>(() => FileLocalizationTestData.CreateLocalizer(directory.Path))
            .Message.ShouldContain("plain string");
    }

    [Test]
    public void FileReferenceEntriesAreRejected()
    {
        using var directory = TemporaryDirectory.Create();
        File.WriteAllText(
            Path.Combine(directory.Path, "Messages.resx"),
            """<root><data name="Value" type="System.Resources.ResXFileRef, System.Windows.Forms"><value>secret.txt;System.String</value></data></root>""");

        Should.Throw<FormatException>(() => FileLocalizationTestData.CreateLocalizer(directory.Path));
    }

    [Test]
    public void DocumentTypeAndExternalEntitiesAreRejected()
    {
        using var directory = TemporaryDirectory.Create();
        File.WriteAllText(
            Path.Combine(directory.Path, "Messages.resx"),
            """
            <!DOCTYPE root [<!ENTITY xxe SYSTEM "file:///etc/passwd">]>
            <root><data name="Value"><value>&xxe;</value></data></root>
            """);

        FormatException exception = Should.Throw<FormatException>(() =>
            FileLocalizationTestData.CreateLocalizer(directory.Path));

        exception.Message.ShouldContain("Messages.resx");
    }

    [Test]
    public void MaximumFileSizeIsCheckedBeforeXmlParsing()
    {
        using var directory = TemporaryDirectory.Create();
        File.WriteAllText(Path.Combine(directory.Path, "Messages.resx"), "<root />");

        Should.Throw<FormatException>(() => FileLocalizationTestData.CreateLocalizer(
            directory.Path,
            options => options.MaximumFileSizeBytes = 4));
    }

    [Test]
    public async Task ResourceNamesRemainCaseSensitive()
    {
        using var directory = TemporaryDirectory.Create();
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "Messages.resx"),
            FileLocalizationTestData.CreateResx(("Value", "upper"), ("value", "lower")));

        await using FileStringLocalizer localizer = FileLocalizationTestData.CreateLocalizer(directory.Path);

        localizer["Value"].Value.ShouldBe("upper");
        localizer["value"].Value.ShouldBe("lower");
    }
}
