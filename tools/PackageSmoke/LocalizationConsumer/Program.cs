// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Microsoft.Extensions.Localization;
using Pocok.Localization;

var localizer = new CompositeStringLocalizer([
    new EmptyStringLocalizer()
]);
var result = localizer["consumer.missing"];
CultureInfo culture = ResourceCulture.GetCultureFromFileName("messages.de-DE.json", CultureInfo.InvariantCulture);
var status = ConsumerStatus.Ready.Translate(localizer);
return result.ResourceNotFound && result.Value == "consumer.missing" && culture.Name == "de-DE" && status == "Ready" ? 0 : 1;

enum ConsumerStatus
{
    Ready
}

sealed class EmptyStringLocalizer : IStringLocalizer
{
    public LocalizedString this[string name] => new(name, name, true);
    public LocalizedString this[string name, params object[] arguments] => new(name, name, true);
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
}
