// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Primitives;
using System.Globalization;

var result = Result<int>.Success(21)
    .Map(value => value * 2)
    .Bind(value => Result<string>.Success(value.ToString(CultureInfo.InvariantCulture)));

return result.Match(
    value => value == "42" ? 0 : 1,
    _ => 1);
