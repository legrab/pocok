// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Globalization;
using Pocok.Conversion;

IValueConverter converter = new ValueConverter();
string[] values = ["1", "300"];
var context = new ConversionContext(
    CultureInfo.InvariantCulture,
    overflow: OverflowPolicy.Saturate);

var number = converter.Convert<int>("42");
var bytes = converter.Convert<byte[]>(values, context);

return number.IsSuccess && number.Value == 42 &&
       bytes.IsSuccess && bytes.Value is [1, 255]
    ? 0
    : 1;
