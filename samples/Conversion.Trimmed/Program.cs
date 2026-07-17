// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Diagnostics.CodeAnalysis;

namespace Pocok.Conversion.Trimmed;

internal static class Program
{
    private static void Main()
    {
        string[] value = ["1", "2", "3"];

        ConversionResult<int[]> values = ConvertKnownArrayPath(value);
        if (values.IsFailure)
            throw new InvalidOperationException(values.Error!.Message);

        if (!values.Value.SequenceEqual([1, 2, 3]))
            throw new InvalidOperationException("The trimmed array conversion result was incorrect.");

        Console.WriteLine(string.Join(',', values.Value));
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification =
            "This fixture deliberately exercises and executes one known array conversion path. " +
            "It is not a claim that arbitrary runtime-selected conversion is trim-compatible.")]
    private static ConversionResult<int[]> ConvertKnownArrayPath(string[] value)
    {
        return ValueConverter.Default.Convert<int[]>(value);
    }
}
