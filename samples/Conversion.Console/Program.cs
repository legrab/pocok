// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Pocok.Conversion;

var converter = new ValueConverter();
string[] values = ["1", "2", "3"];

var strict = converter.Convert<int>("42");
var german = converter.Convert<decimal>("1.234,5", new ConversionContext(
    CultureInfo.GetCultureInfo("de-DE")));
var saturated = converter.Convert<byte>(300, new ConversionContext(
    CultureInfo.InvariantCulture,
    overflow: OverflowPolicy.Saturate));
var flags = converter.Convert<FileAccess>("Read, Write");
var collection = converter.Convert<int[]>(values);

Console.WriteLine($"strict={strict.Value}");
Console.WriteLine($"culture={german.Value.ToString(CultureInfo.InvariantCulture)}");
Console.WriteLine($"saturated={saturated.Value}");
Console.WriteLine($"flags={flags.Value}");
Console.WriteLine($"collection={string.Join(',', collection.Value)}");

[Flags]
internal enum FileAccess
{
    None = 0,
    Read = 1,
    Write = 2
}
