// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Pocok.Conversion;

var context = ConversionContext.Strict;
var contract = typeof(IValueConverter);

return context.Overflow == OverflowPolicy.Fail &&
       contract.IsInterface &&
       ConversionErrorCodes.Overflow == "conversion.overflow"
    ? 0
    : 1;
