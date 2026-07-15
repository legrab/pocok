// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Diagnostics;
using System.Globalization;
using Pocok.Conversion;

const int iterations = 250_000;
ValueConverter converter = ValueConverter.Default;
var context = new ConversionContext(CultureInfo.InvariantCulture);

static TimeSpan Measure(Action action)
{
    action();
    var stopwatch = Stopwatch.StartNew();
    action();
    stopwatch.Stop();
    return stopwatch.Elapsed;
}

TimeSpan bcl = Measure(() =>
{
    for (var index = 0; index < iterations; index++) _ = int.Parse("12345", CultureInfo.InvariantCulture);
});

TimeSpan pocok = Measure(() =>
{
    for (var index = 0; index < iterations; index++) _ = converter.Convert<int>("12345", context).Value;
});

Console.WriteLine($"BCL parse: {bcl.TotalMilliseconds:F1} ms");
Console.WriteLine($"Pocok conversion: {pocok.TotalMilliseconds:F1} ms");
Console.WriteLine("Results are informational. No arbitrary timing threshold is enforced.");
