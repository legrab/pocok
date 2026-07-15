// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Conversion;

string[] value = ["1", "2", "3"];

ConversionResult<int[]> values = ValueConverter.Default.Convert<int[]>(value);

Console.WriteLine(values.IsSuccess ? string.Join(',', values.Value) : values.Error!.Message);
