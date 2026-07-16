// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Microsoft.Extensions.Localization;
using Pocok.Localization;
using Pocok.Localization.Sample;

var localizer = new CompositeStringLocalizer([
    new DictionaryStringLocalizer(("Greeting", "Hello {0}")),
    new DictionaryStringLocalizer(("Greeting", "Hallo {0}"), ("Farewell", "Goodbye"))
]);

var greeting = localizer["Greeting", "Pocok"];
var fallback = localizer["Missing"];
var culture = ResourceCulture.GetCultureFromFileName("messages.de-DE.json", CultureInfo.InvariantCulture);
var status = SampleStatus.Ready.Translate(localizer);
Console.WriteLine($"greeting={greeting.Value} fallback={fallback.Value} missing={fallback.ResourceNotFound} culture={culture.Name} status={status}");

return greeting.Value == "Hello Pocok" && fallback.ResourceNotFound && culture.Name == "de-DE" && status == "Ready" ? 0 : 1;

internal enum SampleStatus
{
    Ready
}
