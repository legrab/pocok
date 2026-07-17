// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Microsoft.Extensions.Localization;
using Pocok.Localization;

namespace Pocok.Localization.Tests;

public sealed class CompositeStringLocalizerTests
{
    private enum Sample
    {
        Unknown,
        Known
    }

    [Test]
    public void FirstProviderWinsAndMissingKeysFallBackToTheKey()
    {
        var localizer = new CompositeStringLocalizer([
            new DictionaryStringLocalizer(("Shared", "first"), ("FirstOnly", "first-only")),
            new DictionaryStringLocalizer(("Shared", "second"), ("SecondOnly", "second-only"))
        ]);

        localizer["Shared"].Value.ShouldBe("first");
        localizer["SecondOnly"].Value.ShouldBe("second-only");
        localizer["Missing"].Value.ShouldBe("Missing");
        localizer["Missing"].ResourceNotFound.ShouldBeTrue();
    }

    [Test]
    public void FormattedLookupUsesTheFirstProviderContainingTheResource()
    {
        var localizer = new CompositeStringLocalizer([
            new DictionaryStringLocalizer(("Greeting", "Hello {0}")),
            new DictionaryStringLocalizer(("Greeting", "Hallo {0}"))
        ]);

        LocalizedString result = localizer["Greeting", "Ada"];

        result.Value.ShouldBe("Hello Ada");
        result.ResourceNotFound.ShouldBeFalse();
    }

    [Test]
    public void EnumerationPreservesPrecedenceAndSuppressesDuplicateNames()
    {
        var localizer = new CompositeStringLocalizer([
            new DictionaryStringLocalizer(("Shared", "first"), ("FirstOnly", "first-only")),
            new DictionaryStringLocalizer(("Shared", "second"), ("SecondOnly", "second-only"))
        ]);

        LocalizedString[] values = localizer.GetAllStrings(false).ToArray();

        values.Select(value => value.Name).ShouldBe(["Shared", "FirstOnly", "SecondOnly"]);
        values[0].Value.ShouldBe("first");
    }

    [Test]
    public void EnumerationDoesNotLetMissingEarlierEntriesShadowLaterResources()
    {
        var localizer = new CompositeStringLocalizer([
            new MissingStringLocalizer("Shared"),
            new DictionaryStringLocalizer(("Shared", "second"))
        ]);

        LocalizedString[] values = localizer.GetAllStrings(false).ToArray();

        values.ShouldHaveSingleItem();
        values[0].Name.ShouldBe("Shared");
        values[0].Value.ShouldBe("second");
        values[0].ResourceNotFound.ShouldBeFalse();
    }

    [Test]
    public void ConstructorRejectsNullLocalizers()
    {
        Should.Throw<ArgumentNullException>(() => new CompositeStringLocalizer(null!));
        Should.Throw<ArgumentException>(() => new CompositeStringLocalizer([
            null!
        ]));
    }

    [Test]
    public void ResourceCultureResolvesLanguageAndRegionTags()
    {
        ResourceCulture.TryGetCultureFromFileName("resources/messages.de-DE.json", out CultureInfo? culture)
            .ShouldBeTrue();

        culture!.Name.ShouldBe("de-DE");
    }

    [Test]
    public void ResourceCultureUsesFallbackForMissingInvalidAndExtensionlessTags()
    {
        CultureInfo fallback = CultureInfo.InvariantCulture;

        ResourceCulture.GetCultureFromFileName("messages.txt", fallback).ShouldBeSameAs(fallback);
        ResourceCulture.GetCultureFromFileName("messages.zzzz.txt", fallback).ShouldBeSameAs(fallback);
        ResourceCulture.GetCultureFromFileName("messages.de", fallback).ShouldBeSameAs(fallback);
    }

    [Test]
    public void EnumTranslationPrefersTypeQualifiedKey()
    {
        var localizer = new DictionaryStringLocalizer(("Sample.Known", "Known (translated)"));

        Sample.Known.Translate(localizer).ShouldBe("Known (translated)");
    }

    [Test]
    public void EnumTranslationFallsBackToBareMemberKey()
    {
        var localizer = new DictionaryStringLocalizer(("Known", "Known (bare)"));

        Sample.Known.Translate(localizer).ShouldBe("Known (bare)");
    }

    [Test]
    public void EnumTranslationReturnsBareMemberWhenBothKeysAreMissing()
    {
        Sample.Known.Translate(new DictionaryStringLocalizer()).ShouldBe("Known");
    }

    [Test]
    public void BoxedEnumTranslationUsesTheSameLookupPolicy()
    {
        Enum value = Sample.Known;

        value.Translate(new DictionaryStringLocalizer(("Sample.Known", "Known (boxed)")))
            .ShouldBe("Known (boxed)");
    }

    [Test]
    public void EnumTranslationUsesMemberNameForExplicitZeroValue()
    {
        Sample.Unknown.Translate(new DictionaryStringLocalizer(("Sample.Unknown", "Unknown (translated)")))
            .ShouldBe("Unknown (translated)");
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
                    : new LocalizedString(name, string.Format(CultureInfo.InvariantCulture, result.Value, arguments), false);
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            _entries.Select(entry => new LocalizedString(entry.Key, entry.Value, false));

        private LocalizedString Find(string name) =>
            _entries.TryGetValue(name, out var value)
                ? new LocalizedString(name, value, false)
                : new LocalizedString(name, name, true);
    }

    private sealed class MissingStringLocalizer(string name) : IStringLocalizer
    {
        public LocalizedString this[string requestedName] => Missing(requestedName);

        public LocalizedString this[string requestedName, params object[] arguments] => Missing(requestedName);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            [Missing(name)];

        private static LocalizedString Missing(string requestedName) =>
            new(requestedName, requestedName, true);
    }
}
