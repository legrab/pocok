// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;

namespace Pocok.Conversion.Tests;

public sealed class BoundedConversionTests
{
    [Test]
    public void NestedFailureReportsCollectionPath()
    {
        ConversionResult<int[][]> result = ValueConverter.Default.Convert<int[][]>(new[] { new[] { "1", "bad" } });

        result.IsFailure.ShouldBeTrue();
        result.Error!.Path.ShouldBe("$[0][1]");
    }

    [Test]
    public void ItemBudgetStopsLargeCollection()
    {
        var context = new ConversionContext(CultureInfo.InvariantCulture, maximumCollectionItems: 2);
        ConversionResult<int[]> result = ValueConverter.Default.Convert<int[]>(new[] { "1", "2", "3" }, context);

        result.Error!.Code.ShouldBe(ConversionErrorCodes.ResourceLimit);
        result.Error.Path.ShouldBe("$[2]");
    }

    [Test]
    public void DepthBudgetStopsNestedCollection()
    {
        var context = new ConversionContext(CultureInfo.InvariantCulture, maximumDepth: 1);
        ConversionResult<int[][]> result = ValueConverter.Default.Convert<int[][]>(new[] { new[] { "1" } }, context);

        result.Error!.Code.ShouldBe(ConversionErrorCodes.ResourceLimit);
    }

    [Test]
    public void DuplicateConvertedDictionaryKeyFails()
    {
        KeyValuePair<string, string>[] source = new[]
        {
            new KeyValuePair<string, string>("1", "first"),
            new KeyValuePair<string, string>("01", "second")
        };

        ConversionResult<Dictionary<int, string>> result =
            ValueConverter.Default.Convert<Dictionary<int, string>>(source);

        result.Error!.Code.ShouldBe(ConversionErrorCodes.DuplicateKey);
        result.Error.Path.ShouldBe("$[1].key");
    }
}
