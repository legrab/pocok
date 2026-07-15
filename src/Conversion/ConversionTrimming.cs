// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Conversion;

internal static class ConversionTrimming
{
    internal const string IncompatibleMessage =
        "Conversion uses runtime type inspection and reflective collection construction. " +
        "Trimming and NativeAOT are not generally supported.";
}
