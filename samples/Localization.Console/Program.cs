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
Console.WriteLine($"greeting={greeting.Value} fallback={fallback.Value} missing={fallback.ResourceNotFound} culture={culture.Name}");

return greeting.Value == "Hello Pocok" && fallback.ResourceNotFound && culture.Name == "de-DE" ? 0 : 1;
